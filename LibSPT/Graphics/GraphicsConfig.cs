using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using HollywoodFX.Lighting;
using UnityEngine;

namespace HollywoodFX.Graphics;

public class LodOverrides(
    string name,
    ConfigEntry<bool> enabled,
    ConfigEntry<float> lodBias,
    ConfigEntry<float> detailDistance,
    ConfigEntry<float> detailDensity)
{
    public readonly string Name = name;
    public readonly ConfigEntry<bool> Enabled = enabled;
    public readonly ConfigEntry<float> LodBias = lodBias;
    public readonly ConfigEntry<float> DetailDistance = detailDistance;
    public readonly ConfigEntry<float> DetailDensity = detailDensity;
}

public class GraphicsConfig
{
    public LodOverrides Current;

    public readonly ConfigEntry<float> MipBias;

    private readonly Dictionary<string, LodOverrides> _overrides = new();
    private readonly Dictionary<string, string[]> _mapNames = new()
    {
        { "Customs", ["bigmap"] },
        { "Factory", ["factory4_day", "factory4_night"] },
        { "Interchange", ["interchange"] },
        { "Laboratory", ["laboratory"] },
        { "Lighthouse", ["lighthouse"] },
        { "Reserve", ["rezervbase"] },
        { "GroundZero", ["sandbox", "sandbox_high"] },
        { "Shoreline", ["shoreline"] },
        { "Streets", ["tarkovstreets"] },
        { "Woods", ["woods"] },
        { "Default", ["default"] }
    };

    public GraphicsConfig(ConfigFile config, string section)
    {
        MipBias = config.Bind(section, "Effect Quality Bias", 0f, new ConfigDescription(
            "Positive values force higher quality effect textures at a distance, lower values force lower quality. Numbers above 4 can have *heavy*" +
            "VRAM impact and cause stuttering.",
            new AcceptableValueRange<float>(-5f, 10f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));
        MipBias.SettingChanged += (_, _) => { UpdateMipBias(); };

        AddDetailOverrides(config, section, "Default", browsable: false);
        AddDetailOverrides(config, section, "Customs", false, 6f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Factory");
        AddDetailOverrides(config, section, "Interchange", false, 6f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Laboratory");
        AddDetailOverrides(config, section, "Lighthouse", false, 10f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Reserve", false, 4f, 2.5f, 2f);
        AddDetailOverrides(config, section, "GroundZero", false, 6f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Shoreline", false, 10f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Streets");
        AddDetailOverrides(config, section, "Woods", false, 10f, 2.5f, 2f);

        Current = _overrides["default"];
    }

    public void SetCurrentMap(string map)
    {
        Current.LodBias.SettingChanged -= OnLodBiasChanged;

        if (!_overrides.TryGetValue(map, out Current))
        {
            Plugin.Log.LogInfo($"Map {map} not found in GraphicsConfig using default settings");
            Current = _overrides["default"];
        }

        Current.LodBias.SettingChanged += OnLodBiasChanged;
    }

    public void UpdateMipBias()
    {
        var materialRegistry = Singleton<MaterialRegistry>.Instance;

        if (materialRegistry == null)
            return;

        materialRegistry.SetMipBias(MipBias.Value);
    }

    public void UpdateLodBias()
    {
        if (!Current.Enabled.Value)
        {
            if (!Singleton<SharedGameSettingsClass>.Instantiated)
                return;

            var defaultLodBias = Singleton<SharedGameSettingsClass>.Instance.Graphics.Settings.LodBias;
            Plugin.Log.LogInfo($"LoD overrides disabled, resetting to the default value of {defaultLodBias}.");
            QualitySettings.lodBias = defaultLodBias;
            return;
        }

        QualitySettings.lodBias = Current.LodBias.Value;
    }

    public void SetDetailOverrides(string map, bool enabled = false, float lodBias = 4, float detailDistance = 1f, float detailDensityScaling = 1f)
    {
        foreach (var name in _mapNames[map])
        {
            var overrides = _overrides[name];

            overrides.Enabled.Value = enabled;
            overrides.LodBias.Value = lodBias;
            overrides.DetailDistance.Value = detailDistance;
            overrides.DetailDensity.Value = detailDensityScaling;
        }
    }

    private void AddDetailOverrides(
        ConfigFile config, string section, string map,
        bool enabled = false, float lodBias = 4, float detailDistance = 1f, float detailDensityScaling = 1f, bool browsable = true
    )
    {
        var mapSection = $"{section} - {map}";

        var overrides = new LodOverrides(
            map,
            config.Bind(mapSection, $"{map} Enable (RESTART)", enabled, new ConfigDescription(
                "Toggles whether the LOD settings should be overridden at all.",
                null,
                new ConfigurationManagerAttributes { Order = 4, Browsable = browsable }
            )),
            config.Bind(mapSection, $"{map} LOD Bias", lodBias, new ConfigDescription(
                "Adjust the LOD bias in a wider range than what the game allows.",
                new AcceptableValueRange<float>(1f, 20f),
                new ConfigurationManagerAttributes { Order = 3, Browsable = browsable }
            )),
            config.Bind(mapSection, $"{map} Detail Cull Range Scaling", detailDistance, new ConfigDescription(
                "Scales the maximum visible distance for detail like rocks, debris and foliage.",
                new AcceptableValueRange<float>(0.5f, 10f),
                new ConfigurationManagerAttributes { Order = 2, Browsable = browsable }
            )),
            config.Bind(mapSection, $"{map} Detail Density Scaling", detailDensityScaling, new ConfigDescription(
                "Scales the density of detail like rocks, debris and foliage.",
                new AcceptableValueRange<float>(0.5f, 5f),
                new ConfigurationManagerAttributes { Order = 1, Browsable = browsable }
            ))
        );

        foreach (var name in _mapNames[map])
        {
            _overrides[name] = overrides;
        }
    }

    private void OnLodBiasChanged(object o, EventArgs e)
    {
        UpdateLodBias();
    }
}