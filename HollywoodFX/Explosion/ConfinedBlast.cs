using System;
using System.Collections;
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
    EffectBundle[] premade,
    EffectBundle dust,
    EffectBundle dustRing,
    EffectBundle sparks
)
{
    private bool _emitting = false;
    private readonly WaitForSeconds _waitEmit = new(0.115f);
    private readonly Confinement _confinement = new(GClass3449.HitMask, radius, granularity);

    public void Emit(Vector3 origin)
    {
        // If we are emitting, do the static effects only and bail out
        if (_emitting)
            EmitStatic(origin);

        eftEffects.StartCoroutine(Detonate(origin));
    }

    private IEnumerator Detonate(Vector3 origin)
    {
        _emitting = true;

        try
        {
            _confinement.Schedule(origin, Vector3.up);

            EmitStatic(origin);

            yield return _waitEmit;

            _confinement.Complete();

            foreach (var coords in _confinement.Up.Entries)
            {
                var cell = _confinement.Up.Cells[coords.x, coords.y, coords.z];
                var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);

                // ConsoleScreen.Log($"Long Range Cell pos: {cell.Position} aabb pos: {cell.Bounds.center} aabb size: {cell.Bounds.size.magnitude} count: {cell.Count}");
                // DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(countScale, 0, 0));
            }

            // TODO: pass in the shortfall in emitting long range effects (trails, sparks) and emit some through this if possible.
            ConfinedEffects(origin);

            RingEffects(origin);

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
    private void ConfinedEffects(Vector3 origin)
    {
        if (_confinement.Confined.Entries.Count > 35)
            return;

        var pickCount = _confinement.Confined.Sample(Random.Range(15, 25));

        EmitDust(dust, _confinement.Confined, pickCount, origin, 1f, 0.75f);
    }

    private void RingEffects(Vector3 origin)
    {
        if (_confinement.Confined.Entries.Count < 10 && _confinement.Up.Entries.Count < 6)
        {
            EmitSparks(_confinement.Ring, origin);
        }

        EmitDust(dustRing, _confinement.Ring, _confinement.Ring.Entries.Count, origin, 2f, 0.5f, minSpeed: 30f, maxSpeed: 45);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EmitStatic(Vector3 origin)
    {
        for (var i = 0; i < premade.Length; i++)
            premade[i].Emit(origin, Vector3.up, 1f);
    }

    private void EmitDust(
        EffectBundle effect, Grid grid, int count, Vector3 origin, float puffPerDistance = 1f, float puffSpread = 0.5f,
        float minSpeed = 50f, float maxSpeed = 60f
    )
    {
        const float randomDegrees = 2.5f;

        var puffSpreadInv = 1 - puffSpread;
        var state = (ulong)Random.Range(int.MinValue, int.MaxValue);

        for (var i = 0; i < count; i++)
        {
            var baseSpeed = Random.Range(minSpeed, maxSpeed);

            var coords = grid.Entries[i];
            var cell = grid.Cells[coords.x, coords.y, coords.z];

            var direction = cell.Position - origin;
            var directionNormalized = direction.normalized;
            var magnitude = direction.magnitude;
            var lengthScale = Mathf.InverseLerp(0f, radius, magnitude);

            var puffPerCell = Mathf.RoundToInt(magnitude / puffPerDistance) - 1 + (int)(state % 3);
            var seqScaleNorm = puffPerCell - 1f;

            // Emit N puffs, scaling the speed up with each to cover the distance
            for (var j = 0; j < puffPerCell; j++)
            {
                var pick = effect.ParticleSystems[state % (ulong)effect.ParticleSystems.Length];
                // Scaler as a function of the puff sequence. We start with the slowest puff travelling the shortest distance and end with the fastest.
                // NB: apply an sqrt to account for the nonlinear damping effect. 50% speed travels less than 50% of the distance of 100% speed.
                var seqScale = Mathf.Sqrt(puffSpreadInv + puffSpread * Mathf.InverseLerp(0f, seqScaleNorm, j));
                // Add a bit of randomness to the direction
                var directionRandom = VectorMath.AddRandomRotation(directionNormalized, randomDegrees);

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = directionRandom * (baseSpeed * lengthScale * seqScale),
                };
                pick.Emit(emitParams, 1);

                // xorshift algo: https://en.wikipedia.org/wiki/Xorshift
                state ^= state << 7;
                state ^= state >> 9;
            }
        }
    }

    private void EmitSparks(Grid grid, Vector3 origin)
    {
        const float randomDegrees = 5f;

        for (var i = 0; i < grid.Entries.Count; i++)
        {
            var coords = grid.Entries[i];
            var cell = grid.Cells[coords.x, coords.y, coords.z];

            var direction = cell.Position - origin;
            var directionNormalized = direction.normalized;
            var magnitude = direction.magnitude;
            var lengthScale = Mathf.InverseLerp(0f, radius, magnitude);

            for (var j = 0; j < Random.Range(3, 7); j++)
            {
                var pick = sparks.ParticleSystems[(i + j) % sparks.ParticleSystems.Length];

                // Add a bit of randomness to the direction
                var directionRandom = VectorMath.AddRandomRotation(directionNormalized, randomDegrees);

                var baseSpeed = Random.Range(10f, 40f);

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = directionRandom * (baseSpeed * lengthScale),
                };
                pick.Emit(emitParams, 1);
            }
        }
    }
}