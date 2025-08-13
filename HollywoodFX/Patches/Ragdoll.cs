using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.AssetsManager;
using EFT.Interactive;
using SPT.Reflection.Patching;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Patches;

internal class PlayerPoolObjectRoleModelPostfixPatch : ModulePatch
{
    private static readonly Dictionary<string, float> DragOverrides = new()
    {
        { "HumanLCalf", 1.25f },
        { "HumanRCalf", 1.25f },
        { "HumanLThigh1", 1.5f },
        { "HumanRThigh1", 1.5f },
        { "HumanPelvis", 1.75f },
        { "HumanSpine2", 1.5f },
        { "HumanSpine3", 1.25f },
    };

    private static readonly Dictionary<string, float> MassFactors = new()
    {
        { "HumanSpine2", 1.25f },
        { "HumanSpine3", 1.5f },
        { "HumanHead", 1.5f },
    };


    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerPoolObject).GetMethod(nameof(PlayerPoolObject.OnCreatePoolRoleModel));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    public static void Postfix(PlayerPoolObject __instance)
    {
        foreach (var spawner in __instance.RigidbodySpawners)
        {
            Plugin.Log.LogInfo(
                $"Adjusting rigidbody spawner: {spawner.name}"
            );

            if (!DragOverrides.TryGetValue(spawner.name, out var drag))
                drag = 1f;

            if (!MassFactors.TryGetValue(spawner.name, out var mass))
                mass = 1f;

            spawner.angularDrag = 0f;
            spawner.drag = drag;
            spawner.mass *= mass;

            var collider = spawner.GetComponent<Collider>();

            if (collider == null)
                continue;

            collider.material.bounciness = 0.75f;
            collider.material.bounceCombine = PhysicMaterialCombine.Maximum;

            collider.sharedMaterial.bounciness = 0.75f;
            collider.sharedMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
        }

        foreach (var spawner in __instance.JointSpawners)
        {
            Plugin.Log.LogInfo(
                $"Adjusting joint spawner: {spawner.name} projection: {spawner.enableProjection} distance: {spawner.projectionDistance} angle: {spawner.projectionAngle}"
            );

            spawner.enableProjection = true;

            spawner.swing1Limit.bounciness = 0.3f;
            spawner.swing2Limit.bounciness = 0.3f;
            spawner.highTwistLimit.bounciness = 0.3f;
            spawner.lowTwistLimit.bounciness = 0.3f;

            spawner.swingLimitSpring.spring = 150f;
            spawner.twistLimitSpring.spring = 150f;

            spawner.swingLimitSpring.damper = 0.3f;
            spawner.twistLimitSpring.damper = 0.3f;
        }
    }
}

internal class RagdollStartPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.Start));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(RagdollClass __instance, ref Func<bool, float, bool> ___func_0)
    {
        ___func_0 = CheckCorpseIsStill;
    }

    private static bool CheckCorpseIsStill(bool sleeping, float timePassed)
    {
        return timePassed >= 15f;
    }
}

internal class PlayerRigidbodySleepHierarchyTryPutToSleepPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerRigidbodySleepHierarchy).GetMethod(nameof(PlayerRigidbodySleepHierarchy.TryPutToSleep));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(PlayerRigidbodySleepHierarchy __instance, ref bool __result)
    {
        // There's a NRE triggered in CanSleep sometimes when a dead body ragdoll is reactivated and the game tries to put it to sleep again.
        if (__instance.RigidbodySpawner.Rigidbody != null) return true;
        
        __result = true;
        __instance.MustBeSleeping = true;
        return false;
    }
}

internal class RagdollM1PostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.method_1));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(RagdollClass __instance, Rigidbody rigidbody)
    {
        return rigidbody != null;
    }
}

internal class RagdollStartPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.Start));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    public static void Postfix(RagdollClass __instance, RigidbodySpawner[] ___rigidbodySpawner_0)
    {
        foreach (var spawner in ___rigidbodySpawner_0)
        {
            spawner.Rigidbody.maxDepenetrationVelocity = 1f;
        }
    }
}

internal class AttachWeaponPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagdollClass).GetMethod(nameof(RagdollClass.AttachWeapon));
    }

    [PatchPostfix]
    private static void Postfix(RagdollClass __instance, Rigidbody weaponRigidbody)
    {
        var component = weaponRigidbody.gameObject.GetComponent<SpringJoint>();
        if (component != null)
        {
            Object.Destroy(component);
            return;
        }

        weaponRigidbody.gameObject.SetActive(false);
    }
}

internal class LootItemIsRigidBodyDonePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LootItem).GetMethod(nameof(LootItem.IsRigidbodyDone));
    }

    [PatchPrefix]
    private static bool Prefix(LootItem __instance, ref bool __result, float ____currentPhysicsTime)
    {
        if (__instance.RigidBody.IsSleeping())
            __result = true;

        __result = ____currentPhysicsTime >= 15f;

        return false;
    }
}