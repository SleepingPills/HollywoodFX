using UnityEngine;

namespace HollywoodFX.Render;

// DoF params:
// dofFocusDistance: 0f
// dofFocusPoint: 0.15f (move this to -2f when suppressed)
// dofBokehFactor: 0f

// For concussion we increase the bokeh factor to the default

// Default DoF bokeh factor is 157
public class DepthOfField
{
    private readonly PrismEffects _prism;

    private float _concussionAmount;
    private float _scopeAmount;
    
    private const float Eps = 1e-2f;

    public DepthOfField()
    {
        var camera = CameraClass.Instance?.Camera;

        if (camera == null)
        {
            Plugin.Log.LogError("DepthOfField: No camera found!");
            return;
        }

        _prism = camera.GetComponent<PrismEffects>();
        _prism.debugDofPass = false;
        _prism.useNearDofBlur = false;

        _prism.dofFocusDistance = 0f;
        _prism.dofFocusPoint = 0.15f;
        _prism.dofBokehFactor = 0f;
    }
    
    public void ApplyConcussion(float amount)
    {
        if (_prism == null)
            return;
        
        _concussionAmount = amount;
        
        Update();
    }

    public void ApplyScope(float amount)
    {
        if (_prism == null)
            return;
        
        _scopeAmount = amount;
        
        Update();
    }

    private void Update()
    {
        _prism.dofRadius = _scopeAmount + _concussionAmount * Plugin.BattleBlurIntensity.Value;
        
        if (_prism.dofRadius <= Eps && _prism.useDof)
        {
            _prism.dofRadius = 0f;
            _prism.dofBokehFactor = 0f;
            _prism.dofFocusPoint = 0.15f;
            _prism.useDof = false;
            return;
        }
        
        _prism.useDof = true;
        
        _prism.dofFocusPoint = Mathf.Lerp(0.15f, -2f, _concussionAmount);
        _prism.dofBokehFactor = Mathf.Lerp(0f, 157f, _concussionAmount);
    }
}