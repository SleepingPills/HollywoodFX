using System;
using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Lighting.Patches;

public class LampControllerAwakePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LampController).GetMethod(nameof(LampController.Awake));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static void Prefix(LampController __instance, MultiFlareLight[] ___MultiFlareLights, MaterialEmission[] ____materialsWithEmission)
    {
        // Plugin.Log.LogInfo($"Found light: {__instance.name} lights: {___MultiFlareLights} alights: {__instance.CustomLights.Length}");
        
        foreach (var flareLight in ___MultiFlareLights)
        {
            // Plugin.Log.LogInfo($"Flare light: {__instance.name} alpha {flareLight.Alpha} scale {flareLight.Scale} flares {flareLight.Flares.Count}");

            // Apply a floor on the alpha and compress the range with an sqrt
            flareLight.Alpha = Mathf.Sqrt(Math.Max(flareLight.Alpha, 0.2f));
            flareLight.Alpha *= Plugin.LightFlareIntensity.Value;

            var scaleField = Traverse.Create(flareLight).Field("_scale");

            // ReSharper disable once HeapView.BoxingAllocation
            scaleField.SetValue(scaleField.GetValue<float>() * Plugin.LightFlareSize.Value);

            foreach (var flare in flareLight.Flares)
            {
                // Plugin.Log.LogInfo($"Flare: {flare.TexId} alpha {flare._alpha} @ {flare._minAlpha} -> {flare._maxAlpha} scale {flare._minScale} -> {flare._maxScale} dist {flare._minDist} -> {flare._maxDist}");

                flare._alpha = Mathf.Sqrt(flare._alpha);

                // Randomly replace some of the beamy flares with another one
                if (flare._texId == 1 && Random.Range(0f, 1f) < 0.75f)
                {
                    flare._texId = 2;
                }

                if (flare._minAlpha >= flare._maxAlpha)
                {
                    flare._minAlpha *= 2f;
                }
                else
                {
                    var lowVal = flare._minAlpha;
                    flare._minAlpha = 2f * flare._maxAlpha;
                    flare._maxAlpha = lowVal;
                }
            }
        }
    }
}
