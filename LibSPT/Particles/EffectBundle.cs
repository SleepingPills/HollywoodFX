using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Particles;

internal class EffectBundle(ParticleSystem[] particleSystems)
{
    private readonly ParticleSystem[] _particleSystems = particleSystems;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EmitRandom(Vector3 position, Vector3 normal, float scale)
    {
        var pick = _particleSystems[Random.Range(0, _particleSystems.Length)];
        Singleton<EmissionController>.Instance.Emit(pick, position, normal, scale);
    }

    public static EffectBundle Merge(params EffectBundle[] bundles)
    {
        return new EffectBundle(bundles.SelectMany(b => b._particleSystems).ToArray());
    }

    public static Dictionary<string, EffectBundle> LoadPrefab(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        Plugin.Log.LogInfo($"Instantiating Effects Prefab {prefab.name}");
        var rootInstance = Object.Instantiate(prefab);

        var effectMap = new Dictionary<string, EffectBundle>();

        foreach (var group in rootInstance.transform.GetChildren())
        {
            var groupName = group.name;
            var effects = new List<ParticleSystem>();

            foreach (var child in group.GetChildren())
            {
                if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;

                child.parent = eftEffects.transform;
                Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, dynamicAlpha);
                effects.Add(particleSystem);
            }

            effectMap[groupName] = new EffectBundle(effects.ToArray());
            Plugin.Log.LogInfo($"Added effect `{groupName}` with {effects.Count} particle systems");
        }

        return effectMap;
    }
}