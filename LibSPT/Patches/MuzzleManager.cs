using System.Reflection;
using EFT.UI;
using HollywoodFX.Helpers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

/*
 * Player.InitiateShot contains a FireportPosition - might be the way to do things.
 */

internal class MuzzleManagerUpdatePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.UpdateJetsAndFumes));
    }

    [PatchPostfix]
    private static void Prefix(MuzzleManager __instance, MuzzleJet[] ___muzzleJet_0)
    {
        ConsoleScreen.Log($"Muzzle Manager: {__instance.Hierarchy.name} {__instance.transform.name}");

        // There's a silencer in the list when a silencer is attached and we also get zero jets
        foreach (var child in __instance.Hierarchy.GetComponentsInChildren<Transform>())
        {
            ConsoleScreen.Log($"Hierarchy: {child.name}");
        }

        if (___muzzleJet_0 == null) return;

        foreach (var jet in ___muzzleJet_0)
        {
            ConsoleScreen.Log($"Jet: {jet.name} {jet.gameObject.name} {jet.transform.position} {jet.transform.rotation}");
        }
    }
}

internal class MuzzleManagerShotPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.Shot));
    }

    [PatchPrefix]
    private static bool Prefix(MuzzleManager __instance, bool isVisible, float sqrCameraDistance,
        ref float ___float_0, float ___float_1, Vector2 ___vector2_0, MuzzleParticlePivot[] ___muzzleParticlePivot_0,
        MuzzleSparks[] ___muzzleSparks_0,
        MuzzleFume[] ___muzzleFume_0, MuzzleSmoke[] ___muzzleSmoke_0, HeatEmitter[] ___heatEmitter_0, HeatHazeEmitter[] ___heatHazeEmitter_0,
        MuzzleJet[] ___muzzleJet_0
    )
    {
        ___float_0 = Time.time + __instance.ShotLength;

        // Shows the muzzle flush. But how the feck?
        if (__instance.JetMaterial != null)
            MuzzleJet.RandomizeMaterial(__instance.JetMaterial, ___vector2_0);

        // foreach (var jet in ___muzzleJet_0)
        // {
        //     var color = Color.white;
        //
        //     if (jet.name.Contains("000"))
        //     {
        //         color = Color.red;
        //     }
        //     if (jet.name.Contains("003"))
        //     {
        //         color = Color.blue;
        //     }
        //     
        //     DebugGizmos.Ray(jet.transform.position, -1 * jet.transform.up, color, temporary: true, expiretime: 1f);
        // }
        
        // foreach (var child in __instance.Hierarchy.GetComponentsInChildren<Transform>())
        // {
        //     if (child.name == "fireport")
        //         DebugGizmos.Ray(child.position, -1 * child.up, Color.white, temporary: true, expiretime: 1f);
        // }

        // ???
        if (___muzzleParticlePivot_0 != null && (isVisible || sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleParticlePivot_0)
                t.Play(__instance);
        }

        // Sparks duh
        if (___muzzleSparks_0 != null && (isVisible || sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleSparks_0)
                t.Emit(__instance);
        }

        // This is the lingering smoke cloud after heavy shooting
        // if (___muzzleFume_0 != null && (isVisible && sqrCameraDistance < 100.0 || !isVisible && sqrCameraDistance < 4.0))
        // {
        //     foreach (var t in ___muzzleFume_0)
        //         t.Emit(__instance);
        // }

        // This is the smoke trail emitting from the barrel
        if (___muzzleSmoke_0 != null && (isVisible && sqrCameraDistance < 100.0 || !isVisible && sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleSmoke_0)
                t.Shot();
        }

        if (___float_1 > 0.0f && (isVisible || sqrCameraDistance < 400.0))
            __instance.Light.method_0();

        if (___heatEmitter_0 != null && (isVisible || sqrCameraDistance < 400.0))
        {
            foreach (var t in ___heatEmitter_0)
                t.OnShot();
        }

        if (___heatHazeEmitter_0 == null || !isVisible && sqrCameraDistance >= 400.0)
            return false;

        foreach (var t in ___heatHazeEmitter_0)
            t.OnShot(__instance);

        return false;
    }
}