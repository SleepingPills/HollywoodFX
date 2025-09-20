using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Particles;

public class Emitter
{
    public ParticleSystem Main;
    private List<SubEmitter> _emitters;

    public Emitter(ParticleSystem main)
    {
        Main = main;

        _emitters = [];
        foreach (var subSystem in main.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = subSystem.emission;

            if (!emission.enabled)
                continue;

            for (var i = 0; i < emission.burstCount; i++)
            {
                var burst = emission.GetBurst(i);
                _emitters.Add(
                    new SubEmitter { ParticleSystem = subSystem, MinCount = burst.minCount, MaxCount = burst.maxCount, Chance = burst.probability }
                );
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Vector3 position, Vector3 normal, float scale)
    {
        var rotation = Quaternion.LookRotation(normal);

        Main.transform.position = position;
        Main.transform.localScale = new Vector3(scale, scale, scale);
        Main.transform.rotation = rotation;

        Main.Play(true);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale)
    {
        var rotation = Quaternion.LookRotation(normal);

        Main.transform.position = position;
        Main.transform.localScale = new Vector3(scale, scale, scale);
        Main.transform.rotation = rotation;
        
        for (var i = 0; i < _emitters.Count; i++)
        {
            var emitter = _emitters[i];
            
            if (emitter.Chance < 0.99f && Random.Range(0f, 1f) > emitter.Chance)
                continue;
            
            var system = emitter.ParticleSystem;
            
            var count = Random.Range(emitter.MinCount, emitter.MaxCount);
            
            system.Emit(count);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale, int count)
    {
        var rotation = Quaternion.LookRotation(normal);

        Main.transform.position = position;
        Main.transform.localScale = new Vector3(scale, scale, scale);
        Main.transform.rotation = rotation;

        Main.Emit(count);
    }

    public void ScaleDensity(float density)
    {
        if (Mathf.Approximately(density, 1f)) return;

        foreach (var subSystem in Main.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleHelpers.ScaleEmissionRate(subSystem, density);
        }
    }
}

public struct SubEmitter
{
    public ParticleSystem ParticleSystem;
    public int MinCount;
    public int MaxCount;
    public float Chance;
}

public class EffectBundle(Emitter[] emitters)
{
    public readonly Emitter[] Emitters = emitters;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Vector3 position, Vector3 normal, float scale)
    {
        var pick = Emitters.Length == 1 ? Emitters[0] : Emitters[Random.Range(0, Emitters.Length)];
        pick.Emit(position, normal, scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale)
    {
        var pick = Emitters.Length == 1 ? Emitters[0] : Emitters[Random.Range(0, Emitters.Length)];
        pick.EmitDirect(position, normal, scale);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale, int count)
    {
        var pick = Emitters.Length == 1 ? Emitters[0] : Emitters[Random.Range(0, Emitters.Length)];
        pick.EmitDirect(position, normal, scale, count);
    }

    public void Shuffle(int count = 0)
    {
        if (count >= Emitters.Length || count <= 0)
        {
            count = Emitters.Length;
        }

        // Partial Fisher-Yates: only shuffle the first 'count' positions
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(i, Emitters.Length);
            (Emitters[i], Emitters[randomIndex]) = (Emitters[randomIndex], Emitters[i]);
        }
    }


    public static EffectBundle Merge(params EffectBundle[] bundles)
    {
        return new EffectBundle(bundles.SelectMany(b => b.Emitters).ToArray());
    }

    public static Dictionary<string, EffectBundle> LoadPrefab(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        var effectMap = new Dictionary<string, EffectBundle>();

        foreach (var (name, particleSystems) in ParticleHelpers.LoadEmitterBundles(eftEffects, prefab, dynamicAlpha))
        {
            effectMap[name] = new EffectBundle(particleSystems);
        }

        return effectMap;
    }

    public void ScaleDensity(float density)
    {
        if (Mathf.Approximately(density, 1f)) return;

        foreach (var emitter in Emitters)
        {
            emitter.ScaleDensity(density);
        }
    }
}