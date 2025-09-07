using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace HollywoodFX.Concussion;

public class ConcussionController : MonoBehaviour
{
    private float _time;
    
    private PrismEffects _prism;
    private UltimateBloom _bloom;

    private float _minLensDust = 0.3f;
    private float _maxLensDust = 4.3f;
    
    private const float Eps = 1e-2f;

    private ConfigEntry<float> _lensDustIntensity;

    public void Init()
    {
        var camera = CameraClass.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("Concussion: No camera found!");
            return;
        }

        _prism = camera.GetComponent<PrismEffects>();
        _prism.debugDofPass = false;
        _prism.useNearDofBlur = false;
        
        _bloom = camera.GetComponent<UltimateBloom>();

        HollywoodGraphicsIntegration();
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
        if (_prism == null || cameraDistance >= distanceNorm)
            return;
        
        var normalizedDistance = cameraDistance / distanceNorm;
        
        // Reach maximum effect at half the normalized distance
        var distanceScale = 1f - Mathf.InverseLerp(0.5f, 1f, normalizedDistance);
        
        _time += t * distanceScale;
        _time = Mathf.Clamp(_time, 0f, maxTime);
        
        if (_time > Eps)
            _prism.useDof = true;
    }

    public void Update()
    {
        if (_prism == null)
            return;
        
        if (_time <= Eps)
        {
            _prism.useDof = false;
            _time = 0f;
            return;
        }

        var dofScale = Mathf.Clamp01(_time / 2f);
        
        // Focus point is behind the camera slightly to avoid immediately de-blurring the gun and hands
        _prism.dofFocusPoint = -2f;
        _prism.dofFocusDistance = Mathf.Lerp(0f, 5f, dofScale);
        _prism.dofRadius = dofScale * Plugin.BattleBlurIntensity.Value;
        
        _bloom.m_DustIntensity = Mathf.Lerp(_minLensDust, _maxLensDust, dofScale);

        _time -= Time.deltaTime;
    }

    private void HollywoodGraphicsIntegration()
    {
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
    
    private void UpdateLensDustSettings(object o, EventArgs e)
    {
        if (_lensDustIntensity == null)
            return;
        
        _minLensDust = _lensDustIntensity.Value;
        _maxLensDust = _lensDustIntensity.Value + 4f * Plugin.BattleBlurIntensity.Value;
    }

    private void OnDestroy()
    {
        if (_lensDustIntensity == null)
            return;
        
        _lensDustIntensity.SettingChanged -= UpdateLensDustSettings;       
    }
}