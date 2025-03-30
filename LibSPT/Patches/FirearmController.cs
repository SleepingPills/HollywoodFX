using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Muzzle;
using SPT.Reflection.Patching;

namespace HollywoodFX.Patches;

public class FirearmControllerInitiateShotPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.InitiateShot));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(IWeapon weapon, AmmoItemClass ammo)
    {
        Singleton<MuzzleEffects>.Instance.UpdateCurrentShot(weapon, ammo);
    }
}