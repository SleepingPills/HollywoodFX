using System.Collections.Generic;
using Comfort.Common;
using EFT.Weather;
using UnityEngine;

namespace HollywoodFX.Lighting;

internal static class AmbientLightingPropertyIds
{
    public static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    public static readonly int LocalMinimalAmbientLightId = Shader.PropertyToID("_LocalMinimalAmbientLight");
}

public class AmbientLightingController : MonoBehaviour
{
    private List<Material> _materials;
    
    private List<Vector4> _ambientLightColors;
    private List<Vector4> _tintColors;
    private List<float> _tintAlphaFactors;

    private WeatherController _weatherController;

    private const float MaxLightingBoost = 2f;

    private const float MinTintColorFactor = 0.9f;
    private const float MaxTintColorFactor = 1f;

    private const float MinTintAlphaFactor = 0f;
    private const float MaxTintAlphaFactor = 0.15f;

    private const float FactorChangeThreshold = 0.1f;
    private float _lightingFactor;


    private const float RepeatRate = 5f;
    private float _timer;

    public void Awake()
    {
        _materials = [];
        _tintAlphaFactors = [];

        foreach (var material in Singleton<MaterialRegistry>.Instance.DynamicAlpha)
        {
            _materials.Add(material);
            _tintAlphaFactors.Add(1f);
        }

        foreach (var material in Singleton<MaterialRegistry>.Instance.StaticAlpha)
        {
            _materials.Add(material);
            _tintAlphaFactors.Add(0f);
        }

        Plugin.Log.LogInfo($"Found {_materials.Count} materials with ambient lighting parameters");

        _ambientLightColors = new List<Vector4>(_materials.Count);
        _tintColors = new List<Vector4>(_materials.Count);

        foreach (var material in _materials)
        {
            _ambientLightColors.Add(material.GetVector(AmbientLightingPropertyIds.LocalMinimalAmbientLightId));
            _tintColors.Add(material.GetVector(AmbientLightingPropertyIds.TintColorId));
        }

        _weatherController = GameObject.Find("Weather").GetComponent<WeatherController>();

        /* Original color curve:
         * [Info   :Janky's HollywoodFX] Light color time: 0 key: RGBA(0.809, 0.881, 1.000, 1.000)
           [Info   :Janky's HollywoodFX] Light color time: 0.5115129 key: RGBA(0.000, 0.000, 0.000, 1.000)
           [Info   :Janky's HollywoodFX] Light color time: 0.5266652 key: RGBA(1.000, 0.457, 0.322, 1.000)
           [Info   :Janky's HollywoodFX] Light color time: 0.5535668 key: RGBA(0.859, 0.631, 0.392, 1.000)
           [Info   :Janky's HollywoodFX] Light color time: 0.6971694 key: RGBA(1.000, 0.867, 0.537, 1.000)
           [Info   :Janky's HollywoodFX] Light color time: 0.9992523 key: RGBA(0.585, 0.530, 0.361, 1.000)
         */
        // 0 -> 0.31, 6 -> 0.46, 12 -> 0.9, 18 - 0.76
        if (Plugin.GraphicsConfig.RealLightTempEnabled.Value)
        {
            // Add back the missing blue component to natural daylight
            _weatherController.TimeOfDayController.LightColor = new Gradient()
            {
                alphaKeys =
                [
                    new GradientAlphaKey(1f, 0.0f),
                    new GradientAlphaKey(1f, 1f)
                ],
                colorKeys =
                [
                    new GradientColorKey(new Color(0f, 0f, 0f), 0.32f),
                    new GradientColorKey(new Color(0.65f, 0.45f, 0.35f), 0.45f),
                    new GradientColorKey(new Color(0.9f, 0.8f, 0.5f), 0.7f),
                    new GradientColorKey(new Color(0.9f, 0.85f, 0.9f), 1f)
                ]
            };
        }

        _lightingFactor = CalculateLightingFactor();
        UpdateMaterials(_lightingFactor);
        Plugin.Log.LogInfo($"Initialized lighting factor to {_lightingFactor}");
    }

    public void Update()
    {
        if (_timer <= 0)
        {
            var currentLightingFactor = CalculateLightingFactor();

            if (Mathf.Abs(currentLightingFactor - _lightingFactor) > FactorChangeThreshold)
            {
                _lightingFactor = currentLightingFactor;
                UpdateMaterials(_lightingFactor);
            }
            _timer = RepeatRate;
        }

        _timer -= Time.deltaTime;
    }

