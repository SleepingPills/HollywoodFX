using System.Collections.Generic;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Lighting;

public class MaterialRegistry
{
    public readonly HashSet<int> SeenMaterials = new();
    public readonly List<Material> DynamicAlpha = new();
    public readonly List<Material> StaticAlpha = new();
    public readonly Dictionary<int, Texture> Textures = new();
    
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

    public void SetMipBias(float bias)
    {
        foreach (var texture in Textures.Values)
        {
            texture.mipMapBias = -bias;
        }
    }
    
    private void Register(Material material, bool dynamicAlpha)
    {
        RegisterTextures(material);
        
        if (material.shader.name != "Global Fog/Alpha Blended Lighted") return;

        var materialId = material.GetInstanceID();

        if (!SeenMaterials.Add(materialId))
        {
            return;
        }

        Plugin.Log.LogInfo($"Registering material {material}");
        
        if (dynamicAlpha)
            DynamicAlpha.Add(material);
        else
            StaticAlpha.Add(material);
    }

    private void RegisterTextures(Material material)
    {
        foreach (var texturePropertyId in material.GetTexturePropertyNameIDs())
        {
            var texture = material.GetTexture(texturePropertyId);
            
            if (texture == null)
                continue;
            
            var textureId = texture.GetInstanceID();

            if (Textures.ContainsKey(textureId))
            {
                continue;
            }
            
            Plugin.Log.LogInfo($"Registering texture {texture}");
            Textures.Add(textureId, texture);
        }
    }
}