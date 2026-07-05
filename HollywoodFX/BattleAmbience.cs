using System.Collections.Generic;
using EFT.UI;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class BattleAmbienceEmission
{
    public float EmissionTime;
    public float HeavyChance = 0.5f;
}

internal class BattleAmbience
{
    private readonly EffectBundle _smoke;
    private readonly EffectBundle _debris;

    private readonly EffectBundle _puffFrontLight;
    private readonly EffectBundle _puffFrontHeavy;
    private readonly EffectBundle _puffSideLight;
    private readonly EffectBundle _puffSideHeavy;

    private readonly Dictionary<int, BattleAmbienceEmission> _emissions = new();

    public BattleAmbience(Effects eftEffects, GameObject lingerPrefab, GameObject puffPrefab)
    {
        Plugin.Log.LogInfo("Building Battle Ambience Effects");
        var linger = EffectBundle.LoadPrefab(eftEffects, lingerPrefab, false);
        var puff = EffectBundle.LoadPrefab(eftEffects, puffPrefab, true);

        foreach (var bundle in linger.Values)
        {
            bundle.ScaleDensity(Plugin.AmbientEffectDensity.Value);
            bundle.ScaleLifetime(Plugin.AmbientParticleLifetime.Value);
            bundle.ScaleLimit(Plugin.AmbientParticleLimit.Value);
        }

        _smoke = linger["Smoke"];
        _debris = linger["Debris"];

        _puffFrontLight = puff["Puff_Smoke_Front_Light"];
        _puffFrontHeavy = puff["Puff_Smoke_Front_Heavy"];
        _puffSideLight = puff["Puff_Smoke_Side_Light"];
        _puffSideHeavy = puff["Puff_Smoke_Side_Heavy"];
    }

    public void Emit(ImpactKinetics kinetics, float baseSizeScale)
    {
        // ReSharper disable once MergeSequentialChecks
        if (kinetics.Bullet.Info == null || kinetics.Bullet.Info.Player == null) return;

        var playerId = kinetics.Bullet.Info.Player.iPlayer.Id;

        if (!_emissions.TryGetValue(playerId, out var emission))
        {
            _emissions[playerId] = emission = new BattleAmbienceEmission();
        }

        var elapsed = Time.unscaledTime - emission.EmissionTime;
        
        if (elapsed < 0) return;
        
        var lingerChance = kinetics.Bullet.Energy / 2500f;

        if (Random.Range(0f, 1f) < lingerChance)
        {
            _smoke.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
        }

        if (Random.Range(0f, 1f) < lingerChance)
        {
            _debris.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
        }
            
        var sizeScale = baseSizeScale * kinetics.Bullet.SizeScale * Plugin.EffectSize.Value;

        // If we haven't fired for more than 2.5 seconds, reset the heavy chance to default 
        if (elapsed > 2.5f)
        {
            emission.HeavyChance = 0.5f;
        }
        
        if (Random.Range(0f, 1f) < emission.HeavyChance)
        {
            if (kinetics.CamAngle < 160)
            {
                _puffSideHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            else
            {
                _puffFrontHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            
            emission.HeavyChance = Mathf.Max(emission.HeavyChance / 2f, 0.05f);
        }
        else
        {
            if (kinetics.CamAngle < 160)
            {
                _puffSideLight.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            else
            {
                _puffFrontLight.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            
            emission.HeavyChance = Mathf.Min(emission.HeavyChance + 0.1f, 1f);
        }
        
        emission.EmissionTime = Time.unscaledTime + Random.Range(0.1f, 0.3f);
    }
}