    private float CalculateLightingFactor()
    {
        var dayLight = Mathf.Min(Mathf.Sqrt(Mathf.Max(_weatherController.SunHeight, 0f)) / 0.75f, 1f);
        // Clouds will really only factor into the equation during daytime. At night, the cloud factor is 0.
        var cloudFactor = dayLight * Mathf.Max(_weatherController.WeatherCurve.Cloudiness, 0f);
        // Full night will add a half-strength factor at most, to avoid full bright effects when it's pitch black. 
        var dayLightFactor = 0.5f * (1 - dayLight);

        return Mathf.Min(cloudFactor + dayLightFactor, 1f);
    }

    private void UpdateMaterials(float lightingFactor)
    {
        var tintColorFactor = Mathf.Lerp(MinTintColorFactor, MaxTintColorFactor, lightingFactor);
        var tintAlphaFactorBase = Mathf.Lerp(MinTintAlphaFactor, MaxTintAlphaFactor, lightingFactor);

        var lightingBoost = MaxLightingBoost * lightingFactor;
        var lightingBoostVec = new Vector4(lightingBoost, lightingBoost, lightingBoost, 1f);

        for (var i = 0; i < _materials.Count; i++)
        {
            var ambientLightColor = _ambientLightColors[i];
            ambientLightColor += Vector4.Scale(ambientLightColor, lightingBoostVec);

            var tintColor = _tintColors[i];
            var tintAlphaFactor = 1 + _tintAlphaFactors[i] * tintAlphaFactorBase;
            tintColor.Scale(new Vector4(tintColorFactor, tintColorFactor, tintColorFactor, tintAlphaFactor));

            _materials[i].SetVector(AmbientLightingPropertyIds.TintColorId, tintColor);
            _materials[i].SetVector(AmbientLightingPropertyIds.LocalMinimalAmbientLightId, ambientLightColor);
        }
    }
}

public static class StaticMaterialAmbientLighting
{
    public static void AdjustLighting(string location)
    {
        var tintColorFactor = new Vector4(1f, 1f, 1f, 1f);
        var ambientLightFactor = new Vector4(1f, 1f, 1f, 1f);

        switch (location)
        {
            case "factory4_day":
                tintColorFactor = new Vector4(0.7f, 0.65f, 0.6f, 1f);
                ambientLightFactor = new Vector4(0f, 0f, 0f, 1f);
                break;
            case "factory4_night":
                tintColorFactor = new Vector4(0.7f, 0.65f, 0.6f, 1f);
                ambientLightFactor = new Vector4(1.5f, 1.5f, 1.5f, 1f);
                break;
            default:
                Plugin.Log.LogError($"Location using default lighting: {location}");
                break;
        }

        foreach (var material in Singleton<MaterialRegistry>.Instance.DynamicAlpha)
        {
            if (material.name.ToLower().Contains("blood")) continue;
            ApplyScaling(material, tintColorFactor, ambientLightFactor);
        }

        // Don't scale the alpha
        tintColorFactor.w = 1f;
        foreach (var material in Singleton<MaterialRegistry>.Instance.StaticAlpha)
        {
            if (material.name.ToLower().Contains("blood")) continue;
            ApplyScaling(material, tintColorFactor, ambientLightFactor);
        }
    }

    private static void ApplyScaling(Material material, Vector4 tintColorFactor, Vector4 ambientLightFactor)
    {
        var tintColor = material.GetVector(AmbientLightingPropertyIds.TintColorId);
        var ambientLightColor = material.GetVector(AmbientLightingPropertyIds.LocalMinimalAmbientLightId);

        var newTintColor = Vector4.Scale(tintColor, tintColorFactor);
        var newAmbientLightColor = Vector4.Scale(ambientLightColor, ambientLightFactor);

        material.SetVector(AmbientLightingPropertyIds.TintColorId, newTintColor);
        material.SetVector(AmbientLightingPropertyIds.LocalMinimalAmbientLightId, newAmbientLightColor);
        Plugin.Log.LogInfo(
            $"Adjusting material: {material.name} Tint: {tintColor} -> {newTintColor} Ambient: {ambientLightColor} -> {newAmbientLightColor}"
        );
    }
}