using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using EFT.UI;
using HarmonyLib;
using HollywoodFX.Particles;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Patches;

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
        if (Plugin.WoundDecalsEnabled.Value)
        {
            var texDecalsTraverse = Traverse.Create(effects.TexDecals);
            var bloodDecals = texDecalsTraverse.Field("_bloodDecalTexture").GetValue();
            if (bloodDecals != null)
            {
                Plugin.Log.LogInfo("Overriding blood decal textures");
                texDecalsTraverse.Field("_vestDecalTexture").SetValue(bloodDecals);
                texDecalsTraverse.Field("_backDecalTexture").SetValue(bloodDecals);
                var woundDecalSize = new Vector2(0.25f, 0.25f) * Plugin.WoundDecalsSize.Value;
                texDecalsTraverse.Field("_decalSize").SetValue(woundDecalSize);
            }            
        }
        
        var decalRenderer = effects.DeferredDecals;

        if (decalRenderer == null) return;
            
        var decalsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Decals");
        Plugin.Log.LogInfo("Instantiating Decal Effects Prefab");
        var decalsInstance = Object.Instantiate(decalsPrefab);
        Plugin.Log.LogInfo("Getting Effects Component");
        var decalsEffects = decalsInstance.GetComponent<Effects>();

        var bleedingDecalOrig = Traverse.Create(decalRenderer).Field("_bleedingDecal").GetValue() as DeferredDecalRenderer.SingleDecal;
        var bleedingDecalNew = Traverse.Create(decalsEffects.DeferredDecals).Field("_bleedingDecal").GetValue() as DeferredDecalRenderer.SingleDecal;
        
        if (bleedingDecalOrig == null || bleedingDecalNew == null) return;
        
        bleedingDecalOrig.DecalMaterial = bleedingDecalNew.DecalMaterial;
        bleedingDecalOrig.DynamicDecalMaterial = bleedingDecalNew.DynamicDecalMaterial;
        Plugin.Log.LogInfo("Decal overrides complete");
    }

    private static void SetDecalLimits(Effects effects)
    {
        if (!Plugin.MiscDecalsEnabled.Value)
            return;

        Plugin.Log.LogInfo("Adjusting decal limits");

        var decalRenderer = effects.DeferredDecals;

        if (decalRenderer == null) return;

        var newDecalLimit = Plugin.MiscMaxDecalCount.Value;

        var decalRendererTraverse = Traverse.Create(decalRenderer);
        
        var maxStaticDecalsValue = decalRendererTraverse.Field("_maxDecals").GetValue<int>();
        Plugin.Log.LogWarning($"Current static decals limit is: {maxStaticDecalsValue}");
        if (maxStaticDecalsValue != newDecalLimit)
        {
            Plugin.Log.LogWarning($"Setting max static decals to {newDecalLimit}");
            decalRendererTraverse.Field("_maxDecals").SetValue(newDecalLimit);
        }

        var maxDynamicDecalsValue = decalRendererTraverse.Field("_maxDynamicDecals").GetValue<int>();
        Plugin.Log.LogWarning($"Current dynamic decals limit is: {maxDynamicDecalsValue}");
        if (maxDynamicDecalsValue != newDecalLimit)
        {
            Plugin.Log.LogWarning($"Setting max dynamic decals to {newDecalLimit}");
            decalRendererTraverse.Field("_maxDynamicDecals").SetValue(newDecalLimit);
        }
        
        var maxConcurrentParticlesField = Traverse.Create(typeof(Effects)).Field("int_0");
        var maxConcurrentParticles = maxConcurrentParticlesField.GetValue<int>();

        Plugin.Log.LogWarning($"Current concurrent particle system limit is: {maxConcurrentParticles}");
        if (maxConcurrentParticles == Plugin.MiscMaxConcurrentParticleSys.Value) return;

        Plugin.Log.LogWarning($"Setting max concurrent particle system limit to {Plugin.MiscMaxConcurrentParticleSys.Value}");
        maxConcurrentParticlesField.SetValue(Plugin.MiscMaxConcurrentParticleSys.Value);
    }

    private static void WipeDefaultParticles(Effects effects)
    {
        Plugin.Log.LogInfo("Dropping various default particle effects");

        foreach (var effect in effects.EffectsArray)
        {
            // Skip grenade effects or those which have no material attached
            if (effect.Name.ToLower().Contains("grenade") || effect.MaterialTypes.Length == 0)
            {
                Plugin.Log.LogInfo($"Skipping {effect.Name}");
                continue;
            }
            
            Plugin.Log.LogInfo($"Processing {effect.Name}");
            var filteredParticles = new List<Effects.Effect.ParticleSys>();

            foreach (var particle in effect.Particles)
            {
                if (particle.Particle.name.ToLower().Contains("spark") || particle.Particle.name.ToLower().Contains("puff"))
                {
                    Plugin.Log.LogInfo($"Dropping {particle.Particle.name}");
                    continue;
                }

                Plugin.Log.LogInfo($"Keeping {particle.Particle.name}");
                filteredParticles.Add(particle);
            }

            Plugin.Log.LogInfo(
                $"Clearing out particles for {effect.Name}: {effect.Particles}, {effect.Flash}, {effect.FlareID}"
            );

            effect.Particles = filteredParticles.ToArray();
            effect.Flash = false;
            effect.FlareID = 0;
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
            var emissionController = __instance.gameObject.AddComponent<EmissionController>();
            Singleton<EmissionController>.Create(emissionController);
            Singleton<ImpactController>.Create(new ImpactController(__instance));
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
    private static readonly ImpactKinetics ImpactKinetics = new();
    
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
        Vector3 position, Vector3 normal, float volume, bool isKnife, bool isHitPointVisible, EPointOfView pov)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        ImpactKinetics.Update(material, position, normal, isHitPointVisible);
        Singleton<ImpactController>.Instance.Emit(ImpactKinetics);
    }
}