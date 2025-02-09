using System.Reflection;
using SPT.Reflection.Patching;
using Systems.Effects;

namespace HollywoodFX.Patches
{
    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EffectsCommutator).GetMethod(nameof(EffectsCommutator.PlayHitEffect));
        }

        [PatchPrefix]
        public static void Prefix(EftBulletClass info, ShotInfoClass playerHitInfo)
        {
            ImpactController.Instance.BulletInfo = info;
            ImpactController.Instance.PlayerHitInfo = playerHitInfo;
        }
    }
}
