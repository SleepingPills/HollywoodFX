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

public class ConfinedBlast(
    Effects eftEffects,
    float radius,
    float granularity,
    EffectBundle[] misc,
    EffectBundle splash,
    EffectBundle trail,
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
            MiscEffects(origin, Vector3.up);
            splash.Emit(origin, Vector3.up, 1f);
        }

        eftEffects.StartCoroutine(Detonate(origin));
    }

    private IEnumerator Detonate(Vector3 origin)
    {
        _emitting = true;
        
        try
        {
            _confinement.ScheduleProximity(origin);
            yield return null;
            _confinement.CompleteProximity();
            
            _confinement.ScheduleMain(origin);
            MiscEffects(origin, _confinement.Normal);
            yield return _waitEmit;
            _confinement.CompleteMain();

            var trailCount = Random.Range(3, 7);
            trail.Shuffle(trailCount);
            var trailEmitter = new TrailEmitter(trail, trailCount);
            
            // Has to be a ref otherwise the struct gets copied (since it's a value type).
            UpEffects(origin, ref trailEmitter);
            ConfinedEffects(origin, ref trailEmitter);
            RingEffects(origin);

            // Only emit the splash if we are not super confined
            if (_confinement.Proximity >= 0.75f)
            {
                splash.Emit(origin, _confinement.Normal, 1f);
            }

            ConsoleScreen.Log($"Long Range cells: {_confinement.Up.Entries.Count} cells");
            ConsoleScreen.Log($"Ring Grid cells: {_confinement.Ring.Entries.Count} cells");
            ConsoleScreen.Log($"Confined Grid cells: {_confinement.Confined.Entries.Count} cells");

            _confinement.Clear();
        }
        finally
        {
            _emitting = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpEffects(Vector3 origin, ref TrailEmitter trailEmitter)
    {
        var count = Random.Range(10, 15);
        var samples = _confinement.Up.Pick(count);
        var sparksEmitter = new BigSparkEmitter(sparksBig);
        
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            
            sparksEmitter.Emit(sample, origin, i);
        }
        
        trailEmitter.Emit(samples, origin);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConfinedEffects(Vector3 origin, ref TrailEmitter trailEmitter)
    {
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

        // Emit any leftover trails
        trailEmitter.Emit(samples, origin);
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
    private void MiscEffects(Vector3 origin, Vector3 normal)
    {
        for (var i = 0; i < misc.Length; i++)
            misc[i].Emit(origin, normal, 1f);
    }
}

public struct TrailEmitter(EffectBundle effect, int count)
{
    private int _iter = 0;
    private readonly ParticleSystem[] _particles = effect.ParticleSystems;
    private readonly int _limit = Mathf.Min(effect.ParticleSystems.Length, count);
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(List<Sample> samples, Vector3 origin)
    {
        // Iterate through the samples until we reach the target iteration count
        for (var i = 0; i < samples.Count; i++)
        {
            if (_iter >= _limit)
                break;
            
            var sample = samples[i];
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);
            var rotation = Quaternion.LookRotation(directionRandom);

            var pick = _particles[_iter];
            pick.transform.position = origin;
            pick.transform.rotation = rotation;
            pick.Play(true);
            
            ConsoleScreen.Log($"Emitted {_iter}/{_limit} sample {i}.");
            
            _iter++;
        }
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