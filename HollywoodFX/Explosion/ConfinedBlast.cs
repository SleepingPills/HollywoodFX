using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    EffectBundle splashUp,
    EffectBundle splashGeneric,
    EffectBundle splashFront,
    EffectBundle trailMain,
    EffectBundle trailSparks,
    EffectBundle dust,
    EffectBundle dustRing,
    EffectBundle sparks,
    EffectBundle sparksBig
) : IBlast
{
    private bool _emitting;
    private readonly WaitForSeconds _waitEmit = new(0.115f);
    private readonly Confinement _confinement = new(GClass3449.HitMask, radius, granularity);

    public void Emit(Vector3 origin, Vector3 _)
    {
        // If we are currently emitting, do the static effects only and bail out
        if (_emitting)
        {
            MiscEffects(origin, Vector3.up);
            EmitSplash(origin, Vector3.up);
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

            // Has to be a ref otherwise the struct gets copied (since it's a value type).
            UpEffects(origin);
            ConfinedEffects(origin);
            RingEffects(origin);
            EmitSplash(origin, _confinement.Normal);

            // ConsoleScreen.Log($"Long Range cells: {_confinement.Up.Entries.Count} cells");
            // ConsoleScreen.Log($"Ring Grid cells: {_confinement.Ring.Entries.Count} cells");
            // ConsoleScreen.Log($"Confined Grid cells: {_confinement.Confined.Entries.Count} cells");

            _confinement.Clear();
        }
        finally
        {
            _emitting = false;
        }
    }

    private void UpEffects(Vector3 origin)
    {
        var count = Random.Range(10, 15);
        var samples = _confinement.Up.Pick(count);
        var sparksEmitter = new BigSparkEmitter(sparksBig);
        
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            
            sparksEmitter.Emit(sample, origin, i);
        }

        // Note: ensure that the max trail count is <= effect count
        var trailCount = Random.Range(3, 5);
        trailMain.Shuffle(trailCount);
        var trailEmitter = new TrailEmitter(trailMain, trailCount);
        trailEmitter.Emit(samples, origin);
    }

    
    private void ConfinedEffects(Vector3 origin)
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

        // Note: ensure that the max trail count is <= effect count
        var trailCount = Random.Range(1, 4);
        trailSparks.Shuffle(trailCount);
        var trailEmitter = new TrailEmitter(trailSparks, trailCount);
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

    private void EmitSplash(Vector3 origin, Vector3 normal)
    {
        var camDir = Orientation.GetCamDir(normal);

        if (camDir.IsSet(CamDir.Front))
            splashFront.Emit(origin, _confinement.Normal, 1f);
        
        // Don't emit the vertical splash in very confined spaces
        if (!(_confinement.Proximity >= 0.4f)) return;
        
        var adjNormal = Orientation.GetNormOffset(normal, camDir);
        var worldDir = Orientation.GetWorldDir(adjNormal);

        if (worldDir.IsSet(WorldDir.Up) && Random.Range(0f, 1f) <= 0.5f)
        {
            // Emit the up facing splash
            splashUp.Emit(origin, adjNormal, 1f);
        }
        else
        {
            // Emit a generic splash
            splashGeneric.Emit(origin, adjNormal, 1f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MiscEffects(Vector3 origin, Vector3 normal)
    {
        for (var i = 0; i < misc.Length; i++)
            misc[i].Emit(origin, normal, 1f);
    }
}

public readonly struct TrailEmitter(EffectBundle effect, int count)
{
    private readonly ParticleSystem[] _particles = effect.ParticleSystems;
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(List<Sample> samples, Vector3 origin)
    {
        for (var i = 0; i < Mathf.Min(count, samples.Count); i++)
        {
            var sample = samples[i];
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);
            var rotation = Quaternion.LookRotation(directionRandom);

            var pick = _particles[i];
            pick.transform.position = origin;
            pick.transform.rotation = rotation;
            pick.Play(true);
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
        var count = (int)(Random.Range(1, 3) * Plugin.ExplosionDensitySparks.Value);
        
        for (var j = 0; j < count; j++)
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
        var count = (int)(Random.Range(3, 7) * Plugin.ExplosionDensitySparks.Value);
        
        for (var j = 0; j < count; j++)
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

public readonly struct DustEmitter(EffectBundle effect, float puffPerDistance, float puffSpread, float minSpeed, float maxSpeed)
{
    private readonly float _puffSpreadInv = 1 - puffSpread;
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Sample sample, Vector3 origin, float lengthScale)
    {
        var count = (int)(sample.Magnitude / puffPerDistance * Random.Range(0.75f, 1.25f) * Plugin.ExplosionDensityDust.Value);
        var seqScaleNorm = count - 1f;
        var baseSpeed = Random.Range(minSpeed, maxSpeed);

        // Emit N puffs, scaling the speed up with each to cover the distance
        for (var j = 0; j < count; j++)
        {
            var pick = effect.ParticleSystems[Random.Range(0, effect.ParticleSystems.Length)];
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
        }
    }
}