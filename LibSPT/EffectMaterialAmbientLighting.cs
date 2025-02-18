using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.Weather;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

internal static class MaterialFinder
{
    public static IEnumerable<Material> FindEffectMaterials()
    {
        foreach (var effect in Singleton<Effects>.Instance.EffectsArray)
        {
            if (effect.BasicParticleSystemMediator == null)
                continue;
            
            var particleSystems = Traverse.Create(effect.BasicParticleSystemMediator).Field("_particleSystems").GetValue<ParticleSystem[]>();
            
            foreach (var system in particleSystems)
            {
                foreach (var renderer in system.GetComponentsInChildren<ParticleSystemRenderer>())
                {
                    if (renderer == null || renderer.material == null) continue;
                    yield return renderer.material;
                }
            }
        }
    }
}

public class DynamicMaterialAmbientLighting : Component
{
    private List<Material> _materials;
    private List<Vector4> _tintColors;
    private List<Vector4> _ambientLightColors;

    private GameWorld _gameWorld;
    private IWeatherCurve _weatherCurve;

    private int tintColorId;
    private int ambientLightColorId;

    private const float LightIntensityThreshold = 0.25f;
    private float _lightIntensity;

    private const float RepeatRate = 5f;
    private float _timer;

    public void Awake()
    {
        _materials = [];

        foreach (var material in MaterialFinder.FindEffectMaterials())
        {
            if (material.shader.name == "Global Fog/Alpha Blended Lighted")
            {
                _materials.Add(material);
            }
        }

        tintColorId = Shader.PropertyToID("_TintColor");
        ambientLightColorId = Shader.PropertyToID("_LocalMinimalAmbientLight");

        Plugin.Log.LogInfo($"Found {_materials.Count} materials with ambient lighting parameters");

        _tintColors = new List<Vector4>(_materials.Count);
        _ambientLightColors = new List<Vector4>(_materials.Count);

        foreach (var material in _materials)
        {
            _tintColors.Add(material.GetVector(tintColorId));
            _ambientLightColors.Add(material.GetVector(ambientLightColorId));
        }

        _gameWorld = Singleton<GameWorld>.Instance;

        var weatherController = GameObject.Find("Weather").GetComponent<WeatherController>();
        _weatherCurve = weatherController.WeatherCurve;
    }

    public void Update()
    {
        if (_timer <= 0)
        {
            ConsoleScreen.Log($"Cloudiness: {_weatherCurve.Cloudiness} {_gameWorld.GameDateTime.DateTime_0} {_gameWorld.GameDateTime.DateTime_1}");

            var currentLightIntensity = CalculateAmbientLightIntensity();
            if (Mathf.Abs(currentLightIntensity - _lightIntensity) > LightIntensityThreshold)
            {
                // TODO Update materials
                _lightIntensity = currentLightIntensity;
            }

            _timer = RepeatRate;
        }

        _timer -= Time.deltaTime;
    }

    private float CalculateAmbientLightIntensity()
    {
        return 4f;
    }
}

public static class StaticMaterialAmbientLighting
{
    public static void AdjustLighting(string location)
    {
        var tintColorFactor = new Vector4(1f, 1f, 1f, 1f);
        var ambientLightFactor = new Vector4(0f, 0f, 0f, 0f);

        switch (location)
        {
            case "factory4_day":
                tintColorFactor = new Vector4(0.6f, 0.6f, 0.6f, 1.75f);
                ambientLightFactor = new Vector4(0f, 0f, 0f, 1f);
                break;
            case "factory4_night":
                tintColorFactor = new Vector4(0.5f, 0.5f, 0.5f, 1.75f);
                ambientLightFactor = new Vector4(1.5f, 1.5f, 1.5f, 1f);
                break;
            default:
                Plugin.Log.LogError($"Unknown factory location: {location}");
                break;
        }

        foreach (var material in Singleton<LitMaterialRegistry>.Instance.DynamicAlpha)
        {
            ApplyScaling(material, tintColorFactor, ambientLightFactor);
        }

        // Alpha channel scaling to 1 as we don't want to upscale alpha on these effects
        tintColorFactor.w = 1f;
        foreach (var material in Singleton<LitMaterialRegistry>.Instance.StaticAlpha)
        {
            ApplyScaling(material, tintColorFactor, ambientLightFactor);
        }
    }

    private static void ApplyScaling(Material material, Vector4 tintColorFactor, Vector4 ambientLightFactor)
    {
        var tintColor = material.GetVector("_TintColor");
        var ambientLightColor = material.GetVector("_LocalMinimalAmbientLight");

        var newTintColor = Vector4.Scale(tintColor, tintColorFactor);
        var newAmbientLightColor = Vector4.Scale(ambientLightColor, ambientLightFactor);

        material.SetVector("_TintColor", newTintColor);
        material.SetVector("_LocalMinimalAmbientLight", newAmbientLightColor);
        Plugin.Log.LogInfo($"Adjusting material: {material.name} Tint: {tintColor} -> {newTintColor} Ambient: {ambientLightColor} -> {newAmbientLightColor}");
    }
}