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
        else
        {
            Plugin.Log.LogInfo("UltimateBloomController: Found existing Ultimate Bloom component on camera");
        }

        Plugin.Log.LogInfo($"UltimateBloomController: HDR is {ultimateBloom.m_HDR}");

        ultimateBloom.m_IntensityManagement = UltimateBloom.BloomIntensityManagement.FilmicCurve;
        ultimateBloom.m_SamplingMode = UltimateBloom.SamplingMode.HeightRelative;

        ultimateBloom.m_FlareTint0 = new Color(137 / 255.0f, 82 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint1 = new Color(0 / 255.0f, 63 / 255.0f, 126 / 255.0f);
        ultimateBloom.m_FlareTint2 = new Color(72 / 255.0f, 151 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint3 = new Color(114 / 255.0f, 35 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint4 = new Color(122 / 255.0f, 88 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint5 = new Color(137 / 255.0f, 71 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint6 = new Color(97 / 255.0f, 139 / 255.0f, 0 / 255.0f);
        ultimateBloom.m_FlareTint7 = new Color(40 / 255.0f, 142 / 255.0f, 0 / 255.0f);

        for (var i = 0; i < 10; i++)
        {
            if (i < ultimateBloom.m_BloomUsages.Length)
            {
                Plugin.Log.LogInfo($"Bloom: {ultimateBloom.m_BloomUsages[i]} {ultimateBloom.m_BloomIntensities[i]}");
                ultimateBloom.m_BloomIntensities[i] = 1f;

            }
            if (i < ultimateBloom.m_AnamorphicBloomUsages.Length)
            {
                Plugin.Log.LogInfo($"AnamorphicBloom: {ultimateBloom.m_AnamorphicBloomUsages[i]} {ultimateBloom.m_AnamorphicBloomIntensities[i]}");
                ultimateBloom.m_AnamorphicBloomIntensities[i] = 1f;

            }
            if (i < ultimateBloom.m_StarBloomUsages.Length)
            {
                Plugin.Log.LogInfo($"StarBloom: {ultimateBloom.m_StarBloomUsages[i]} {ultimateBloom.m_StarBloomIntensities[i]}");
                ultimateBloom.m_StarBloomIntensities[i] = 1f;
            }
        }

        // Turn these off as they form the "blob" part of the bloom and can oversaturate the entire screen.
        ultimateBloom.m_BloomUsages[0] = ultimateBloom.m_BloomUsages[1] = false;
        ultimateBloom.m_AnamorphicBloomUsages[0] = false;
        ultimateBloom.m_AnamorphicBloomUsages[1] = true;
        ultimateBloom.m_StarBloomUsages[0] = false;

        Plugin.GraphicsConfig.Bloom.ApplyConfig(ultimateBloom);
        Plugin.GraphicsConfig.Bloom.ConfigChanged += UpdateSettings;
        Plugin.Log.LogInfo($"UltimateBloomController: Ultimate Bloom effect applied to camera {targetCamera.name}");

        _weatherController = GameObject.Find("Weather").GetComponent<WeatherController>();
    }

    private void Update()
    {
        if (_weatherController == null)
            return;

        float sunLightFactorCur;

        if (_weatherController.SunHeight < 0)
        {
            // Increase intensity in the dark
            sunLightFactorCur = Mathf.InverseLerp(0f, -0.1f, _weatherController.SunHeight);
        }
        else
        {
            // Decrease the intensity slightly around morning/dusk to avoid the sky being obliterated by bloom
            sunLightFactorCur = -0.35f * Mathf.InverseLerp(0.7f, 0.5f, _weatherController.SunHeight);
        }
        
        if (Mathf.Abs(sunLightFactorCur - _sunLightFactor) < 0.03f)
            return;

        var highlightScaling = 1f + 0.3f * sunLightFactorCur;
        
        // TODO: Delete this
        Plugin.Log.LogInfo($"Highlight scaling: {highlightScaling}");

        var bloomConfig = Plugin.GraphicsConfig.Bloom;
        ultimateBloom.SetFilmicCurveParameters(
            bloomConfig.BloomMid.Value,
            bloomConfig.BloomDark.Value,
            bloomConfig.BloomBright.Value,
            highlightScaling * bloomConfig.BloomHighlight.Value
        );

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