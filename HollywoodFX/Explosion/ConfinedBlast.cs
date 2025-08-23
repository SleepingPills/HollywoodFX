using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.UI;
using HollywoodFX.Helpers;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Explosion;

/*
 * TODO:
 * Ring will contain dust puffs.
 * Weight the amount of puffs and speed based on the length of the cell vector. Cell vectors > than 90% of the radius will get a speed boost. We want to
 * emit about 1 puff per 1.5 meters of size.
 * We'll emit puffs for each ring cell, and we'll pick randomly from the 3 available puffs (or just rotate through them).
 *
 */

public class ConfinedBlast(
    Effects eftEffects,
    float radius,
    float granularity,
    EffectBundle[] misc,
    EffectBundle splash,
    EffectBundle debrisGlow,
    EffectBundle dust,
    EffectBundle dustRing,
    EffectBundle sparks,
    EffectBundle sparksBig
)
{
    private bool _emitting;
    private readonly WaitForSeconds _waitEmit = new(0.115f);
    private readonly Confinement _confinement = new(GClass3449.HitMask, radius, granularity);

    public void Emit(Vector3 origin)
    {
        // If we are emitting, do the static effects only and bail out
        if (_emitting)
        {
            MiscEffects(origin);
            splash.Emit(origin, Vector3.up, 1f);
        }

        eftEffects.StartCoroutine(Detonate(origin));
    }

    private IEnumerator Detonate(Vector3 origin)
    {
        _emitting = true;

        try
        {
            _confinement.Schedule(origin, Vector3.up);

            MiscEffects(origin);

            yield return _waitEmit;

            _confinement.Complete();

            var shortfall = UpEffects(origin);
            ConfinedEffects(origin, shortfall);
            RingEffects(origin);

            if (_confinement.Confined.Entries.Count >= 10 && _confinement.Up.Entries.Count >= 6)
            {
                // Only emit the splash if we are not super confined
                splash.Emit(origin, Vector3.up, 1f);
            }

            ConsoleScreen.Log($"Long Range cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Up.Entries.Count} cells");
            ConsoleScreen.Log($"Ring Grid cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Ring.Entries.Count} cells");
            ConsoleScreen.Log($"Confined Grid cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Confined.Entries.Count} cells");

            _confinement.Clear();
        }
        finally
        {
            _emitting = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UpEffects(Vector3 origin)
    {
        var count = Random.Range(3, 7);
        var samples = _confinement.Up.Pick(count);
        var debrisEmitter = new ChunkEmitter(debrisGlow);
        var sparksEmitter = new BigSparkEmitter(sparksBig);
        
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            
            debrisEmitter.Emit(sample, origin);
            sparksEmitter.Emit(sample, origin, i);
        }
        
        return samples.Count - count;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConfinedEffects(Vector3 origin, int shortfall)
    {
        if (_confinement.Confined.Entries.Count > 35)
            return;

        var samples = _confinement.Confined.Pick(Random.Range(15, 25));
        var dustEmitter = new DustEmitter(dust, 1.5f, 0.75f, 50f, 60f);
        var sparksEmitter = new BigSparkEmitter(sparksBig);
        
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            
            var lengthScale = Mathf.InverseLerp(0f, radius, sample.Magnitude);
            
            dustEmitter.Emit(sample, origin, lengthScale);
            sparksEmitter.Emit(sample, origin, i);
        }

        // Bail out if we don't have a shortfall
        if (shortfall <=0)
            return;
        
        var debrisEmitter = new ChunkEmitter(debrisGlow);
        
        for (var i = 0; i < Mathf.Min(shortfall, samples.Count); i++)
        {
            var sample = samples[i];
            debrisEmitter.Emit(sample, origin);
        }
    }

    private void RingEffects(Vector3 origin)
    {
        var samples = _confinement.Ring.Pick();
        var dustEmitter = new DustEmitter(dustRing, 2f, 0.5f, 30f, 45f);
        var sparksEmitter = new SmallSparkEmitter(sparks);
        
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            
            var lengthScale = Mathf.InverseLerp(0f, radius, sample.Magnitude);
            
            dustEmitter.Emit(sample, origin, lengthScale);
            sparksEmitter.Emit(sample, origin, lengthScale, i);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MiscEffects(Vector3 origin)
    {
        for (var i = 0; i < misc.Length; i++)
            misc[i].Emit(origin, Vector3.up, 1f);
    }
}

public readonly struct ChunkEmitter(EffectBundle effect)
{
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Sample sample, Vector3 origin)
    {
        var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);
        effect.EmitDirect(origin, directionRandom, 1f, 1);
    }
}

