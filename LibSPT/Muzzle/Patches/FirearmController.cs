using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Patches;
using SPT.Reflection.Patching;

namespace HollywoodFX.Muzzle.Patches;

public class FirearmControllerInitiateShotPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.InitiateShot));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(Player.FirearmController __instance, AmmoItemClass ammo)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;
        
        Singleton<MuzzleStatic>.Instance.UpdateCurrentShot(ammo, __instance.IsSilenced);
    }
}