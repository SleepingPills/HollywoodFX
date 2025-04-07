using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodFX.Muzzle.Patches;


internal class FirearmsEffectsCache : Dictionary<int, MuzzleManager>;

internal class WeaponPrefabInitHotObjectsPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(WeaponPrefab).GetMethod(nameof(WeaponPrefab.InitHotObjects));
    }

    [PatchPostfix]
    private static void Postfix(WeaponPrefab __instance, Weapon weapon)
    {
        if (Singleton<MuzzleEffects>.Instance is null)
            return;

        var cache = Singleton<FirearmsEffectsCache>.Instance;
        
        if (cache is null)
            return;
        
        if (__instance.ObjectInHands is not WeaponManagerClass weaponManagerClass)
            return;
        
        var firearmsEffectsId = weaponManagerClass.firearmsEffects_0.transform.GetInstanceID();
        
        if (!cache.TryGetValue(firearmsEffectsId, out var muzzleManager))
        {
            muzzleManager = Traverse.Create(weaponManagerClass.firearmsEffects_0).Field("_muzzleManager").GetValue<MuzzleManager>();
            
            if (muzzleManager is null)
                return;
            
            cache[firearmsEffectsId] = muzzleManager;
        }
        
        Singleton<MuzzleEffects>.Instance.UpdateWeapon(muzzleManager, weapon);
    }
}
