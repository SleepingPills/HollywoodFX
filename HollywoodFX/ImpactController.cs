using EFT;
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
        var ambiencePuffsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Ambience Puffs");
        _battleAmbience = new BattleAmbience(eftEffects, ambiencePrefab, ambiencePuffsPrefab);

        var impactsMainPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Impacts");
        var impactsTracerPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Impacts Tracer");

        _impactEffects = new ImpactEffects(eftEffects, impactsMainPrefab, impactsTracerPrefab);

        var bloodMainPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Main");
        var bloodSquirtsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Squirts");
        var bloodBleedPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Bleed");
        var bloodBleedoutPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Bleedout");
        var bloodFinishersPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Blood Finishers");

        _goreEffects = new GoreEffects(eftEffects, bloodMainPrefab, bloodSquirtsPrefab, bloodBleedPrefab, bloodBleedoutPrefab, bloodFinishersPrefab);
    }

    public void Emit(ImpactKinetics kinetics)
    {
        var hitColliderRoot = kinetics.Bullet.HitColliderRoot;

        // Don't render effects on the local player in first person view
        var localPlayer = ImpactStatic.LocalPlayer;
        if (hitColliderRoot != null && hitColliderRoot == localPlayer.Transform.Original && localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        var isBodyShot = kinetics.Material is MaterialType.Body or MaterialType.BodyArmor or MaterialType.Helmet or MaterialType.HelmetRicochet;

        if (kinetics.IsHitPointVisible)
        {
            _impactEffects.Emit(kinetics);
            if (isBodyShot)
                _goreEffects.Apply(kinetics);
        }

        if (kinetics.IsHitPointVisible || kinetics.DistanceToImpact < Plugin.AmbientSimulationRange.Value)
            _battleAmbience.Emit(kinetics, isBodyShot ? 0.375f : 0.75f);
    }
}