using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Gore;
using HollywoodFX.Lighting;
using HollywoodFX.Muzzle;
using HollywoodFX.Muzzle.Patches;
using SPT.Reflection.Patching;

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

        Singleton<LitMaterialRegistry>.Create(new LitMaterialRegistry());
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

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in __instance.RegisteredPlayers)
        {
            if (!player.IsYourPlayer) continue;

            Plugin.Log.LogInfo($"Found local player: {player.ProfileId}");
            ImpactStatic.LocalPlayerTransform = player.Transform.Original;
            break;
        }

        if (__instance.LocationId.Contains("factory"))
        {
            Plugin.Log.LogInfo("Factory location detected, applying static lighting");
            StaticMaterialAmbientLighting.AdjustLighting(__instance.LocationId);
        }
        else
        {
            __instance.gameObject.AddComponent<DynamicMaterialAmbientLighting>();
        }
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