using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using UnityEngine;

namespace HollywoodFX.Particles;

internal class EffectBundle(ParticleSystem[] particleSystems)
{
    internal readonly ParticleSystem[] ParticleSystems = particleSystems;

    public void EmitRandom(Vector3 position, Vector3 normal, float scale)
    {
        var pick = ParticleSystems[Random.Range(0, ParticleSystems.Length)];
        Singleton<EmissionController>.Instance.Emit(pick, position, normal, scale);
    }

    public static EffectBundle Merge(params EffectBundle[] bundles)
    {
        return new EffectBundle(bundles.SelectMany(b => b.ParticleSystems).ToArray());
    }
}