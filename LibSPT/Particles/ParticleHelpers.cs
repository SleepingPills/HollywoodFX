using System.Collections.Generic;
using Comfort.Common;
using HollywoodFX.Lighting;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Particles;

public static class ParticleHelpers
{
    public static IEnumerable<(string, ParticleSystem[])> EnumerateParticleSystemBundles(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        Plugin.Log.LogInfo($"Instantiating Effects Prefab {prefab.name}");
        var rootInstance = Object.Instantiate(prefab);

        foreach (var group in rootInstance.transform.GetChildren())
        {
            var groupName = group.name;
            var effects = new List<ParticleSystem>();

            foreach (var child in group.GetChildren())
            {
                if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;

                child.parent = eftEffects.transform;
                effects.Add(particleSystem);
                
                Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, dynamicAlpha);
            }

            yield return new(groupName, effects.ToArray());
        }
    }

}