using BepInEx.Configuration;

namespace HollywoodFX;

public static class ConfigurationTemplates
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
        Plugin.BloodBleedoutEmission.Value = 2f;

        Plugin.MipBias.Value = 10f;
    }

    public static void SetPotato(ConfigFile mainConfig)
    {
        // First reset to the defaults
        SetDefaults(mainConfig);
        
        // Override the important bits
        Plugin.ExplosionDensityDust.Value = 0.5f;
        Plugin.ExplosionDensitySmoke.Value = 0.5f;
        Plugin.ExplosionDensitySparks.Value = 0.5f;
        
        Plugin.BloodBleedEmission.Value = 0.3f;
        Plugin.BloodSquirtEmission.Value = 0.3f;
        Plugin.BloodFinisherEmission.Value = 0.3f;
        
        Plugin.WoundDecalsEnabled.Value = false;
        Plugin.BloodSplatterDecalsEnabled.Value = false;
        Plugin.MiscDecalsEnabled.Value = false;
        
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