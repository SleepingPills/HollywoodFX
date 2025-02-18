using System.Reflection;
using Comfort.Common;
using EFT;
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
        Singleton<LitMaterialRegistry>.Create(new LitMaterialRegistry());
        Plugin.Log.LogInfo($"Game world hideout flag: {IsHideout}");
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
        
        if (__instance.LocationId.Contains("factory"))
        {
            Plugin.Log.LogInfo($"Factory location detected, applying static lighting");
            StaticMaterialAmbientLighting.AdjustLighting(__instance.LocationId);
        }
        else
        {
            __instance.gameObject.AddComponent<DynamicMaterialAmbientLighting>();
        }
    }
}
