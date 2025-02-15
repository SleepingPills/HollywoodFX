using System.Reflection;
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
        Plugin.Log.LogInfo($"Game World Awake Patch: Game world is {__instance}, hideout flag: {IsHideout}");
    }
}