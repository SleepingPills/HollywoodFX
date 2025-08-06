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

public sealed class BloomConfig
{
    public event EventHandler ConfigChanged;

    private readonly ConfigEntry<float> _bloomIntensity;

    public readonly ConfigEntry<float> BloomDark;
    public readonly ConfigEntry<float> BloomMid;
    public readonly ConfigEntry<float> BloomBright;
    public readonly ConfigEntry<float> BloomHighlight;
    
    private readonly ConfigEntry<float> _samplingMinHeight;
    private readonly ConfigEntry<bool> _useLensDust;
    private readonly ConfigEntry<float> _dustIntensity;
    private readonly ConfigEntry<float> _dirtLightIntensity;
    
    private readonly ConfigEntry<bool> _useAnamorphicFlare;
    private readonly ConfigEntry<float> _anamorphicFlareIntensity;
    private readonly ConfigEntry<int> _anamorphicScale;
    private readonly ConfigEntry<bool> _anamorphicSmallVerticalBlur;
    private readonly ConfigEntry<int> _anamorphicBlurPass;
    
    private readonly ConfigEntry<bool> _useStarFlare;
    private readonly ConfigEntry<float> _starFlareIntensity;
    private readonly ConfigEntry<float> _starScale;
    private readonly ConfigEntry<int> _starBlurPass;
    
    public BloomConfig(ConfigFile config, string section)
    {
        var bloomSection = $"{section} - Bloom";

        _bloomIntensity = config.Bind(bloomSection, "Master Bloom Intensity", 0.2f, new ConfigDescription(
            "Controls the overall intensity of the bloom effect.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 104 }
        ));
        _bloomIntensity.SettingChanged += OnConfigChanged;

        BloomDark = config.Bind(bloomSection, "Bloom Curve Dark", -0.98f, new ConfigDescription(
            "Bloom intensity of the dark colors range.",
            new AcceptableValueRange<float>(-3f, 3f),
            new ConfigurationManagerAttributes { Order = 103 }
        ));
        BloomDark.SettingChanged += OnConfigChanged;
        
        BloomMid = config.Bind(bloomSection, "Bloom Curve Mid", 0.4f, new ConfigDescription(
            "Bloom intensity of the mid colors range.",
            new AcceptableValueRange<float>(-3f, 3f),
            new ConfigurationManagerAttributes { Order = 102 }
        ));
        BloomMid.SettingChanged += OnConfigChanged;

        BloomBright = config.Bind(bloomSection, "Bloom Curve Bright", 0.75f, new ConfigDescription(
            "Bloom intensity of the bright colors range.",
            new AcceptableValueRange<float>(-3f, 3f),
            new ConfigurationManagerAttributes { Order = 101 }
        ));
        BloomBright.SettingChanged += OnConfigChanged;
        
        BloomHighlight = config.Bind(bloomSection, "Bloom Curve Highlight", 0.5f, new ConfigDescription(
            "Bloom intensity of the bright colors range.",
            new AcceptableValueRange<float>(-3f, 3f),
            new ConfigurationManagerAttributes { Order = 100 }
        ));
        BloomHighlight.SettingChanged += OnConfigChanged;
        
        _samplingMinHeight = config.Bind(bloomSection, "Sampling Min Height", 768f, new ConfigDescription(
            "Minimum height for height-relative sampling mode.",
            new AcceptableValueRange<float>(0f, 2048f),
            new ConfigurationManagerAttributes { Order = 97 }
        ));
        _samplingMinHeight.SettingChanged += OnConfigChanged;

        _useLensDust = config.Bind(bloomSection, "Use Lens Dust", true, new ConfigDescription(
            "Enables lens dust effect.",
            null,
            new ConfigurationManagerAttributes { Order = 96 }
        ));
        _useLensDust.SettingChanged += OnConfigChanged;

        _dustIntensity = config.Bind(bloomSection, "Dust Intensity", 0.075f, new ConfigDescription(
            "Controls the intensity of the lens dust effect.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 95 }
        ));
        _dustIntensity.SettingChanged += OnConfigChanged;

        _dirtLightIntensity = config.Bind(bloomSection, "Dirt Light Intensity", 0.5f, new ConfigDescription(
            "Controls the intensity of dirt light effects.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 94 }
        ));
        _dirtLightIntensity.SettingChanged += OnConfigChanged;

        _useAnamorphicFlare = config.Bind(bloomSection, "Use Anamorphic Flare", true, new ConfigDescription(
            "Enables anamorphic lens flare effects.",
            null,
            new ConfigurationManagerAttributes { Order = 84 }
        ));
        _useAnamorphicFlare.SettingChanged += OnConfigChanged;

        _anamorphicFlareIntensity = config.Bind(bloomSection, "Anamorphic Flare Intensity", 2.5f, new ConfigDescription(
            "Controls the intensity of anamorphic flares.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 83 }
        ));
        _anamorphicFlareIntensity.SettingChanged += OnConfigChanged;

        _anamorphicScale = config.Bind(bloomSection, "Anamorphic Scale", 10, new ConfigDescription(
            "Scaling factor for anamorphic flares.",
            new AcceptableValueRange<int>(0, 50),
            new ConfigurationManagerAttributes { Order = 82 }
        ));
        _anamorphicScale.SettingChanged += OnConfigChanged;

        _anamorphicSmallVerticalBlur = config.Bind(bloomSection, "Anamorphic Small Vertical Blur", true, new ConfigDescription(
            "Enables small vertical blur for anamorphic flares.",
            null,
            new ConfigurationManagerAttributes { Order = 81 }
        ));
        _anamorphicSmallVerticalBlur.SettingChanged += OnConfigChanged;

        _anamorphicBlurPass = config.Bind(bloomSection, "Anamorphic Blur Pass", 3, new ConfigDescription(
            "Number of blur passes for anamorphic flares.",
            new AcceptableValueRange<int>(1, 5),
            new ConfigurationManagerAttributes { Order = 80 }
        ));
        _anamorphicBlurPass.SettingChanged += OnConfigChanged;

        _useStarFlare = config.Bind(bloomSection, "Use Star Flare", true, new ConfigDescription(
            "Enables star-shaped lens flare effects.",
            null,
            new ConfigurationManagerAttributes { Order = 79 }
        ));
        _useStarFlare.SettingChanged += OnConfigChanged;

        _starFlareIntensity = config.Bind(bloomSection, "Star Flare Intensity", 1.5f, new ConfigDescription(
            "Controls the intensity of star flares.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 78 }
        ));
        _starFlareIntensity.SettingChanged += OnConfigChanged;

        _starScale = config.Bind(bloomSection, "Star Scale", 5f, new ConfigDescription(
            "Scaling factor for star flares.",
            new AcceptableValueRange<float>(0f, 50f),
            new ConfigurationManagerAttributes { Order = 77 }
        ));
        _starScale.SettingChanged += OnConfigChanged;

        _starBlurPass = config.Bind(bloomSection, "Star Blur Pass", 2, new ConfigDescription(
            "Number of blur passes for star flares.",
            new AcceptableValueRange<int>(1, 5),
            new ConfigurationManagerAttributes { Order = 76 }
        ));
        _starBlurPass.SettingChanged += OnConfigChanged;
    }

