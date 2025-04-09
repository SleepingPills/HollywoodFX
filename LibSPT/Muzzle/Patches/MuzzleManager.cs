using System.Reflection;
using Comfort.Common;
using EFT.UI;
using HollywoodFX.Helpers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Muzzle.Patches;

internal class MuzzleManagerShotPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MuzzleManager).GetMethod(nameof(MuzzleManager.Shot));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    private static bool Prefix(MuzzleManager __instance, bool isVisible, float sqrCameraDistance)
    {
        var muzzleStatic = Singleton<MuzzleStatic>.Instance;
        
        if (!muzzleStatic.TryGetMuzzleState(__instance, out var state))
            return true;

        return state.Player.IsYourPlayer
            ? Singleton<LocalPlayerMuzzleEffects>.Instance.Emit(muzzleStatic.CurrentShot, state, isVisible, sqrCameraDistance)
            : Singleton<MuzzleEffects>.Instance.Emit(muzzleStatic.CurrentShot, state, isVisible, sqrCameraDistance);
    }
}