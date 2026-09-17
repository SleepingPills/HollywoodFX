using System.Reflection;
using EFT.AssetsManager;
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
    private static void Postfix(AmmoPoolObject __instance, ref float ___c)
    {
        ___c = Plugin.MiscShellLifetime.Value;
        __instance.Shell.transform.localScale *= Plugin.MiscShellSize.Value;
    }
}
