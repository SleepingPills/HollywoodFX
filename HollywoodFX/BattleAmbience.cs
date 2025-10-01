using System.Collections.Generic;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal class BattleAmbienceEmission
{
    public float LingerTime;
    public float PuffTime;
    public int PuffCounter;
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

    public void Emit(ImpactKinetics kinetics)
    {
        // ReSharper disable once MergeSequentialChecks
        if (kinetics.Bullet.Info == null || kinetics.Bullet.Info.Player == null) return;
        
        var playerId = kinetics.Bullet.Info.Player.iPlayer.Id;
        
        if (!_emissions.TryGetValue(playerId, out var emission))
        {
            _emissions[playerId] = emission = new BattleAmbienceEmission();
        }

        if (emission.LingerTime <= Time.unscaledTime)
        {
            var lingerChance = 0.4 * (kinetics.Bullet.Energy / 2500f);

            var emitted = false;
            
            if (Random.Range(0f, 1f) < lingerChance)
            {
                _smoke.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
                emitted = true;
            }

            if (Random.Range(0f, 1f) < lingerChance)
            {
                _debris.EmitDirect(kinetics.Position, kinetics.Normal, 1f);
                emitted = true;
            }
            
            if (emitted)
            {
                emission.LingerTime = Time.unscaledTime + Random.Range(0.2f, 0.4f);
            }
        }

        var sizeScale = kinetics.Bullet.SizeScale * Plugin.EffectSize.Value * 0.75f;
        
        if (emission.PuffTime <= Time.unscaledTime)
        {
            // Emit a heavy puff
            if (kinetics.CamAngle < 160)
            {
                _puffSideHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            else
            {
                _puffFrontHeavy.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
            }
            
            emission.PuffTime = Time.unscaledTime + Random.Range(0.45f, 0.75f);
            emission.PuffCounter = 0;
        }
        else
        {
            if (emission.PuffCounter >= 2) return;
            
            // Emit a light puff and increment counter
            if (kinetics.CamAngle < 160)
                _puffSideLight.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);            
            else
                _puffFrontLight.EmitDirect(kinetics.Position, kinetics.Normal, sizeScale);
                
            emission.PuffCounter++;
        }
    }
}