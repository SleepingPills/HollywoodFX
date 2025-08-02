using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Particles;

public class EffectBundle(ParticleSystem[] particleSystems)
{
    public readonly ParticleSystem[] ParticleSystems = particleSystems;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Vector3 position, Vector3 normal, float scale)
    {
        var pick = ParticleSystems[Random.Range(0, ParticleSystems.Length)];
        Singleton<EmissionController>.Instance.Emit(pick, position, normal, scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale)
    {
        var pick = ParticleSystems.Length == 1 ? ParticleSystems[0] : ParticleSystems[Random.Range(0, ParticleSystems.Length)];
        var rotation = Quaternion.LookRotation(normal);

        pick.transform.position = position;
        pick.transform.localScale = new Vector3(scale, scale, scale);
        pick.transform.rotation = rotation;

        pick.Play(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale, int count)
    {
        var pick = ParticleSystems.Length == 1 ? ParticleSystems[0] : ParticleSystems[Random.Range(0, ParticleSystems.Length)];
        var rotation = Quaternion.LookRotation(normal);

        pick.transform.position = position;
        pick.transform.localScale = new Vector3(scale, scale, scale);
        pick.transform.rotation = rotation;

        pick.Emit(count);
    }

    public static EffectBundle Merge(params EffectBundle[] bundles)
    {
        return new EffectBundle(bundles.SelectMany(b => b.ParticleSystems).ToArray());
    }

    public static Dictionary<string, EffectBundle> LoadPrefab(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        var effectMap = new Dictionary<string, EffectBundle>();

        foreach (var (name, particleSystems) in ParticleHelpers.LoadParticleSystemBundles(eftEffects, prefab, dynamicAlpha))
        {
            effectMap[name] = new EffectBundle(particleSystems);
        }

        return effectMap;
    }

    public void ScaleDensity(float density)
    {
        if (Mathf.Approximately(density, 1f)) return;
        
        foreach (var system in ParticleSystems)
        {
            foreach (var subSystem in system.GetComponentsInChildren<ParticleSystem>()) 
            {
                ParticleHelpers.ScaleEmissionRate(subSystem, density);
            }
        }
    }
}