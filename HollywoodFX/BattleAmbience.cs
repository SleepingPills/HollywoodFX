using System.Collections.Generic;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class BattleAmbienceEmission
{
    public float EmissionTime;
    public float PuffHeavyChance = 0.75f;
    public float LingerChance = 1f;
}

internal class BattleAmbience
{
    private readonly EffectBundle _haze;
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

        _haze = linger["Smoke"];
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
        
        switch (elapsed)
        {
            case < 0:
                return;
            case > 2.5f:
            {
                // If we haven't fired for more than 2.5 seconds, reset the heavy chance to default 
                emission.PuffHeavyChance = 0.75f;
            
                // If we haven't fired for more than 7.5 seconds, also reset the haze chance
                if (elapsed > 7.5f)
                {
                    emission.LingerChance = 1f;
                }

                break;
            }
        }
        
        var bulletChanceScale = kinetics.Bullet.Energy / 2500f;

        if (Random.Range(0f, 1f) < emission.LingerChance * bulletChanceScale)
        {
            _haze.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
            _debris.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
            emission.LingerChance = Mathf.Max(emission.LingerChance / 3f, 0.05f);
        }
        else
        {
            emission.LingerChance = Mathf.Min(emission.LingerChance + 0.05f, 1f);
        }
            
        var sizeScale = baseSizeScale * kinetics.Bullet.SizeScale * Plugin.EffectSize.Value;
        
        if (Random.Range(0f, 1f) < emission.PuffHeavyChance)
        {
            if (kinetics.CamAngle < 160)
            {
                _puffSideHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            else
            {
                _puffFrontHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            
            emission.PuffHeavyChance = Mathf.Max(emission.PuffHeavyChance / 3f, 0.05f);
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
            
            emission.PuffHeavyChance = Mathf.Min(emission.PuffHeavyChance + 0.1f, 1f);
        }
        
        emission.EmissionTime = Time.unscaledTime + Random.Range(0.1f, 0.3f);
    }
}