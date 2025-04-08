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
        return Singleton<MuzzleEffects>.Instance.Emit(__instance, isVisible, sqrCameraDistance);
    }
}
