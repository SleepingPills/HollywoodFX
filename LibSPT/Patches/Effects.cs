using System;
using System.Collections.Generic;
using System.Reflection;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Patches
{
    public class GameWorldAwakePrefixPatch : ModulePatch
    {
        public static bool IsHideout;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.Awake));
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(GameWorld __instance)
        {
            IsHideout = __instance is HideoutGameWorld;
            Plugin.Log.LogInfo($"Game World Awake Patch: Game world is {__instance}, hideout flag: {IsHideout}");
        }
    }

    public class EffectsAwakePrefixPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Effects).GetMethod(nameof(Effects.Awake));
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance)
        {
            if (__instance.name.Contains("HFX"))
            {
                Plugin.Log.LogInfo($"Skipping EffectsAwakePrefixPatch Reentrancy for HFX effects {__instance.name}");
                return;
            }

            if (GameWorldAwakePrefixPatch.IsHideout)
            {
                Plugin.Log.LogInfo("Skipping EffectsAwakePrefixPatch for the Hideout");
                return;
            }

            try
            {
                SetDecalLimits(__instance);
                SetDecalsProps(__instance);
                WipeDefaultParticles(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EffectsAwakePrefixPatch Exception: {e}");
                throw;
            }
        }

        private static void SetDecalsProps(Effects effects)
        {
            var texDecalsPainter = effects.TexDecals;
            var bloodDecalTexField = typeof(TextureDecalsPainter).GetField("_bloodDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var vestDecalField = typeof(TextureDecalsPainter).GetField("_vestDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var backDecalField = typeof(TextureDecalsPainter).GetField("_backDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var decalSizeField = typeof(TextureDecalsPainter).GetField("_decalSize", BindingFlags.NonPublic | BindingFlags.Instance);

            var bloodDecals = bloodDecalTexField?.GetValue(texDecalsPainter);

            if (bloodDecals != null)
            {
                Plugin.Log.LogInfo("Overriding blood decal textures");
                vestDecalField?.SetValue(texDecalsPainter, bloodDecals);
                backDecalField?.SetValue(texDecalsPainter, bloodDecals);
                decalSizeField?.SetValue(texDecalsPainter, new Vector2(0.25f, 0.25f));
            }

            var decalRenderer = effects.DeferredDecals;

            if (decalRenderer == null) return;
            
            var decalsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Decals");
            Plugin.Log.LogInfo("Instantiating Decal Effects Prefab");
            var decalsInstance = UnityEngine.Object.Instantiate(decalsPrefab);
            Plugin.Log.LogInfo("Getting Effects Component");
            var decalsEffects = decalsInstance.GetComponent<Effects>();

            var bleedingDecalOrig =
                ReflectionUtils.GetFieldValue<DeferredDecalRenderer, DeferredDecalRenderer.SingleDecal>(
                    decalRenderer, "_bleedingDecal");

            var bleedingDecalNew =
                ReflectionUtils.GetFieldValue<DeferredDecalRenderer, DeferredDecalRenderer.SingleDecal>(
                    decalsEffects.DeferredDecals, "_bleedingDecal");

            bleedingDecalOrig.DecalMaterial = bleedingDecalNew.DecalMaterial;
            bleedingDecalOrig.DynamicDecalMaterial = bleedingDecalNew.DynamicDecalMaterial;
        }

        private static void SetDecalLimits(Effects effects)
        {
            if (!Plugin.MiscDecalsEnabled.Value)
                return;

            Plugin.Log.LogInfo("Adjusting decal limits");

            var decalRenderer = effects.DeferredDecals;

            if (decalRenderer == null) return;

            var newDecalLimit = Plugin.MiscMaxDecalCount.Value;

            var maxStaticDecalsField = decalRenderer.GetType().GetField("_maxDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxStaticDecalsField != null)
            {
                var maxStaticDecalsValue = (int)maxStaticDecalsField.GetValue(decalRenderer);

                Plugin.Log.LogWarning($"Current static decals limit is: {maxStaticDecalsValue}");
                if (maxStaticDecalsValue < newDecalLimit)
                {
                    Plugin.Log.LogWarning($"Setting max static decals to {newDecalLimit}");
                    maxStaticDecalsField.SetValue(decalRenderer, newDecalLimit);
                }
            }

            var maxDynamicDecalsField = decalRenderer.GetType().GetField("_maxDynamicDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxDynamicDecalsField != null)
            {
                var maxDynamicDecalsValue = (int)maxDynamicDecalsField.GetValue(decalRenderer);

                Plugin.Log.LogWarning($"Current dynamic decals limit is: {maxDynamicDecalsValue}");
                if (maxDynamicDecalsValue < newDecalLimit)
                {
                    Plugin.Log.LogWarning($"Setting max dynamic decals to {newDecalLimit}");
                    maxDynamicDecalsField.SetValue(decalRenderer, newDecalLimit);
                }
            }

            var maxConcurrentParticles = typeof(Effects).GetField("int_0", BindingFlags.NonPublic | BindingFlags.Static);

            if (maxConcurrentParticles == null) return;
            var maxConcurrentParticlesValue = (int)maxConcurrentParticles.GetValue(null);

            Plugin.Log.LogWarning($"Current concurrent particle system limit is: {maxConcurrentParticlesValue}");
            if (maxConcurrentParticlesValue >= Plugin.MiscMaxConcurrentParticleSys.Value) return;

            Plugin.Log.LogWarning($"Setting max concurrent particle system limit to {Plugin.MiscMaxConcurrentParticleSys.Value}");
            maxConcurrentParticles.SetValue(null, Plugin.MiscMaxConcurrentParticleSys.Value);
        }

        private static void WipeDefaultParticles(Effects effects)
        {
            Plugin.Log.LogInfo("Dropping various default particle effects");

            HashSet<MaterialType> materialsTypes =
            [
                MaterialType.Chainfence,
                MaterialType.GarbageMetal,
                MaterialType.Grate,
                MaterialType.MetalThin,
                MaterialType.MetalThick,
                MaterialType.MetalNoDecal,
                MaterialType.Concrete,
                MaterialType.Stone
            ];

            foreach (var effect in effects.EffectsArray)
            {
                Plugin.Log.LogInfo($"Processing {effect.Name}");
                foreach (var materialType in effect.MaterialTypes)
                {
                    if (!materialsTypes.Contains(materialType)) continue;

                    var filteredParticles = new List<Effects.Effect.ParticleSys>();

                    foreach (var particle in effect.Particles)
                    {
                        if (particle.Particle.name.Contains("Spark"))
                        {
                            Plugin.Log.LogInfo($"Dropping {particle.Particle.name}");
                            continue;
                        }

                        Plugin.Log.LogInfo($"Keeping {particle.Particle.name}");
                        filteredParticles.Add(particle);
                    }

                    Plugin.Log.LogInfo(
                        $"Clearing out particles for {effect.Name} material {materialType}: {effect.Particles}, {effect.Flash}, {effect.FlareID}"
                    );

                    effect.Particles = filteredParticles.ToArray();
                    effect.Flash = false;
                    effect.FlareID = 0;
                    break;
                }
            }
        }
    }

    public class EffectsAwakePostfixPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Effects).GetMethod(nameof(Effects.Awake));
        }

        [PatchPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance)
        {
            if (__instance.name.Contains("HFX"))
            {
                Plugin.Log.LogInfo($"Skipping EffectsAwakePostfixPatch Reentrancy for HFX effects {__instance.name}");
                return;
            }

            if (GameWorldAwakePrefixPatch.IsHideout)
            {
                Plugin.Log.LogInfo("Skipping EffectsAwakePostfixPatch for the Hideout");
                return;
            }

            try
            {
                ImpactController.Instance.Setup(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EffectsAwakePostfixPatch Exception: {e}");
                throw;
            }
        }
    }

    public class EffectsEmitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Need to disambiguate the correct emit method
            return typeof(Effects).GetMethod(nameof(Effects.Emit),
            [
                typeof(MaterialType), typeof(BallisticCollider), typeof(Vector3), typeof(Vector3), typeof(float),
                typeof(bool), typeof(bool), typeof(EPointOfView)
            ]);
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance, MaterialType material, BallisticCollider hitCollider,
            Vector3 position, Vector3 normal, float volume, bool isKnife, ref bool isHitPointVisible, EPointOfView pov)
        {
            if (GameWorldAwakePrefixPatch.IsHideout)
                return;

            var context = new EmissionContext(material, hitCollider, position, normal, volume, isKnife, pov);
            ImpactController.Instance.Emit(__instance, context, ref isHitPointVisible);
        }
    }
}