using System.Reflection;
using EFT.Interactive;
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
    public static void Prefix(LampController __instance, MultiFlareLight[] ___MultiFlareLights, MaterialEmission[] ____materialsWithEmission, ref float ____scale)
    {
        // Plugin.Log.LogInfo($"Found light: {__instance.name} lights: {__instance.Lights.Length} alights: {__instance.CustomLights.Length}");
        foreach (var flareLight in ___MultiFlareLights)
        {
            // Plugin.Log.LogInfo($"Flare light: {flareLight.name} alpha {flareLight.Alpha} scale {flareLight.Scale} flares {flareLight.Flares.Count}");

            flareLight.Alpha *= Plugin.LightFlareIntensity.Value;
            ____scale *= Plugin.LightFlareSize.Value;
            
            // foreach (var flare in flareLight.Flares)
            // {
            //     Plugin.Log.LogInfo($"Flare: {flare._alpha} {flare._color} {flare._scale}");
            // }
        }
    }
}