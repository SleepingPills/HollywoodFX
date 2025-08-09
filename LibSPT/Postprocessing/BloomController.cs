using System;
using EFT.Weather;
using UnityEngine;

namespace HollywoodFX.Postprocessing;

public class BloomController : MonoBehaviour
{
    public UltimateBloom ultimateBloom;

    private WeatherController _weatherController;
    private float _sunLightFactor = 10000f;

    private void Start()
    {
        // Find the main camera
        var targetCamera = CameraClass.Instance?.Camera;

        if (targetCamera == null)
        {
            Plugin.Log.LogError("UltimateBloomController: No camera found!");
            return;
        }

        // Check if Ultimate Bloom is already on the camera
        ultimateBloom = targetCamera.GetComponent<UltimateBloom>();

        if (ultimateBloom == null)
        {
            // Add Ultimate Bloom component to camera
            ultimateBloom = targetCamera.gameObject.AddComponent<UltimateBloom>();
            Plugin.Log.LogInfo("UltimateBloomController: Added Ultimate Bloom component to camera");
        }

        ultimateBloom.m_IntensityManagement = UltimateBloom.BloomIntensityManagement.FilmicCurve;
        ultimateBloom.m_SamplingMode = UltimateBloom.SamplingMode.HeightRelative;

        Plugin.Log.LogInfo("Resetting Main Bloom intensities");
        ResetIntensities(ultimateBloom.m_BloomIntensities);
        Plugin.Log.LogInfo("Resetting Anamorphic Bloom intensities");
        ResetIntensities(ultimateBloom.m_AnamorphicBloomIntensities);
        Plugin.Log.LogInfo("Resetting Star Bloom intensities");
        ResetIntensities(ultimateBloom.m_StarBloomIntensities);
        
        // Turn these off as they form the "blob" part of the bloom and can oversaturate the entire screen.
        ultimateBloom.m_BloomUsages[0] = ultimateBloom.m_BloomUsages[1] = false;
        ultimateBloom.m_AnamorphicBloomUsages[0] = false;
        ultimateBloom.m_AnamorphicBloomUsages[1] = true;
        ultimateBloom.m_StarBloomUsages[0] = false;

        Plugin.GraphicsConfig.Bloom.ApplyConfig(ultimateBloom);
        Plugin.GraphicsConfig.Bloom.ConfigChanged += UpdateSettings;
        Plugin.Log.LogInfo($"UltimateBloomController: Ultimate Bloom effect applied to camera {targetCamera.name}");

        var weather = GameObject.Find("Weather");
        
        if (weather == null)
            return;
        
        _weatherController = weather.GetComponent<WeatherController>();
    }

    private static void ResetIntensities(float[] intensities)
    {
        for (var i = 0; i < intensities.Length; i++)
        {
            Plugin.Log.LogInfo($"Intensity: {intensities[i]}");
            intensities[i] = 1f;
        }
    }

    private void Update()
    {
        if (_weatherController == null)
            return;

        var streakScale = 1f;
        float sunLightFactorCur;

        if (_weatherController.SunHeight < 0)
        {
            // Increase intensity in the dark
            sunLightFactorCur = Mathf.InverseLerp(0f, -0.1f, _weatherController.SunHeight);
            // Decrease streak size 
            streakScale = 1f - 0.75f * sunLightFactorCur;
        }
        else
        {
            // Decrease the intensity slightly around morning/dusk to avoid the sky being obliterated by bloom
            sunLightFactorCur = -0.35f * Mathf.InverseLerp(0.7f, 0.5f, _weatherController.SunHeight);
        }

        if (Mathf.Abs(sunLightFactorCur - _sunLightFactor) < 0.03f)
            return;

        var bloomConfig = Plugin.GraphicsConfig.Bloom;
        ultimateBloom.m_AnamorphicScale = streakScale * bloomConfig.AnamorphicScale.Value;
        ultimateBloom.m_StarScale = streakScale * bloomConfig.StarScale.Value;

        // var highlightScaling = 1f + 0.3f * sunLightFactorCur;
        // ultimateBloom.SetFilmicCurveParameters(
        //     bloomConfig.BloomMid.Value,
        //     bloomConfig.BloomDark.Value,
        //     bloomConfig.BloomBright.Value,
        //     highlightScaling * bloomConfig.BloomHighlight.Value
        // );

        _sunLightFactor = sunLightFactorCur;
    }

    private void OnDestroy()
    {
        Plugin.GraphicsConfig.Bloom.ConfigChanged -= UpdateSettings;
    }

    private void UpdateSettings(object sender, EventArgs e)
    {
        Plugin.GraphicsConfig.Bloom.ApplyConfig(ultimateBloom);

        // Force the recalculation of the sunlight factor
        _sunLightFactor = 10000f;
    }
}