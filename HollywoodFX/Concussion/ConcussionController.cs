using System.Runtime.CompilerServices;
using EFT.UI;
using UnityEngine;

namespace HollywoodFX.Concussion;

public class ConcussionController : MonoBehaviour
{
    private float _time;
    
    private PrismEffects _prism;
    private UltimateBloom _bloom;
    private const float Eps = 1e-2f;

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
        
        ConsoleScreen.Log("Test");
        
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
        
        _bloom.m_DustIntensity = Mathf.Lerp(0.3f, 4f, dofScale);

        _time -= Time.deltaTime;
    }
}