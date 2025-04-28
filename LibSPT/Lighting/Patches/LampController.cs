using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;

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
            flareLight.Alpha *= Plugin.LightFlareIntensity.Value;

            var scaleField = Traverse.Create(flareLight).Field("_scale");

            // ReSharper disable once HeapView.BoxingAllocation
            scaleField.SetValue(scaleField.GetValue<float>() * Plugin.LightFlareSize.Value);
        }
    }
}
