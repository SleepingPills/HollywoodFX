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
    // ReSharper disable InconsistentNaming
    private static void Prefix(AmmoPoolObject __instance, ref float ___float_0)
    {
        ___float_0 = Plugin.MiscShellLifetime.Value;
        __instance.Shell.transform.localScale *= Plugin.MiscShellSize.Value;
    }
}