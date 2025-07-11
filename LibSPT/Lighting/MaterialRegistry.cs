using System.Collections.Generic;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Lighting;

public class MaterialRegistry
{
    public readonly List<Material> DynamicAlpha = new();
    public readonly List<Material> StaticAlpha = new();

    private void Register(Material material, bool dynamicAlpha)
    {
        
        
        if (material.shader.name != "Global Fog/Alpha Blended Lighted") return;

        Plugin.Log.LogInfo($"Registering material {material.name}");
        
        if (dynamicAlpha)
            DynamicAlpha.Add(material);
        else
            StaticAlpha.Add(material);
    }

    public void Register(Effects.Effect effect, bool dynamicAlpha)
    {
        var particleSystems = Traverse.Create(effect.BasicParticleSystemMediator).Field("_particleSystems").GetValue<ParticleSystem[]>();

        foreach (var system in particleSystems)
        {
            foreach (var renderer in system.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                if (renderer == null || renderer.material == null) continue;

                Register(renderer.material, dynamicAlpha);
            }
        }
    }
    
    public void Register(ParticleSystem system, bool dynamicAlpha)
    {
        foreach (var renderer in system.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            if (renderer == null || renderer.material == null) continue;

            Register(renderer.material, dynamicAlpha);
        }
    }
}