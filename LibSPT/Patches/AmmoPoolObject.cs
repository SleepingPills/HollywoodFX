using System.Reflection;
using EFT.AssetsManager;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

public class AmmoPoolObjectAutoDestroyPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AmmoPoolObject).GetMethod(nameof(AmmoPoolObject.StartAutoDestroyCountDown));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    private static void Postfix(AmmoPoolObject __instance, ref float ___float_0)
    {
        ___float_0 = Plugin.MiscShellLifetime.Value;
        __instance.Shell.transform.localScale *= Plugin.MiscShellSize.Value;
    }
}

public class AmmoPoolObjectEnablePhysicsPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AmmoPoolObject).GetMethod(nameof(AmmoPoolObject.EnablePhysics));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    private static void Prefix(AmmoPoolObject __instance, Vector3 force, ref Vector3 torque, Vector3 parentForce, Vector3 weaponForward)
    {
        var adjVector = -1 * __instance.transform.forward * torque.magnitude;
        torque += adjVector;
    }
}
