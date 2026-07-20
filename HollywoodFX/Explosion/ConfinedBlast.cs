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
    float spacing,
    EffectBundle[] misc,
    EffectBundle splashGeneric,
    EffectBundle splashDust,
    EffectBundle trailSparks,
    EffectBundle dust
) : IBlast
{
    private bool _emitting;
    private readonly WaitForSeconds _waitEmit = new(0.115f);
    private readonly Confinement _confinement = new(LayerMasksDataAbstractClass.HitMask, radius, spacing);

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
            
            EmitSplash(origin, _confinement.Normal);
            ConfinedEffects(origin);

            // ConsoleScreen.Log($"Long Range cells: {_confinement.Up.Entries.Count} cells");
            // ConsoleScreen.Log($"Ring Grid cells: {_confinement.Ring.Entries.Count} cells");
            // ConsoleScreen.Log($"Confined Grid cells: {_confinement.Confined.Entries.Count} cells");

            // DebugGizmos.Line(origin, origin + 5f * _confinement.Normal, expiretime: 30f, color: new Color(0f, 1f, 0f));
            //
            // for (var i = 0; i < 20; i++)
            // {
            //     var adj = VectorMath.AddRandomRotation(_confinement.Normal, 2.5f);
            //     DebugGizmos.Line(origin, origin + 5f * adj, expiretime: 30f, color: new Color(0f, 0f, 1f));
            // }

            _confinement.Clear();
        }
        finally
        {
            _emitting = false;
        }
    }
    
    private void ConfinedEffects(Vector3 origin)
    {
        var confinementScaling = 0.35f + 0.65f * Mathf.InverseLerp(0, _confinement.ConfinedNorm, _confinement.Confined.Entries.Count);
        
        var samples = _confinement.Confined.Pick(Random.Range(7, 10));

        var dustEmitter = new DustEmitter(dust, 0.5f * confinementScaling, 0.75f, 50f, 60f);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            var lengthScale = Mathf.InverseLerp(0f, radius, sample.Magnitude);
            dustEmitter.Emit(sample, origin, lengthScale);
        }

        // Note: ensure that the max trail count is <= effect count
        var trailCount  = Random.Range(4, 6);
        trailSparks.Shuffle(trailCount);
        var trailSparksEmitter = new TrailEmitter(trailSparks, trailCount);
        trailSparksEmitter.Emit(samples, origin);
    }
    
    private void EmitSplash(Vector3 origin, Vector3 normal)
    {
        var camDir = Orientation.GetCamDir(_confinement.Normal);

        splashDust.Emit(origin, normal, 1f);

        if (camDir.Item1.IsSet(CamDir.Front))
            return;
        
        // Don't emit the vertical splash in very confined spaces or directly head on
        if (!(_confinement.Proximity >= 0.4f) || !camDir.Item1.IsSet(CamDir.Angled)) return;

        var adjNormal = Orientation.GetNormOffset(normal, camDir.Item1);

        splashGeneric.Emit(origin, adjNormal, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MiscEffects(Vector3 origin, Vector3 normal)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < misc.Length; i++)
        {
            misc[i].Emit(origin, normal, 1f);
        }
    }
}

public readonly struct TrailEmitter(EffectBundle effect, int count)
{
    private readonly Particles.Emitter[] _particles = effect.Emitters;
    private const float RandomDegrees = 2.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(List<Sample> samples, Vector3 origin)
    {
        for (var i = 0; i < Mathf.Min(count, samples.Count); i++)
        {
            var sample = samples[i];
            var directionRandom = VectorMath.AddRandomRotation(sample.Direction, RandomDegrees);
            var rotation = Quaternion.LookRotation(directionRandom);

            var pick = _particles[i].Main;
            pick.transform.position = origin;
            pick.transform.rotation = rotation;
            pick.Play(true);
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
            var pick = effect.Emitters[Random.Range(0, effect.Emitters.Length)].Main;
            // Scaler as a function of the puff sequence. We start with the slowest puff traveling the shortest distance and end with the fastest.
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