using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.AssetsManager;
using EFT.NextObservedPlayer;
using EFT.UI;
using HarmonyLib;
using RootMotion.FinalIK;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

internal class Patch1 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.method_8));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Rigidbody rigidbody, Vector3 direction, Vector3 point, float thrust)
    {
        thrust = 500;
        rigidbody.AddForceAtPosition(direction * thrust, point, ForceMode.Impulse);
        ConsoleScreen.Log($"Applying corpse impulse, collider: {rigidbody.name} direction: {direction}, point: {point}, thrust: {thrust}");
        return false;
    }
}

internal class Patch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.Start));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(RagdollClass __instance)
    {
        Traverse.Create(__instance).Field("func_0").SetValue(new Func<bool, float, bool>(CheckCorpseIsStill));
    }

    private static bool CheckCorpseIsStill(bool sleeping, float timePassed)
    {
        // ConsoleScreen.Log($"Check corpse isStill: {sleeping}, timePassed: {timePassed}");
        return timePassed >= 15f;
    }
}

internal class Patch3 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.ShotReactions));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(Player __instance, DamageInfoStruct shot, EBodyPart bodyPart)
    {
        if (__instance.PointOfView == EPointOfView.ThirdPerson)
        {
            if (shot.HittedBallisticCollider is BodyPartCollider ballisticCollider)
            {
                ConsoleScreen.Log($"FBBI Enabled: {(__instance.EnabledAnimators & Player.EAnimatorMask.FBBIK)}");
                
                __instance.EnabledAnimators = Player.EAnimatorMask.IK;
                
                Vector3 normalized1 = shot.Direction.normalized * 100;
                var fullBodyBipedIK = Traverse.Create(__instance.HitReaction).Field("ik").GetValue<FullBodyBipedIK>();
                // ConsoleScreen.Log(
                //     $"ShotReactions - {ballisticCollider.BodyPartType} {ballisticCollider.BodyPartColliderType} {normalized1} enabled: {__instance.HitReaction.enabled} ik: {fullBodyBipedIK.enabled}");
                __instance.HitReaction.Hit(EBodyPartColliderType.SpineTop, EBodyPart.Chest, normalized1, shot.HitPoint);
                
                ConsoleScreen.Log($"Body PW: {fullBodyBipedIK.solver.bodyEffector.positionWeight}, P: {fullBodyBipedIK.solver.bodyEffector.position}, PO: {fullBodyBipedIK.solver.bodyEffector.positionOffset}, T: {fullBodyBipedIK.solver.bodyEffector.target}");
                ConsoleScreen.Log($"Left Should PW: {fullBodyBipedIK.solver.leftShoulderEffector.positionWeight}, P: {fullBodyBipedIK.solver.leftShoulderEffector.position}, PO: {fullBodyBipedIK.solver.leftShoulderEffector.positionOffset}, T: {fullBodyBipedIK.solver.leftShoulderEffector.target}");

                // foreach (HitReaction.HitPointEffector effectorHitPoint in __instance.HitReaction.effectorHitPoints)
                // {
                //     ConsoleScreen.Log(
                //         $"EffectorHitPoints - {effectorHitPoint.name} BP: {String.Join(",", effectorHitPoint.bodyParts)} Col: {String.Join(",", effectorHitPoint.colliderTypes)}");
                //     foreach (var effectorLink in effectorHitPoint.effectorLinks)
                //     {
                //         ConsoleScreen.Log($"EffectorLink - {effectorLink.effector} {effectorLink.weight}");
                //     }
                // }
                //
                // foreach (HitReaction.HitPointBone boneHitPoint in __instance.HitReaction.boneHitPoints)
                // {
                //     ConsoleScreen.Log(
                //         $"BoneHitPoints - {boneHitPoint.name} BP: {String.Join(",", boneHitPoint.bodyParts)} Col: {String.Join(",", boneHitPoint.colliderTypes)}");
                // }
            }
        }
    }
}

internal class Patch4 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.method_16));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(Player __instance, EPointOfView pointOfView)
    {
        ConsoleScreen.Log($"Instance: {__instance.GetType()} {__instance.name}");
        
        if (pointOfView == EPointOfView.ThirdPerson)
        {
            FullBodyBipedIK fullBodyBipedIK = Traverse.Create(__instance).Field("_fbbik").GetValue<FullBodyBipedIK>();
            fullBodyBipedIK.enabled = true;
            Traverse.Create(__instance).Field("_fbbikCooldown").SetValue(5);
            fullBodyBipedIK.Start();
            __instance.HitReaction.enabled = true;
            __instance.HitReaction.Start();
            
            ConsoleScreen.Log($"FBBI Enabled: {(__instance.EnabledAnimators & Player.EAnimatorMask.FBBIK)}");

            ConsoleScreen.Log($"IK Position weight: {fullBodyBipedIK.solver.IKPositionWeight}");

            // fullBodyBipedIK.solver.IKPositionWeight = 0.5f;
            
            foreach (var effector in fullBodyBipedIK.solver.effectors)
            {
                ConsoleScreen.Log($"Effector {effector.positionWeight} {effector.rotationWeight} {effector.maintainRelativePositionWeight}");
                // effector.positionWeight = 0.5f;
                // effector.rotationWeight = 0.5f;
                // // effector.maintainRelativePositionWeight = 0.5f;
            }

            foreach (var limbMapping in fullBodyBipedIK.solver.limbMappings)
            {
                ConsoleScreen.Log($"LimbMap {limbMapping.maintainRotationWeight}");
                // limbMapping.maintainRotationWeight = 0.5f;
            }
            
            foreach (var limbMapping in fullBodyBipedIK.solver.boneMappings)
            {
                ConsoleScreen.Log($"BoneMap {limbMapping.maintainRotationWeight}");
                // limbMapping.maintainRotationWeight = 0.5f;
            }
        }
    }
}

