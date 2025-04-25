using System.Reflection;
using EFT;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

public class ShellOnBouncePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Shell).GetMethod(nameof(Shell.OnBounce));
    }

    [PatchPrefix]
    public static bool Prefix(Shell __instance, Collider collider, ref Vector3 ___vector3_2, ECaliber ____caliber)
    {
        // Only does some debug crap and can be safely ignored
        // base.OnBounce(collider);

        ___vector3_2 = 2.5f * __instance.VelocitySqrMagnitude * new Vector3(
            EFTHardSettings.Instance.Shells.ReboundRotationX.Random(true),
            EFTHardSettings.Instance.Shells.ReboundRotationY.Random(true),
            EFTHardSettings.Instance.Shells.ReboundRotationZ.Random(true)
        );
        
        if (__instance.CollisionListener == null)
            return false;
        
        if (__instance.VelocitySqrMagnitude < 1.0f)
            return false;

        var component = collider.gameObject.GetComponent<BallisticCollider>();

        var position = __instance.transform.position;
        var material = component != null ? component.GetSurfaceSound(position) : BaseBallistic.ESurfaceSound.Soil;

        __instance.CollisionListener.InvokeShellCollision(position, material, ____caliber);

        return false;
    }
}