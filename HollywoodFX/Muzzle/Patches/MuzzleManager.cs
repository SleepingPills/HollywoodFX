using System.Reflection;
using Comfort.Common;
using HollywoodFX.Patches;
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
        if (GameWorldAwakePrefixPatch.IsHideout)
            return true;

        var muzzleStatic = Singleton<MuzzleStatic>.Instance;

        if (!muzzleStatic.TryGetMuzzleState(__instance, out var state))
            return true;

        var result = state.Player.IsYourPlayer
            ? Singleton<LocalPlayerMuzzleEffects>.Instance.Emit(muzzleStatic.CurrentShot, state, isVisible, sqrCameraDistance)
            : Singleton<MuzzleEffects>.Instance.Emit(muzzleStatic.CurrentShot, state, isVisible, sqrCameraDistance);

        state.Time = Time.unscaledTime;
        
        return result;
    }
}