using EFT.Animations;
using UnityEngine;

namespace HollywoodFX.Render;

public class ScopeDofController(DepthOfField depthOfField)
{
    private float _current;
    private float _target;
    private float _velocity;

    private const float Eps = 1e-2f;

    public void Update()
    {
        if (!Plugin.ScopeDofEnabled.Value)
            return;
        
        var weaponAnim = ImpactStatic.LocalPlayer.ProceduralWeaponAnimation;

        var isZoomedOpticAiming = false;

        if (weaponAnim != null)
        {
            isZoomedOpticAiming = weaponAnim.IsAiming && weaponAnim.CurrentScope is { IsOptic: true } && GetScopeZoomLevel(weaponAnim) > 1f;
        }

        _target = isZoomedOpticAiming ? 1f : 0f;

        // Don't do the heavy math if the target and current are nearly equal 
        if (Mathf.Abs(_target - _current) <= Eps)
        {
            _current = _target;
        }
        else
        {
            _current = Mathf.SmoothDamp(_current, _target, ref _velocity, 0.15f);            
        }
        
        depthOfField.ApplyScope(Plugin.ScopeDofIntensity.Value * _current);
    }
    
    private static float GetScopeZoomLevel(ProceduralWeaponAnimation weaponAnim)
    {
        var sight = weaponAnim.CurrentAimingMod;
        return sight == null ? 1f : sight.GetCurrentOpticZoom();
    }
}