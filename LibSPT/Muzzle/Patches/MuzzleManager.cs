using System.Reflection;
using Comfort.Common;
using EFT.UI;
using HollywoodFX.Helpers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Muzzle.Patches;

internal class MuzzleManagerUpdatePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.UpdateJetsAndFumes));
    }

    [PatchPostfix]
    private static void Postfix(MuzzleManager __instance, MuzzleJet[] ___muzzleJet_0, MuzzleFume[] ___muzzleFume_0, MuzzleSmoke[] ___muzzleSmoke_0)
    {
        if (Singleton<MuzzleEffects>.Instance == null)
            return;
        
        Singleton<MuzzleEffects>.Instance.UpdateMuzzle(__instance, ___muzzleJet_0, ___muzzleFume_0, ___muzzleSmoke_0);
    }
}

internal class MuzzleManagerShotPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.Shot));
    }

    [PatchPrefix]
    private static bool Prefix(MuzzleManager __instance, bool isVisible, float sqrCameraDistance)
    {
        Singleton<MuzzleEffects>.Instance.Emit(__instance, isVisible, sqrCameraDistance);
        return false;
    }
}

internal class MuzzleManagerDebugPatch1 : ModulePatch
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
            var component = child.gameObject.GetComponent<MeshFilter>();
            Bounds bounds1 = default;
            Bounds bounds2 = default;

            if (component != null)
            {
                bounds1 = component.sharedMesh.bounds;
                bounds2 = component.mesh.bounds;
            }

            ConsoleScreen.Log($"Hierarchy: {child.name} {component} {bounds1.extents} {bounds2.extents}");
        }

        if (___muzzleJet_0 == null) return;

        foreach (var jet in ___muzzleJet_0)
        {
            ConsoleScreen.Log($"Jet: {jet.name} {jet.gameObject.name} {jet.transform.position} {jet.transform.rotation}");
        }
    }
}

internal class MuzzleManagerDebugPatch2 : ModulePatch
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

        // for (var i = 0; i < ___muzzleJet_0.Length; i++)
        // {
        //     var jet = ___muzzleJet_0[i];
        //     var color = Color.white;
        //
        //     if (jet.name.Contains("000") || i == 0)
        //     {
        //         color = Color.red;
        //     }
        //
        //     if (i == (___muzzleJet_0.Length - 1))
        //     {
        //         color = Color.blue;
        //     }
        //
        //     DebugGizmos.Ray(jet.transform.position, -1 * jet.transform.up, color, temporary: true, expiretime: 1f);
        // }

        // ConsoleScreen.Log($"Muzzle Manager cam distance: {Mathf.Sqrt(sqrCameraDistance)}");

        // foreach (var child in __instance.Hierarchy.GetComponentsInChildren<Transform>())
        // {
        //     if (child.name == "fireport")
        //         DebugGizmos.Ray(child.position, -1 * child.up, Color.white, temporary: true, expiretime: 1f);
        // }

        // ???
        if (___muzzleParticlePivot_0 != null && (isVisible || sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleParticlePivot_0)
            {
                t.Play(__instance);
                DebugGizmos.Ray(t.transform.position, -1 * t.transform.up, Color.magenta, temporary: true, expiretime: 1f);
            }
        }

        // Sparks duh
        if (___muzzleSparks_0 != null && (isVisible || sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleSparks_0)
            {
                t.Emit(__instance);
                DebugGizmos.Ray(t.transform.position, -1 * t.transform.up, Color.yellow, temporary: true, expiretime: 1f);
            }
        }

        // This is the lingering smoke cloud after heavy shooting
        if (___muzzleFume_0 != null && (isVisible && sqrCameraDistance < 100.0 || !isVisible && sqrCameraDistance < 4.0))
        {
            foreach (var t in ___muzzleFume_0)
            {
                t.Emit(__instance);
                DebugGizmos.Ray(t.transform.position, -1 * t.transform.up, Color.white, temporary: true, expiretime: 1f);
            }
        }

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