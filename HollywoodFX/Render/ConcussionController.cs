using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace HollywoodFX.Render;

public class ConcussionController
{
    private float _time;
    
    private float _minLensDust = 0.3f;
    private float _maxLensDust = 2.3f;
    
    private const float Eps = 1e-2f;

    private readonly DepthOfField _depthOfField;
    private readonly UltimateBloom _bloom;
    private readonly ConfigEntry<float> _lensDustIntensity;

    public ConcussionController(DepthOfField depthOfField)
    {
        var camera = CameraClass.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("Concussion: No camera found!");
            return;
        }
        
        _bloom = camera.GetComponent<UltimateBloom>();
        _depthOfField = depthOfField;

        if (!Chainloader.PluginInfos.ContainsKey("com.janky.hollywoodgraphics")) return;
        
        Plugin.Log.LogInfo("HollywoodGraphics detected, hooking Bloom config entries");
            
        var assembly = Assembly.Load("HollywoodGraphics");
        var type = assembly.GetType("HollywoodGraphics.Plugin");
        var getter = type.GetProperty("lensDustIntensity")?.GetGetMethod();
        _lensDustIntensity = (ConfigEntry<float>)getter?.Invoke(type, null);

        if (_lensDustIntensity == null) return;
        
        _lensDustIntensity.SettingChanged += UpdateLensDustSettings;
        UpdateLensDustSettings(null, null);
        Plugin.Log.LogInfo($"HollywoodGraphics lens dust config hooked with current value of {_lensDustIntensity.Value}");
    }
    
    public void Apply(Vector3 position, float t, float distanceNorm, float maxTime)
    {
        var camera = CameraClass.Instance.Camera;
        
        if (camera == null)
            return;
        
        Apply(Vector3.Distance(position, camera.transform.position), t, distanceNorm, maxTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Apply(float cameraDistance, float t, float distanceNorm, float maxTime)
    {
        if (cameraDistance >= distanceNorm)
            return;
        
        var normalizedDistance = cameraDistance / distanceNorm;
        
        // Reach maximum effect at half the normalized distance
        var distanceScale = 1f - Mathf.InverseLerp(0.5f, 1f, normalizedDistance);
        
        _time += t * distanceScale;
        _time = Mathf.Clamp(_time, 0f, maxTime);
    }

    public void Update()
    {
        if (_depthOfField == null)
            return;
        
        if (_time <= Eps)
        {
            _time = 0f;
            _depthOfField.ApplyConcussion(0f);            
            return;
        }

        var dofScale = Mathf.Clamp01(_time / 2f);
        
        _depthOfField.ApplyConcussion(dofScale);
        _bloom.m_DustIntensity = Mathf.Lerp(_minLensDust, _maxLensDust, dofScale);
        _time -= Time.deltaTime;
    }
    
    public void OnDestroy()
    {
        if (_lensDustIntensity == null)
            return;
        
        _lensDustIntensity.SettingChanged -= UpdateLensDustSettings;       
    }
    
    private void UpdateLensDustSettings(object o, EventArgs e)
    {
        if (_lensDustIntensity == null)
            return;
        
        _minLensDust = _lensDustIntensity.Value;
        _maxLensDust = _lensDustIntensity.Value + 4f * Plugin.BattleBlurIntensity.Value;
    }
}