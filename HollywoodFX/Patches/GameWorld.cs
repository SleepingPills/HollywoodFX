using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Gore;
using HollywoodFX.Lighting;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

public class GameWorldAwakePrefixPatch : ModulePatch
{
    public static bool IsHideout;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.Awake));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(GameWorld __instance)
    {
        IsHideout = __instance is HideoutGameWorld;
        Plugin.Log.LogInfo($"Game world hideout flag: {IsHideout}");

        if (IsHideout) return;

        Singleton<MaterialRegistry>.Create(new MaterialRegistry());
        Singleton<PlayerDamageRegistry>.Create(new PlayerDamageRegistry());
    }
}

public class GameWorldStartedPostfixPatch : ModulePatch
{
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

        ImpactStatic.LocalPlayer = __instance.MainPlayer;
        Plugin.Log.LogInfo($"Found local player: {__instance.MainPlayer.ProfileId}");

        var locationId = __instance.LocationId.ToLower();
        
        Plugin.Log.LogInfo($"Location: {locationId}");
        
        if (locationId.Contains("factory") || locationId.Contains("laboratory"))
        {
            Plugin.Log.LogInfo("Static lighting location detected, applying static lighting");
            StaticMaterialAmbientLighting.AdjustLighting(locationId);
        }
        else
        {
            __instance.gameObject.AddComponent<AmbientLightingController>();
        }
        
        Singleton<MaterialRegistry>.Instance?.SetMipBias(Plugin.MipBias.Value);
        Plugin.Log.LogInfo($"Updated mipmap bias to {Plugin.MipBias.Value}");
    }
}

public class GameWorldShotDelegatePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientGameWorld).GetMethod(nameof(ClientGameWorld.ShotDelegate));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(EftBulletClass shotResult)
    {
        var bullet = ImpactStatic.Kinetics.Bullet;

        bullet.Update(shotResult);
        var hitCollider = bullet.Info.HitCollider;
        if (hitCollider == null)
            return;

        if (bullet.HitColliderRoot.gameObject.layer != LayerMaskClass.PlayerLayer)
            return;

        Singleton<PlayerDamageRegistry>.Instance.RegisterDamage(ImpactStatic.Kinetics.Bullet, hitCollider, bullet.HitColliderRoot);
    }
}

public class MichelinManPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ClientGameWorld).GetMethod(nameof(ClientGameWorld.ShotDelegate));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(EftBulletClass shotResult)
    {
        var hitCollider = shotResult.HitCollider;
        
        if (hitCollider == null)
            return;
        
        var rigidbody = hitCollider.attachedRigidbody;
        
        if (rigidbody == null)
            return;

        var hitColliderRoot = hitCollider.transform.root;
        
        if (hitColliderRoot.gameObject.layer != LayerMaskClass.PlayerLayer)
            return;

        if (hitColliderRoot == ImpactStatic.LocalPlayer.Transform.Original)
            return;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = rigidbody.transform.position;
        // sphere.transform.localScale = Vector3.one * 0.2f;
        sphere.transform.parent = rigidbody.transform;
    }
}