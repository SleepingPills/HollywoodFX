using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Comfort.Common;
using HollywoodFX.Lighting;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace HollywoodFX.Particles;

internal class EffectBundle(ParticleSystem[] particleSystems)
{
    private readonly ParticleSystem[] _particleSystems = particleSystems;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(Vector3 position, Vector3 normal, float scale)
    {
        var pick = _particleSystems[Random.Range(0, _particleSystems.Length)];
        Singleton<EmissionController>.Instance.Emit(pick, position, normal, scale);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale)
    {
        var pick = _particleSystems.Length == 1 ? _particleSystems[0] : _particleSystems[Random.Range(0, _particleSystems.Length)];
        var rotation = Quaternion.LookRotation(normal);
        
        pick.transform.position = position;
        pick.transform.localScale = new Vector3(scale, scale, scale);
        pick.transform.rotation = rotation;
        
        pick.Play(true);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitDirect(Vector3 position, Vector3 normal, float scale, int count)
    {
        var pick = _particleSystems.Length == 1 ? _particleSystems[0] : _particleSystems[Random.Range(0, _particleSystems.Length)];
        var rotation = Quaternion.LookRotation(normal);
        
        pick.transform.position = position;
        pick.transform.localScale = new Vector3(scale, scale, scale);
        pick.transform.rotation = rotation;

        pick.Emit(count);
    }

    public void SetParent(Transform parent)
    {
        for (var i = 0; i < _particleSystems.Length; i++)
        {
            var particleSystem = _particleSystems[i];
            particleSystem.transform.SetParent(parent);
        }
    }
    
    public static EffectBundle Merge(params EffectBundle[] bundles)
    {
        return new EffectBundle(bundles.SelectMany(b => b._particleSystems).ToArray());
    }

    public static Dictionary<string, EffectBundle> LoadPrefab(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        var effectMap = new Dictionary<string, EffectBundle>();

        foreach (var (name, particleSystems) in ParticleHelpers.EnumerateParticleSystemBundles(eftEffects, prefab, dynamicAlpha))
        {
            effectMap[name] = new EffectBundle(particleSystems); 
            
            Plugin.Log.LogInfo($"Added effect `{name}` with {particleSystems.Length} particle systems");
        }
        
        return effectMap;
    }
}