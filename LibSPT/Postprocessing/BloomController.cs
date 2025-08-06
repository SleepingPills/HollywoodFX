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
            // Re-increase intensity a touch during peak daylight
            sunLightFactorCur = 0.35f * Mathf.InverseLerp(0.6f, 0.7f, _weatherController.SunHeight);
        }
        
        if (Mathf.Abs(sunLightFactorCur - _sunLightFactor) < 0.03f)
            return;

        var highlightScaling = 1f + 0.3f * sunLightFactorCur;
        
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