using System.Reflection;
using EFT.AssetsManager;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodFX.Patches;

public class AmmoPoolObjectAutoDestroyPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AmmoPoolObject).GetMethod(nameof(AmmoPoolObject.StartAutoDestroyCountDown));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    private static void Prefix(AmmoPoolObject __instance)
    {
        Traverse.Create(__instance).Field("float_0").SetValue(Plugin.MiscShellLifetime.Value);
        __instance.Shell.transform.localScale *= Plugin.MiscShellSize.Value;
    }
}