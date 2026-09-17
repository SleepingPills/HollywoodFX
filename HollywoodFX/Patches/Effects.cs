using System;
using System.Reflection;
using Comfort.Common;
using HollywoodFX.Decal;
using HollywoodFX.Muzzle;
using HollywoodFX.Muzzle.Patches;
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

    private static void SetDecalsProps(Effects eftEffects)
    {
        var decalsHfxPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("Assets/HollywoodFX/Particles/Prefabs/HFX Decals.prefab");
        Plugin.Log.LogInfo("Instantiating Decal Effects Prefab");
        var decalsHfxInstance = Object.Instantiate(decalsHfxPrefab);
        Plugin.Log.LogInfo("Getting Effects Component");
        var decalsHfxEffects = decalsHfxInstance.GetComponent<Effects>();

        if (Plugin.WoundDecalsEnabled.Value)
        {
            Plugin.Log.LogInfo("Overriding blood decal textures");
            eftEffects.TexDecals._renderTexDimension = PowOfTwoDimensions._1024;
            eftEffects.TexDecals._bloodDecalTexture = decalsHfxEffects.TexDecals._bloodDecalTexture;
            eftEffects.TexDecals._vestDecalTexture = decalsHfxEffects.TexDecals._vestDecalTexture;
            eftEffects.TexDecals._backDecalTexture = decalsHfxEffects.TexDecals._backDecalTexture;
            eftEffects.TexDecals._decalSize = new Vector2(0.1f, 0.115f) * Plugin.WoundDecalsSize.Value;
        }

        if (Plugin.BloodSplatterDecalsEnabled.Value)
        {
            var decalRenderer = eftEffects.DeferredDecals;

            if (decalRenderer == null) return;

            var bleedingDecalOrig = decalRenderer._bleedingDecal;
            var bleedingDecalNew = decalsHfxEffects.DeferredDecals._bleedingDecal;

            if (bleedingDecalOrig == null || bleedingDecalNew == null) return;

            bleedingDecalOrig.DecalMaterial = bleedingDecalNew.DecalMaterial;
            bleedingDecalOrig.DynamicDecalMaterial = bleedingDecalNew.DynamicDecalMaterial;
            bleedingDecalOrig.TileSheetRows = bleedingDecalNew.TileSheetRows;
            bleedingDecalOrig.TileSheetColumns = bleedingDecalNew.TileSheetColumns;
            bleedingDecalOrig.DecalSize = new Vector2(0.125f, 0.175f) * Plugin.BloodSplatterDecalsSize.Value;

            var splatterDecalOrig = decalRenderer._environmentBlood;
            var splatterDecalNew = decalsHfxEffects.DeferredDecals._environmentBlood;

            if (splatterDecalOrig == null || splatterDecalNew == null) return;

            splatterDecalOrig.DecalMaterial = splatterDecalNew.DecalMaterial;
            splatterDecalOrig.DynamicDecalMaterial = splatterDecalNew.DynamicDecalMaterial;
            splatterDecalOrig.TileSheetRows = splatterDecalNew.TileSheetRows;
            splatterDecalOrig.TileSheetColumns = splatterDecalNew.TileSheetColumns;
            splatterDecalOrig.DecalSize = 1.5f * splatterDecalOrig.DecalSize * Plugin.BloodSplatterDecalsSize.Value;
        }

        var impactDecals = decalsHfxEffects.DeferredDecals._decals;
        Decals.TracerScorchMark = impactDecals[0];
        Plugin.Log.LogInfo($"Extracted decal: {Decals.TracerScorchMark} > {Decals.TracerScorchMark.DecalMaterial.name}");
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

        Plugin.Log.LogWarning($"Current static decals limit is: {decalRenderer._maxDecals}");
        if (decalRenderer._maxDecals != newDecalLimit)
        {
            Plugin.Log.LogWarning($"Setting max static decals to {newDecalLimit}");
            decalRenderer._maxDecals = newDecalLimit;
        }

        Plugin.Log.LogWarning($"Current dynamic decals limit is: {decalRenderer._maxDynamicDecals}");
        if (decalRenderer._maxDynamicDecals == newDecalLimit) return;
        
        Plugin.Log.LogWarning($"Setting max dynamic decals to {newDecalLimit}");
        decalRenderer._maxDynamicDecals = newDecalLimit;
    }

    private static void WipeDefaultParticles(Effects effects)
    {
        Plugin.Log.LogInfo("Dropping default impact effects");

        foreach (var effect in effects.EffectsArray)
        {
            // Skip effects which have no material attached
            var name = effect.Name.ToLower();
            
            if (effect.MaterialTypes.Length == 0 || name.Contains("water") || name.Contains("swamp"))
            {
                Plugin.Log.LogInfo($"Skipping {effect.Name}");
                continue;
            }

            if (name.Contains("metal"))
            {
                Plugin.Log.LogInfo("Enhancing lighting");
                effect.FlashMaxDist *= 2f;
                effect.LightIntensity *= 2f;
                effect.LightRange *= 1.5f;
                effect.LightMaxDist *= 2f;
            }

            Plugin.Log.LogInfo($"Wiping {effect.Name}");
            effect.Particles = [];
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
    public static void Postfix(Effects __instance)
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
            Singleton<ImpactController>.Create(new ImpactController(__instance));
            Singleton<DecalPainter>.Create(new DecalPainter(__instance.DeferredDecals));
            
            if (Plugin.MuzzleEffectsEnabled.Value)
            {
                Singleton<FirearmsEffectsCache>.Create(new FirearmsEffectsCache());
                Singleton<MuzzleStatic>.Create(new MuzzleStatic());
                Singleton<MuzzleEffects>.Create(new MuzzleEffects(__instance, true));
                Singleton<LocalPlayerMuzzleEffects>.Create(new LocalPlayerMuzzleEffects(__instance));
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"EffectsAwakePostfixPatch Exception: {e}");
            throw;
        }
    }
}