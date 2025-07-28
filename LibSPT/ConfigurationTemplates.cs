using BepInEx.Configuration;

namespace HollywoodFX;

public class ConfigurationTemplates
{
    public static void SetJanky(ConfigFile mainConfig)
    {
        // First reset to the defaults
        SetDefaults(mainConfig);
        
        // Override the important bits
        Plugin.BloodSprayEmission.Value = 2f;
        Plugin.BloodBleedEmission.Value = 1f;
        Plugin.BloodSquirtEmission.Value = 1f;
        Plugin.BloodFinisherEmission.Value = 1f;

        Plugin.GraphicsConfig.MipBias.Value = 10f;
        
        Plugin.GraphicsConfig.SetDetailOverrides("Customs", true, 4f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("Interchange", true, 4f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("Lighthouse", true, 10f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("Reserve", true, 4f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("GroundZero", true, 4f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("Shoreline", true, 10f, 2.5f, 2f);
        Plugin.GraphicsConfig.SetDetailOverrides("Woods", true, 10f, 2.5f, 2f);
    }

    public static void SetPotato(ConfigFile mainConfig)
    {
        // First reset to the defaults
        SetDefaults(mainConfig);
        
        // Override the important bits
        Plugin.BattleAmbienceEnabled.Value = false;
        
        Plugin.BloodBleedEmission.Value = 0.3f;
        Plugin.BloodSquirtEmission.Value = 0.3f;
        Plugin.BloodFinisherEmission.Value = 0.3f;
        
        Plugin.WoundDecalsEnabled.Value = false;
        Plugin.BloodSplatterDecalsEnabled.Value = false;
        Plugin.MiscDecalsEnabled.Value = false;
        
        Plugin.MiscMaxConcurrentParticleSys.Value = 10;
        
        Plugin.MiscShellLifetime.Value = 5f;
        Plugin.MiscShellPhysicsEnabled.Value = false;
    }
    
    public static void SetDefaults(ConfigFile mainConfig)
    {
        foreach (var pair in mainConfig)
        {
            pair.Value.BoxedValue = pair.Value.DefaultValue;
        }
    }
}