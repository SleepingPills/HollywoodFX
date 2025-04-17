using System;
using System.Collections.Generic;
using Comfort.Common;
using HollywoodFX.Lighting;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Particles;

public static class ParticleHelpers
{
    public static IEnumerable<(string, ParticleSystem[])> LoadParticleSystemBundles(Effects eftEffects, GameObject prefab, bool dynamicAlpha)
    {
        Plugin.Log.LogInfo($"Instantiating Effects Prefab {prefab.name}");
        var rootInstance = Object.Instantiate(prefab);

        foreach (var group in rootInstance.transform.GetChildren())
        {
            var groupName = group.name;
            var effects = new List<ParticleSystem>();

            foreach (var child in group.GetChildren())
            {
                if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;
                
                child.parent = eftEffects.transform;
                effects.Add(particleSystem);
                
                Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, dynamicAlpha);
            }

            yield return new(groupName, effects.ToArray());
        }
    }
    
    public static void ScaleEmissionRate(ParticleSystem system, float scale)
    {
        var emission = system.emission;
        
        if (!emission.enabled)
            return;

        emission.rateOverDistance = ScaleMinMaxCurve(emission.rateOverDistance, scale);
        emission.rateOverTime = ScaleMinMaxCurve(emission.rateOverTime, scale);
        
        for (var i = 0; i < emission.burstCount; i++)
        {
            var burst = emission.GetBurst(i);
            burst.minCount = (short)Mathf.Clamp(burst.minCount * scale, 0, short.MaxValue);
            burst.maxCount = (short)Mathf.Clamp(burst.maxCount * scale, 0, short.MaxValue);
            emission.SetBurst(i, burst);
        }
    }

    private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve curve, float scale)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                var orig = curve.constant;
                curve.constant *= scale; 
                break;
            case ParticleSystemCurveMode.TwoConstants:
                curve.constantMin *= scale;
                curve.constantMax *= scale;
                break;
            case ParticleSystemCurveMode.Curve:
                curve.curve = ScaleAnimationCurve(curve.curve, scale);
                break;
            case ParticleSystemCurveMode.TwoCurves:
                curve.curveMin = ScaleAnimationCurve(curve.curveMin, scale);
                curve.curveMax = ScaleAnimationCurve(curve.curveMax, scale);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return curve;
    }

    private static AnimationCurve ScaleAnimationCurve(AnimationCurve curve, float scale)
    {
        var keyFrames = curve.keys;
        
        for (var i = 0; i < keyFrames.Length; i++)
        {
            var frame = keyFrames[i];
            frame.value *= scale;
            keyFrames[i] = frame;
        }
        
        curve.keys = keyFrames;

        return curve;
    }
}