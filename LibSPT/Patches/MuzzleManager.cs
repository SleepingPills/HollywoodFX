using System.Reflection;
using EFT.UI;
using HollywoodFX.Helpers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

/*
 * Player.InitiateShot contains a FireportPosition - might be the way to do things.
 *
 * Muzzle Logic:
 *
 * 1. A MuzzleEffect struct will manage all the gubbins for a particular muzzle combo
 * 2. We'll update it in UpdateJetsAndFumes
 * 3. We'll use the sqrCameraDistance to establish pov. Within 1.5m radius the muzzle effects will be 1x size, and then we lerp it to 2x size at 3m radius.
 *    This allows neat 3rd person scaling and the muzzle won't dominate the entire screen if fired up close.
 * 4. Jets will be handled via ParticleSystem.Emit, in the UpdateJetsAndFumes we'll grab out the forward facing jet and calculate angles to other jets.
 * 5. If the angles are all ~0, it's a simple muzzle or a tri-prong (which have borked jets for some reason).
 * 6. If there are no jets, or a silencer object is present in the weapon children, we assume a silencer is installed and we use silencer jets.
 *
 * Notes:
 * - The Weapon in CurrentShot has Speedfactor which we can use to determine whether we are using a shortened or longer barrel
 * - The Weapon has IsSilenced
 * - To find the forward facing jet, we'll simply find the jet with the lowest angle to the fireport
 */

internal class MuzzleManagerUpdatePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.UpdateJetsAndFumes));
    }

    [PatchPostfix]
    private static void Postfix(MuzzleManager __instance, MuzzleJet[] ___muzzleJet_0)
    {
        /*
         * 1. Get the fireport transform
         * 2. Get the jets
         * 3. Get the smokes
         * 4. Split the jets into Forward and Side. The forward jet will be the one with the lowest angle to the fireport. If the angles are all >45, we skip the Forward jet.
         */
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
            var collider = child.gameObject.GetComponent<Collider>();
            Bounds bounds = default;

            if (collider != null)
            {
                bounds = collider.bounds;
            }
            
            ConsoleScreen.Log($"Hierarchy: {child.name} {collider} {bounds.extents}");
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

        for (var i = 0; i < ___muzzleJet_0.Length; i++)
        {
            var jet = ___muzzleJet_0[i];
            var color = Color.white;

            if (jet.name.Contains("000") || i == 0)
            {
                color = Color.red;
            }

            if (i == (___muzzleJet_0.Length - 1))
            {
                color = Color.blue;
            }

            DebugGizmos.Ray(jet.transform.position, -1 * jet.transform.up, color, temporary: true, expiretime: 1f);
        }
        
        ConsoleScreen.Log($"Muzzle Manager cam distance: {Mathf.Sqrt(sqrCameraDistance)}");

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