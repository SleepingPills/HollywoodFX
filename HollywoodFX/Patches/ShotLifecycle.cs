using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using HollywoodFX.Gore;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Patches;

public class ShotDelegateWrapperPatch : ModulePatch
{
    public static GDelegate64 OriginalShotDelegate;
    
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        var ballistics = __instance.gameObject.GetComponent<BallisticsCalculator>();
        
        Plugin.Log.LogInfo("Getting the shot delegate field from BallisticsCalculator");
        var shotDelegateField = Traverse.Create(ballistics).Field("gdelegate64_0");
        OriginalShotDelegate = shotDelegateField.GetValue<GDelegate64>();
        Plugin.Log.LogInfo($"Original shot delegate retrieved: {OriginalShotDelegate.Method}");
        shotDelegateField.SetValue(new GDelegate64(OnShot));
        Plugin.Log.LogInfo("Replaced the shot delegate with internal HFX override");
    }
    
    /*
     * This has to be handled here because we must stash player hits before the player gets killed in the ClientGameWorld.ShotDelegate
     * Furthermore, Fika now overrides the ShotDelegate method and doesn't call the base class, which means we have to hook in before ShotDelegate
     * is called at all.
     */
    private static void OnShot(EftBulletClass shotResult)
    {
        var bullet = ImpactStatic.Kinetics.Bullet;

        bullet.Update(shotResult);
        
        var hitCollider = bullet.Info.HitCollider;

        if (hitCollider != null && bullet.HitColliderRoot.gameObject.layer == LayerMaskClass.PlayerLayer)
        {
            Singleton<PlayerDamageRegistry>.Instance.RegisterDamage(ImpactStatic.Kinetics.Bullet, hitCollider, bullet.HitColliderRoot);            
        }
        
        OriginalShotDelegate(shotResult);
    }
}

public class EffectsEmitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Need to disambiguate the correct emit method
        return typeof(Effects).GetMethod(nameof(Effects.Emit),
        [
            typeof(MaterialType), typeof(BallisticCollider), typeof(Vector3), typeof(Vector3), typeof(float),
            typeof(bool), typeof(bool), typeof(EPointOfView)
        ]);
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Effects __instance, MaterialType material, BallisticCollider hitCollider,
        Vector3 position, Vector3 normal, float volume, bool isKnife, bool isHitPointVisible, EPointOfView pov)
    {
        if (GameWorldAwakePrefixPatch.IsHideout || isKnife)
            return;

        ImpactStatic.Kinetics.Update(material, position, normal, isHitPointVisible);
        Singleton<ImpactController>.Instance.Emit(ImpactStatic.Kinetics);
    }
}