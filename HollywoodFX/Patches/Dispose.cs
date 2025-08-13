using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Decal;
using HollywoodFX.Gore;
using HollywoodFX.Lighting;
using HollywoodFX.Muzzle;
using HollywoodFX.Muzzle.Patches;
using HollywoodFX.Particles;
using SPT.Reflection.Patching;

namespace HollywoodFX.Patches;

public class GameWorldDisposePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.Dispose));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix()
    {
        Plugin.Log.LogInfo("Disposing of static & long lived objects.");
        
        Singleton<DecalPainter>.Release(Singleton<DecalPainter>.Instance);
        Singleton<ImpactController>.Release(Singleton<ImpactController>.Instance);
        Singleton<EmissionController>.Release(Singleton<EmissionController>.Instance);
        Singleton<BloodEffects>.Release(Singleton<BloodEffects>.Instance);
        Singleton<PlayerDamageRegistry>.Release(Singleton<PlayerDamageRegistry>.Instance);
        Singleton<MaterialRegistry>.Release(Singleton<MaterialRegistry>.Instance);
        
        Singleton<LocalPlayerMuzzleEffects>.Release(Singleton<LocalPlayerMuzzleEffects>.Instance);
        Singleton<MuzzleEffects>.Release(Singleton<MuzzleEffects>.Instance);
        Singleton<FirearmsEffectsCache>.Release(Singleton<FirearmsEffectsCache>.Instance);
        Singleton<MuzzleStatic>.Release(Singleton<MuzzleStatic>.Instance);

        ImpactStatic.Kinetics = new ImpactKinetics();
        ImpactStatic.LocalPlayer = null;
        ImpactStatic.PlayerHitInfo = null;
        
        Plugin.Log.LogInfo("Disposing complete.");
    }
}
