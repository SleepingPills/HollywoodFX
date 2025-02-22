using System.Reflection;
using Comfort.Common;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace HollywoodFX.Patches;

public class BulletImpactPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(EffectsCommutator).GetMethod(nameof(EffectsCommutator.PlayHitEffect));
    }

    [PatchPrefix]
    public static void Prefix(EftBulletClass info, ShotInfoClass playerHitInfo)
    {
        ImpactStatic.BulletInfo = info;
        ImpactStatic.PlayerHitInfo = playerHitInfo;
    }
}