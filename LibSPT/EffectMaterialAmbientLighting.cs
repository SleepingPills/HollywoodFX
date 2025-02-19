using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.Weather;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

public class DynamicMaterialAmbientLighting : MonoBehaviour
{
    private List<Material> _materials;
    private List<Vector4> _ambientLightColors;

    private GameWorld _gameWorld;
    private WeatherController _weatherController;
    private IWeatherCurve _weatherCurve;

    private int _tintColorId;
    private int _ambientLightColorId;

    private const float MaxLightingBoost = 0.25f;
    private const float AlphaFactor = 1.5f;

    private const float FactorChangeThreshold = 0.25f;
    private float _lightingFactor;


    private const float RepeatRate = 5f;
    private float _timer;

    public void Awake()
    {
        _tintColorId = Shader.PropertyToID("_TintColor");
        _ambientLightColorId = Shader.PropertyToID("_LocalMinimalAmbientLight");
        
        _materials = [];

        foreach (var material in Singleton<LitMaterialRegistry>.Instance.DynamicAlpha)
        {
            // Pre-multiply Alpha for non-blood effects
            if (!material.name.ToLower().Contains("blood"))
            {
                var tintColor = Vector4.Scale(material.GetVector(_tintColorId), new Vector4(1f, 1f, 1f, AlphaFactor));
                material.SetVector(_tintColorId, tintColor);
            }
            _materials.Add(material);
        }

        foreach (var material in Singleton<LitMaterialRegistry>.Instance.StaticAlpha)
        {
            _materials.Add(material);
        }

        Plugin.Log.LogInfo($"Found {_materials.Count} materials with ambient lighting parameters");

        _ambientLightColors = new List<Vector4>(_materials.Count);

        foreach (var material in _materials)
        {
            _ambientLightColors.Add(material.GetVector(_ambientLightColorId));
        }

        _gameWorld = Singleton<GameWorld>.Instance;
        _weatherController = GameObject.Find("Weather").GetComponent<WeatherController>();
        _weatherCurve = _weatherController.WeatherCurve;

        _lightingFactor = CalculateLightingFactor();
        UpdateMaterials();
        Plugin.Log.LogInfo($"Initialized lighting factor to {_lightingFactor}");
    }

    public void Update()
    {
        if (_timer <= 0)
        {
            ConsoleScreen.Log(
                $"Cloudiness: {_weatherCurve.Cloudiness} Sunheight: {_weatherController.SunHeight} DT: {_gameWorld.GameDateTime.DateTime_0} {_gameWorld.GameDateTime.DateTime_1}");

            var currentLightingFactor = CalculateLightingFactor();

            if (Mathf.Abs(currentLightingFactor - _lightingFactor) > FactorChangeThreshold)
            {
                ConsoleScreen.Log($"Lighting factor threshold breached: {_lightingFactor} -> {currentLightingFactor}");

                // Average out changes to smooth transitions until we get past the threshold
                _lightingFactor = (_lightingFactor + currentLightingFactor) / 2f;
                UpdateMaterials();
            }
            else
            {
                ConsoleScreen.Log($"Lighting factor within threshold: {_lightingFactor} -> {currentLightingFactor}");
            }

            _timer = RepeatRate;
        }

        _timer -= Time.deltaTime;
    }

    private float CalculateLightingFactor()
    {
        var dayLight = Mathf.Sqrt(Mathf.Max(_weatherController.SunHeight, 0f));
        // Clouds will really only factor into the equation during daytime. At night, the cloud factor is 0.
        var cloudFactor = dayLight * Mathf.Max(_weatherCurve.Cloudiness, 0f);
        // Full night will add a half-strength factor at most, to avoid full bright effects when it's pitch black. 
        var dayLightFactor = 0.5f * (1 - dayLight);

        return Mathf.Min(cloudFactor + dayLightFactor, 1f);
    }

    private void UpdateMaterials()
    {
        var lightingBoost = MaxLightingBoost * _lightingFactor;

        for (var i = 0; i < _materials.Count; i++)
        {
            var material = _materials[i];
            var ambientLightColor = _ambientLightColors[i] + new Vector4(lightingBoost, lightingBoost, lightingBoost, 0f);
            material.SetVector(_ambientLightColorId, ambientLightColor);
            Plugin.Log.LogInfo(
                $"Adjusting material: {material.name} Ambient: {ambientLightColor}");
        }
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
                tintColorFactor = new Vector4(0.6f, 0.6f, 0.6f, 1f);
                ambientLightFactor = new Vector4(0f, 0f, 0f, 1f);
                break;
            case "factory4_night":
                tintColorFactor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
                ambientLightFactor = new Vector4(1.5f, 1.5f, 1.5f, 1f);
                break;
            default:
                Plugin.Log.LogError($"Unknown factory location: {location}");
                break;
        }

        foreach (var material in Singleton<LitMaterialRegistry>.Instance.DynamicAlpha)
        {
            // Don't molest blood on factory
            if (material.name.ToLower().Contains("blood")) continue;
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
        Plugin.Log.LogInfo(
            $"Adjusting material: {material.name} Tint: {tintColor} -> {newTintColor} Ambient: {ambientLightColor} -> {newAmbientLightColor}");
    }
}