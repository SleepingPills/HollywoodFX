using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.AssetsManager;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

internal class PlayerPoolObjectRoleModelPostfixPatch : ModulePatch
{
    private static readonly Dictionary<string, float> DragOverrides = new()
    {
        { "HumanLCalf", 1.25f },
        { "HumanRCalf", 1.25f },
        { "HumanLThigh1", 1.5f },
        { "HumanRThigh1", 1.5f },
        { "HumanPelvis", 2.0f },
        { "HumanSpine2", 1.5f },
        { "HumanSpine3", 1.25f },
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
                $"Adjusting ragdoll spawner: {spawner.name} drag: {spawner.drag} angular drag: {spawner.angularDrag} mass: {spawner.mass}"
            );

            if (!DragOverrides.TryGetValue(spawner.name, out var drag))
                drag = 1f;
            
            spawner.drag = drag;
            spawner.angularDrag = 0f;
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
    public static void Prefix(RagdollClass __instance)
    {
        Traverse.Create(__instance).Field("func_0").SetValue(new Func<bool, float, bool>(CheckCorpseIsStill));
    }

    private static bool CheckCorpseIsStill(bool sleeping, float timePassed)
    {
        return timePassed >= Plugin.RagdollLifetime.Value;
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
            UnityEngine.Object.Destroy(component);
            return;
        }

        weaponRigidbody.gameObject.SetActive(false);
    }
}

public class LootItemIsRigidBodyDonePrefixPatch : ModulePatch
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

        __result = ____currentPhysicsTime >= Plugin.RagdollLifetime.Value;

        return false;
    }
}