using EFT.Ballistics;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class ImpactController
{
    private readonly BattleAmbience _battleAmbience;
    private readonly ImpactEffects _impactEffects;

    public ImpactController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Impacts Prefabs");
        var ambiencePrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Ambience");
        _battleAmbience = new BattleAmbience(eftEffects, ambiencePrefab);
        
        var impactsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Impacts");
        _impactEffects = new ImpactEffects(eftEffects, impactsPrefab);
    }

    public void Emit(ImpactContext impactContext)
    {
        var isBodyShot = (impactContext.Material is
            MaterialType.Body or MaterialType.BodyArmor or MaterialType.Helmet or MaterialType.HelmetRicochet or MaterialType.None);

        if (impactContext.IsHitPointVisible)
        {
            _impactEffects.Emit(impactContext);

            if (isBodyShot)
            {
                RagdollEffects.Apply(impactContext);
            }
            else if (Plugin.BattleAmbienceEnabled.Value)
            {
                _battleAmbience.Emit(impactContext);
            }
        }
        else
        {
            if (!isBodyShot && Plugin.BattleAmbienceEnabled.Value && impactContext.DistanceToImpact < Plugin.AmbientSimulationRange.Value)
                _battleAmbience.Emit(impactContext);
        }
    }
}