using System.Reflection;
using Comfort.Common;
using EFT;
using GPUInstancer;
using HollywoodFX.Lighting;
using HollywoodFX.Patches;
using HollywoodFX.Postprocessing;
using SPT.Reflection.Patching;

namespace HollywoodFX.Graphics;

public class GraphicsRaidInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_37));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(RaidSettings ____raidSettings)
    {
        var mapName = ____raidSettings.LocationId.ToLower();
        Plugin.GraphicsConfig.SetCurrentMap(mapName);
        
        var overrides = Plugin.GraphicsConfig.Current;
        Plugin.Log.LogInfo($"Graphics overrides map: {mapName} - {overrides.Name} enabled: {overrides.Enabled.Value}");
    }
}

public class GraphicsLodOverridePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        Singleton<MaterialRegistry>.Instance?.SetMipBias(Plugin.MipBias.Value);
        Plugin.Log.LogInfo($"Updated mipmap bias to {Plugin.MipBias.Value}");

        Plugin.GraphicsConfig.UpdateLodBias();
        Plugin.Log.LogInfo($"Updated lod bias to {Plugin.GraphicsConfig.Current.LodBias.Value}");
        
        // Initialize Post-Processing Stack bloom effect
        __instance.gameObject.AddComponent<BloomController>();
        Plugin.Log.LogInfo("Bloom effect initialized");
    }
}

public class GraphicsTerrainDetailOverridePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GPUInstancerDetailManager).GetMethod(nameof(GPUInstancerDetailManager.Awake));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GPUInstancerDetailManager __instance)
    {
        var overrides = Plugin.GraphicsConfig.Current;
        
        Plugin.Log.LogInfo(
            $"Terrain detail overrides: {overrides.Name} enabled: {overrides.Enabled.Value} terrain: {__instance.terrain.name} DistT: {__instance.terrain.detailObjectDistance} Dist: {__instance.terrainSettings.maxDetailDistance} DistL: {__instance.terrainSettings.maxDetailDistanceLegacy} Dens: {__instance.terrainSettings.detailDensity}"
        );
        
        if (!overrides.Enabled.Value)
            return;

        var terrainDetailDistance = overrides.DetailDistance.Value;
        
        // __instance.terrain.detailObjectDistance *= Plugin.TerrainDetailDistance.Value;

        __instance.terrainSettings.maxDetailDistance *= terrainDetailDistance;
        __instance.terrainSettings.maxDetailDistanceLegacy *= terrainDetailDistance;

        foreach (var prototype in __instance.prototypeList)
        {
            var detailPrototype = prototype as GPUInstancerDetailPrototype;

            if (detailPrototype != null)
            {
                Plugin.Log.LogInfo($"DetailProto: {prototype.name} {prototype.maxDistance} {detailPrototype.densityFadeFactor}");
                detailPrototype.maxDistance *= terrainDetailDistance;
                detailPrototype.lodBiasAdjustment = terrainDetailDistance;
                detailPrototype.densityFadeFactor /= overrides.DetailDensity.Value;
            }
            else
            {
                Plugin.Log.LogInfo($"Proto: {prototype.name} {prototype.maxDistance}");
                prototype.maxDistance *= terrainDetailDistance;
                prototype.lodBiasAdjustment *= terrainDetailDistance;
            }
        }
    }
}