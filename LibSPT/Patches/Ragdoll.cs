using System;
using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

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
        return typeof(RagdollClass).GetMethod("AttachWeapon", BindingFlags.Instance | BindingFlags.Public);
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
        return typeof(LootItem).GetMethod("IsRigidbodyDone", BindingFlags.Instance | BindingFlags.Public, null, [], null);
    }

    [PatchPrefix]
    private static void Prefix(LootItem __instance)
    {
        __instance.gameObject.layer = LayerMask.NameToLayer("Deadbody");
    }
}
