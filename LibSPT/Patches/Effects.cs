using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Patches
{
    public class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(GameWorld __instance)
        {
            var effectsInstance = Singleton<Effects>.Instance;
            // SetBloodDecals(effectsInstance);
            
            ImpactEffectsController.Instance.Setup(effectsInstance);
        }
        
        private static void SetBloodDecals(Effects effects)
        {
            var painter = effects.TexDecals;
            var bloodDecalTexField = typeof(TextureDecalsPainter).GetField("_bloodDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var vestDecalField = typeof(TextureDecalsPainter).GetField("_vestDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var backDecalField = typeof(TextureDecalsPainter).GetField("_backDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);

            var bloodDecals = bloodDecalTexField?.GetValue(painter);

            if (bloodDecals == null)
                return;

            Logger.LogInfo($"Overriding blood decal textures");
            vestDecalField?.SetValue(painter, bloodDecals);
            backDecalField?.SetValue(painter, bloodDecals);
        }
    }

    public class EffectsAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Effects).GetMethod(nameof(Effects.Awake));
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance)
        {
            var decalRenderer = __instance.DeferredDecals;

            if (decalRenderer == null) return;

            var maxStaticDecalsField = decalRenderer.GetType().GetField("_maxDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxStaticDecalsField != null)
            {
                var maxStaticDecalsValue = (int)maxStaticDecalsField.GetValue(decalRenderer);

                Logger.LogWarning($"Current static decals limit is: {maxStaticDecalsValue}");
                if (maxStaticDecalsValue < 2048)
                {
                    Logger.LogWarning($"Setting max static decals to 2048");
                    maxStaticDecalsField.SetValue(decalRenderer, 2048);
                }
            }

            var maxDynamicDecalsField = decalRenderer.GetType().GetField("_maxDynamicDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxDynamicDecalsField == null) return;

            var maxDynamicDecalsValue = (int)maxDynamicDecalsField.GetValue(decalRenderer);

            Logger.LogWarning($"Current dynamic decals limit is: {maxDynamicDecalsValue}");
            if (maxDynamicDecalsValue >= 2048) return;

            Logger.LogWarning($"Setting max dynamic decals to 2048");
            maxDynamicDecalsField.SetValue(decalRenderer, 2048);
            
            var maxConcurrentParticles = typeof(Effects).GetField("int_0", BindingFlags.NonPublic | BindingFlags.Static);

            if (maxConcurrentParticles != null)
            {
                var maxConcurrentParticlesValue = (int)maxConcurrentParticles.GetValue(null);

                Logger.LogWarning($"Current concurrent particle system limit is: {maxConcurrentParticlesValue}");
                if (maxConcurrentParticlesValue < 100)
                {
                    Logger.LogWarning($"Setting max concurrent particle system limit to 100");
                    maxConcurrentParticles.SetValue(null, 100);
                }
            }
            
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

            foreach (var effect in __instance.EffectsArray)
            {
                Logger.LogInfo($"Processing {effect.Name}");
                foreach (var materialType in effect.MaterialTypes)
                {
                    if (!materialsTypes.Contains(materialType)) continue;

                    var filteredParticles = new List<Effects.Effect.ParticleSys>();

                    foreach (var particle in effect.Particles)
                    {
                        if (particle.Particle.name.Contains("Spark"))
                        {
                            Logger.LogInfo($"Dropping {particle.Particle.name}");
                            continue;
                        }

                        Logger.LogInfo($"Keeping {particle.Particle.name}");
                        filteredParticles.Add(particle);
                    }

                    Logger.LogInfo(
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
            // Render things closer than 3 meters but further than 1 of the camera even if the impact location is not directly in the viewport
            if (!isHitPointVisible)
            {
                
                var distance = Vector3.Distance(CameraClass.Instance.Camera.transform.position, position);
                if (distance is > 3f or < 1f)
                {
                    return;
                }

                isHitPointVisible = true;
            }

            var context = new EmissionContext(material, hitCollider, position, normal, volume, isKnife, pov);
            ImpactEffectsController.Instance.Emit(__instance, context);
        }
    }
}