    public void ApplyConfig(UltimateBloom ultimateBloom)
    {
        ultimateBloom.m_BloomIntensity = _bloomIntensity.Value;
        ultimateBloom.SetFilmicCurveParameters(BloomMid.Value, BloomDark.Value, BloomBright.Value, BloomHighlight.Value);
        ultimateBloom.m_SamplingMinHeight = _samplingMinHeight.Value;

        ultimateBloom.m_UseLensDust = _useLensDust.Value;
        ultimateBloom.m_DustIntensity = _dustIntensity.Value;
        ultimateBloom.m_DirtLightIntensity = _dirtLightIntensity.Value;

        ultimateBloom.m_UseAnamorphicFlare = _useAnamorphicFlare.Value;
        ultimateBloom.m_AnamorphicFlareIntensity = _anamorphicFlareIntensity.Value;
        ultimateBloom.m_AnamorphicScale = _anamorphicScale.Value;
        ultimateBloom.m_AnamorphicSmallVerticalBlur = _anamorphicSmallVerticalBlur.Value;
        ultimateBloom.m_AnamorphicBlurPass = _anamorphicBlurPass.Value;

        ultimateBloom.m_UseStarFlare = _useStarFlare.Value;
        ultimateBloom.m_StarFlareIntensity = _starFlareIntensity.Value;
        ultimateBloom.m_StarScale = _starScale.Value;
        ultimateBloom.m_StarBlurPass = _starBlurPass.Value;
    }

    private void OnConfigChanged(object o, EventArgs e)
    {
        ConfigChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class GraphicsConfig
{
    public LodOverrides Current;

    public readonly ConfigEntry<float> MipBias;
    public readonly ConfigEntry<bool> RealLightTempEnabled;
    
    public readonly BloomConfig Bloom;
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
            new ConfigurationManagerAttributes { Order = 2 }
        ));
        MipBias.SettingChanged += (_, _) => { UpdateMipBias(); };
        RealLightTempEnabled = config.Bind(section, "Realistic Light Color Temp", true, new ConfigDescription(
            "Toggles the realistic light color temperature in outdoor areas. The default color temperature has way too much red compared to real sunlight.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        Bloom = new BloomConfig(config, section);

        AddDetailOverrides(config, section, "Default", browsable: false);
        AddDetailOverrides(config, section, "Customs", false, 4f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Factory");
        AddDetailOverrides(config, section, "Interchange", false, 4f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Laboratory");
        AddDetailOverrides(config, section, "Lighthouse", false, 10f, 2.5f, 2f);
        AddDetailOverrides(config, section, "Reserve", false, 4f, 2.5f, 2f);
        AddDetailOverrides(config, section, "GroundZero", false, 4f, 2.5f, 2f);
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
        Singleton<MaterialRegistry>.Instance?.SetMipBias(MipBias.Value);
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