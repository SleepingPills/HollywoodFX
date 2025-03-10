using EFT.Ballistics;
using HollywoodFX.Gore;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class ImpactController
{
    private readonly BattleAmbience _battleAmbience;
    private readonly ImpactEffects _impactEffects;
    private readonly GoreEffects _goreEffects;

    public ImpactController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Impacts Prefabs");
        var ambiencePrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Ambience");
        _battleAmbience = new BattleAmbience(eftEffects, ambiencePrefab);

        var impactsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Impacts");
        _impactEffects = new ImpactEffects(eftEffects, impactsPrefab);

        var bloodMainPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Main");
        var bloodSquirtsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Squirts");
        var bloodFinishersPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Finishers");

        _goreEffects = new GoreEffects(eftEffects, bloodMainPrefab, bloodSquirtsPrefab, bloodFinishersPrefab);
    }

    public void Emit(ImpactKinetics kinetics)
    {
        var isBodyShot = (kinetics.Material is
            MaterialType.Body or MaterialType.BodyArmor or MaterialType.Helmet or MaterialType.HelmetRicochet);

        if (kinetics.IsHitPointVisible)
        {
            _impactEffects.Emit(kinetics);
            if (isBodyShot)
                _goreEffects.Apply(kinetics);
        }

        if (Plugin.BattleAmbienceEnabled.Value
            && !isBodyShot
            && (kinetics.IsHitPointVisible || kinetics.DistanceToImpact < Plugin.AmbientSimulationRange.Value))
            _battleAmbience.Emit(kinetics);
    }
}