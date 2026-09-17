using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using HollywoodFX.Patches;
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
    private static void Postfix(WeaponPrefab __instance, Weapon weapon, IPlayer ____player)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;
        
        var cache = Singleton<FirearmsEffectsCache>.Instance;
        
        if (cache is null || Singleton<MuzzleStatic>.Instance is null || Singleton<LocalPlayerMuzzleEffects>.Instance is null)
            return;
        
        if (__instance.ObjectInHands is not Firearms weaponManagerClass)
            return;
        
        if (____player == null)
            return;
        
        var firearmsEffectsId = weaponManagerClass.FirearmsEffects.transform.GetInstanceID();
        
        if (!cache.TryGetValue(firearmsEffectsId, out var muzzleManager))
        {
            muzzleManager = Traverse.Create(weaponManagerClass.FirearmsEffects).Field("_muzzleManager").GetValue<MuzzleManager>();
            
            if (muzzleManager is null)
                return;
            
            cache[firearmsEffectsId] = muzzleManager;
        }
        
        var muzzleState = Singleton<MuzzleStatic>.Instance.UpdateMuzzleState(muzzleManager, weapon, ____player);

        if (!____player.IsYourPlayer || muzzleState == null) return;
        
        Singleton<LocalPlayerMuzzleEffects>.Instance.UpdateParents(muzzleState);
    }
}