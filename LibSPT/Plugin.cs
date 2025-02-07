using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodFX.Patches;

namespace HollywoodFX
{
    [BepInPlugin("com.janky.HollywoodFX", "Janky's HollywoodFX", "1.0.0")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        
        public static ConfigEntry<bool> BloodEnabled;
        public static ConfigEntry<bool> BloodSplatterEnabled;
        public static ConfigEntry<bool> BloodSplatterFineEnabled;
        public static ConfigEntry<bool> BloodPuffsEnabled;
        public static ConfigEntry<float> BloodEffectSize;
        
        private void Awake()
        {
            Log = Logger;

            SetupConfig();
            
            AssetRegistry.LoadBundles();

            new EffectsAwakePatch().Enable();
            new OnGameStartedPatch().Enable();
            new BulletImpactPatch().Enable();
            new EffectsEmitPatch().Enable();

            Logger.LogInfo("HollywoodFX Loaded, praise the destruction!");
        }

        private void SetupConfig()
        {
            const string bloodGore = "1. Blood/Gore Settings (Changes have no effect in-Raid)";
            
            BloodEnabled = Config.Bind(bloodGore, "Enable Blood Effects", true, new ConfigDescription(
                "Toggles whether blood effects are rendered at all. When toggled off, only the default BSG blood effects will show.",
                null,
                new ConfigurationManagerAttributes { Order = 10 }
            ));

            BloodSplatterEnabled = Config.Bind(bloodGore, "Enable Blood Splatters", true, new ConfigDescription(
                "Toggles the major blood splatters. Some people have Views. This toggle is for them.",
                null,
                new ConfigurationManagerAttributes { Order = 9 }
            ));

            BloodSplatterFineEnabled = Config.Bind(bloodGore, "Enable Blood Fine Splatters", true, new ConfigDescription(
                "Toggles the fine blood splatter that artisanally flies around following a 3d perlin noise.",
                null,
                new ConfigurationManagerAttributes { Order = 8 }
            ));
            
            BloodPuffsEnabled = Config.Bind(bloodGore, "Enable Blood Puffs/Clouds", true, new ConfigDescription(
                "Toggles the fine mist/cloudy effect.",
                null,
                new ConfigurationManagerAttributes { Order = 8 }
            ));
            
            BloodEffectSize = Config.Bind(bloodGore, "Blood Effect Size", 1f, new ConfigDescription(
                "Adjusts the size (not the quantity or quality) of blood effects.",
                new AcceptableValueRange<float>(0f, 5f),
                new ConfigurationManagerAttributes { Order = 7 }
            ));
        }
    }
}
