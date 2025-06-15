using System.Reflection;
using GPUInstancer;
using SPT.Reflection.Patching;

namespace HollywoodFX.Patches;

public class GPUInstancerDetailManagerAwakePostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GPUInstancerDetailManager).GetMethod(nameof(GPUInstancerDetailManager.Awake));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GPUInstancerDetailManager __instance)
    {
        if (!Plugin.TerrainDetailOverrideEnabled.Value)
            return;
        
        Plugin.Log.LogInfo($"S0 Terrain: {__instance.terrain.name} DistT: {__instance.terrain.detailObjectDistance} Dist: {__instance.terrainSettings.maxDetailDistance} DistL: {__instance.terrainSettings.maxDetailDistanceLegacy} Dens: {__instance.terrainSettings.detailDensity}");
        // __instance.terrain.detailObjectDistance *= Plugin.TerrainDetailDistance.Value;
        __instance.terrainSettings.maxDetailDistance *= Plugin.TerrainDetailDistance.Value;
        __instance.terrainSettings.maxDetailDistanceLegacy *= Plugin.TerrainDetailDistance.Value;
        
        foreach (var prototype in __instance.prototypeList)
        {
            var detailPrototype = prototype as GPUInstancerDetailPrototype;

            if (detailPrototype != null)
            {
                Plugin.Log.LogInfo($"S0 DetailProto: {prototype.name} {prototype.maxDistance} {detailPrototype.densityFadeFactor}");
                detailPrototype.maxDistance *= Plugin.TerrainDetailDistance.Value;
                detailPrototype.lodBiasAdjustment = Plugin.TerrainDetailDistance.Value;
                detailPrototype.densityFadeFactor /= Plugin.TerrainDetailDensityScaling.Value;
            }
            else
            {
                Plugin.Log.LogInfo($"S0 Proto: {prototype.name} {prototype.maxDistance}");
                prototype.maxDistance *= Plugin.TerrainDetailDistance.Value;
                prototype.lodBiasAdjustment *= Plugin.TerrainDetailDistance.Value;
            }
        }
    }
}
