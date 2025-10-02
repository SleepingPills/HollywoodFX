using System;
using System.Reflection;
using Comfort.Common;
using DeferredDecals;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using HollywoodFX.Decal;
using HollywoodFX.Explosion;
using HollywoodFX.Muzzle;
using HollywoodFX.Muzzle.Patches;
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

    private static void SetDecalsProps(Effects eftEffects)
    {
        var decalsHfxPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Decals");
        Plugin.Log.LogInfo("Instantiating Decal Effects Prefab");
        var decalsHfxInstance = Object.Instantiate(decalsHfxPrefab);
        Plugin.Log.LogInfo("Getting Effects Component");
        var decalsHfxEffects = decalsHfxInstance.GetComponent<Effects>();

        if (Plugin.WoundDecalsEnabled.Value)
        {
            var texDecalsOrigTraverse = Traverse.Create(eftEffects.TexDecals);

            texDecalsOrigTraverse.Field("_renderTexDimension").SetValue(PowOfTwoDimensions._1024);

            var bloodDecalsHfx = Traverse.Create(decalsHfxEffects.TexDecals).Field("_bloodDecalTexture").GetValue();
            if (bloodDecalsHfx != null)
            {
                Plugin.Log.LogInfo("Overriding blood decal textures");
                texDecalsOrigTraverse.Field("_bloodDecalTexture").SetValue(bloodDecalsHfx);
                texDecalsOrigTraverse.Field("_vestDecalTexture").SetValue(bloodDecalsHfx);
                texDecalsOrigTraverse.Field("_backDecalTexture").SetValue(bloodDecalsHfx);
                texDecalsOrigTraverse.Field("_decalSize").SetValue(new Vector2(0.075f, 0.15f) * Plugin.WoundDecalsSize.Value);
            }
        }

        if (Plugin.BloodSplatterDecalsEnabled.Value)
        {
            var decalRenderer = eftEffects.DeferredDecals;

            if (decalRenderer == null) return;

            var bleedingDecalOrig = Traverse.Create(decalRenderer).Field("_bleedingDecal").GetValue<DeferredDecalRenderer.SingleDecal>();
            var bleedingDecalNew = Traverse.Create(decalsHfxEffects.DeferredDecals).Field("_bleedingDecal").GetValue<DeferredDecalRenderer.SingleDecal>();

            if (bleedingDecalOrig == null || bleedingDecalNew == null) return;

            bleedingDecalOrig.DecalMaterial = bleedingDecalNew.DecalMaterial;
            bleedingDecalOrig.DynamicDecalMaterial = bleedingDecalNew.DynamicDecalMaterial;
            bleedingDecalOrig.TileSheetRows = bleedingDecalNew.TileSheetRows;
            bleedingDecalOrig.TileSheetColumns = bleedingDecalNew.TileSheetColumns;
            bleedingDecalOrig.DecalSize = new Vector2(0.1f, 0.15f) * Plugin.BloodSplatterDecalsSize.Value;

            var splatterDecalOrig = Traverse.Create(decalRenderer).Field("_environmentBlood").GetValue<DeferredDecalRenderer.SingleDecal>();
            var splatterDecalNew = Traverse.Create(decalsHfxEffects.DeferredDecals).Field("_environmentBlood").GetValue<DeferredDecalRenderer.SingleDecal>();

            if (splatterDecalOrig == null || splatterDecalNew == null) return;

            splatterDecalOrig.DecalMaterial = splatterDecalNew.DecalMaterial;
            splatterDecalOrig.DynamicDecalMaterial = splatterDecalNew.DynamicDecalMaterial;
            splatterDecalOrig.TileSheetRows = splatterDecalNew.TileSheetRows;
            splatterDecalOrig.TileSheetColumns = splatterDecalNew.TileSheetColumns;
            splatterDecalOrig.DecalSize = 1.5f * splatterDecalOrig.DecalSize * Plugin.BloodSplatterDecalsSize.Value;
        }

        var impactDecals = Traverse.Create(decalsHfxEffects.DeferredDecals).Field("_decals").GetValue<DeferredDecalRenderer.SingleDecal[]>();
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

            Plugin.Log.LogInfo($"Processing {effect.Name}");
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
        Vector3 position, Vector3 normal, float volume, bool isKnife, bool isHitPointVisible, EPointOfView pov)
    {
        if (GameWorldAwakePrefixPatch.IsHideout || isKnife)
            return;

        ImpactStatic.Kinetics.Update(material, position, normal, isHitPointVisible);
        Singleton<ImpactController>.Instance.Emit(ImpactStatic.Kinetics);
    }
}