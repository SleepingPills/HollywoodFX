using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using HollywoodFX.Patches;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class EffectsWipeDefaultExplosionSystemsPatch : ModulePatch
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
            Plugin.Log.LogInfo($"Skipping EffectsWipeDefaultExplosionSystemsPatch Reentrancy for HFX effects {__instance.name}");
            return;
        }

        if (GameWorldAwakePrefixPatch.IsHideout)
        {
            Plugin.Log.LogInfo("Skipping EffectsWipeDefaultExplosionSystemsPatch for the Hideout");
            return;
        }

        try
        {
            WipeDefaultParticles(__instance);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"EffectsWipeDefaultExplosionSystemsPatch Exception: {e}");
            throw;
        }
    }
    
    private static void WipeDefaultParticles(Effects effects)
    {
        Plugin.Log.LogInfo("Processing grenade effects");

        foreach (var effect in effects.EffectsArray)
        {
            // Skip non-grenade effects
            if (!effect.Name.ToLower().Contains("grenade"))
            {
                Plugin.Log.LogInfo($"Skipping {effect.Name}");
                continue;
            }

            Plugin.Log.LogInfo($"Found grenade script {effect.Name}");
            effect.BasicParticleSystemMediator = null;
            effect.Particles = [];
        }
    }
}

public class EffectsEmitGrenadePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Effects).GetMethod(nameof(Effects.EmitGrenade));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(string ename, Vector3 position, Vector3 normal)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        ConsoleScreen.Log($"Grenade: {ename}");
        Singleton<BlastController>.Instance.Emit(ename, position, normal);
    }
}