public readonly struct BigSparkEmitter(EffectBundle effect)
{
    private const float RandomDegrees = 2.5f;
    private const float MinSpeed = 50f;
    private const float MaxSpeed = 80f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Sample sample, Vector3 origin, int pickSeed)
    {
        for (var j = 0; j < Random.Range(1, 3); j++)
        {
            var pick = effect.ParticleSystems[(pickSeed + j) % effect.ParticleSystems.Length];
            
            // Add a bit of randomness to the direction
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);

            var baseSpeed = Random.Range(MinSpeed, MaxSpeed);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = origin,
                velocity = directionRandom * baseSpeed,
            };

            pick.Emit(emitParams, 1);
        }
    }
}

public readonly struct SmallSparkEmitter(EffectBundle effect)
{
    private const float RandomDegrees = 2.5f;
    private const float MinSpeed = 50f;
    private const float MaxSpeed = 80f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Sample sample, Vector3 origin, float lengthScale, int pickSeed)
    {
        for (var j = 0; j < Random.Range(3, 7); j++)
        {
            var pick = effect.ParticleSystems[(pickSeed + j) % effect.ParticleSystems.Length];

            // Add a bit of randomness to the direction
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);

            var baseSpeed = Random.Range(MinSpeed, MaxSpeed);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = origin,
                velocity = directionRandom * (baseSpeed * lengthScale),
            };
            
            pick.Emit(emitParams, 1);
        }
    }
}

public struct DustEmitter(EffectBundle effect, float puffPerDistance, float puffSpread, float minSpeed, float maxSpeed)
{
    private ulong _randState = (ulong)Random.Range(int.MinValue, int.MaxValue);
    private readonly float _puffSpreadInv = 1 - puffSpread;
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Sample sample, Vector3 origin, float lengthScale)
    {
        var baseSpeed = Random.Range(minSpeed, maxSpeed);
        var puffPerCell = Mathf.RoundToInt(sample.Magnitude / puffPerDistance) - 1 + (int)(_randState % 3);
        var seqScaleNorm = puffPerCell - 1f;
        
        // Emit N puffs, scaling the speed up with each to cover the distance
        for (var j = 0; j < puffPerCell; j++)
        {
            var pick = effect.ParticleSystems[_randState % (ulong)effect.ParticleSystems.Length];
            // Scaler as a function of the puff sequence. We start with the slowest puff travelling the shortest distance and end with the fastest.
            // NB: apply an sqrt to account for the nonlinear damping effect. 50% speed travels less than 50% of the distance of 100% speed.
            var seqScale = Mathf.Sqrt(_puffSpreadInv + puffSpread * Mathf.InverseLerp(0f, seqScaleNorm, j));
            // Add a bit of randomness to the direction
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);

            var emitParams = new ParticleSystem.EmitParams
            {
                position = origin,
                velocity = directionRandom * (baseSpeed * lengthScale * seqScale),
            };
            pick.Emit(emitParams, 1);

            // xorshift algo: https://en.wikipedia.org/wiki/Xorshift
            _randState ^= _randState << 7;
            _randState ^= _randState >> 9;
        }
    }
}