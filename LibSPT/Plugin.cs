using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodFX.Patches;

namespace HollywoodFX
{
    [BepInPlugin("com.janky.hollywoodfx", "Janky's HollywoodFX", "1.0.0")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        
        public static ConfigEntry<float> SmallEffectEnergy;
        
        public static ConfigEntry<float> ChonkEffectEnergy;
        
        public static ConfigEntry<float> SmallEffectSize;
        public static ConfigEntry<float> MediumEffectSize;
        public static ConfigEntry<float> ChonkEffectSize;
        
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
            const string effectSize = "1. Effect Size (Changes have no effect in-Raid)";
            const string bloodGore = "2. Blood/Gore Settings (Changes have no effect in-Raid)";
            
            SmallEffectEnergy = Config.Bind(effectSize, "Small impact energy upper bound (Joules)", 750f, new ConfigDescription(
                "Impacts with less or equal energy to this value trigger a small effect. A 4g bullet travelling at 550m/s has roughly 750J energy.",
                new AcceptableValueRange<float>(100f, 5000f),
                new ConfigurationManagerAttributes { Order = 20 }
            ));
            
            ChonkEffectEnergy = Config.Bind(effectSize, "Chonky impact energy lower bound (Joules)", 2500f, new ConfigDescription(
                "Impacts with more or equal energy to this trigger a large effect. A 5g bullet travelling at 1000m/s has 2500J energy.",
                new AcceptableValueRange<float>(100f, 5000f),
                new ConfigurationManagerAttributes { Order = 19 }
            ));
            
            SmallEffectSize = Config.Bind(effectSize, "Small Effect Scale", 0.5f, new ConfigDescription(
                "Scales the size of effects triggered by light ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 18 }
            ));
            
            MediumEffectSize = Config.Bind(effectSize, "Medium Effect Scale", 1.0f, new ConfigDescription(
                "Scales the size of effects triggered by mid-weight ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 17 }
            ));
            
            ChonkEffectSize = Config.Bind(effectSize, "Chonky Effect Scale", 1.25f, new ConfigDescription(
                "Scales the size of effects triggered by chonky ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 16 }
            ));
            
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
                "Adjusts the size (not the quantity or quality) of blood effects. Multiplicative with the general effect scaling!",
                new AcceptableValueRange<float>(0f, 5f),
                new ConfigurationManagerAttributes { Order = 7 }
            ));
        }
    }
}
