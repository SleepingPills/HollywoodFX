using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodFX.Patches;

namespace HollywoodFX
{
    [BepInPlugin("com.janky.hollywoodfx", "Janky's HollywoodFX", "1.0.2")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public static ConfigEntry<float> SmallEffectEnergy;
        public static ConfigEntry<float> ChonkEffectEnergy;
        public static ConfigEntry<float> SmallEffectSize;
        public static ConfigEntry<float> MediumEffectSize;
        public static ConfigEntry<float> ChonkEffectSize;

        public static ConfigEntry<bool> BattleAmbienceEnabled;
        public static ConfigEntry<float> AmbientSimulationRange;
        public static ConfigEntry<float> AmbientEffectDensity;
        public static ConfigEntry<float> AmbientParticleLimit;
        public static ConfigEntry<float> AmbientParticleLifetime;

        public static ConfigEntry<bool> BloodEnabled;
        public static ConfigEntry<bool> BloodSplatterEnabled;
        public static ConfigEntry<bool> BloodSplatterFineEnabled;
        public static ConfigEntry<bool> BloodPuffsEnabled;
        public static ConfigEntry<float> BloodEffectSize;
        
        public static ConfigEntry<bool> MiscDecalsEnabled;
        public static ConfigEntry<int> MiscMaxDecalCount;
        public static ConfigEntry<int> MiscMaxConcurrentParticleSys;

        private void Awake()
        {
            Log = Logger;

            SetupConfig();

            AssetRegistry.LoadBundles();

            new GameWorldAwakePrefixPatch().Enable();
            new EffectsAwakePrefixPatch().Enable();
            new EffectsAwakePostfixPatch().Enable();
            new BulletImpactPatch().Enable();
            new EffectsEmitPatch().Enable();

            Logger.LogInfo("HollywoodFX Loaded, praise the destruction!");
        }

        private void SetupConfig()
        {
            const string effectSize = "1. Effect Size (Changes have no effect in-Raid)";
            const string battleAmbience = "2. Ambient Battle Effects (Changes have no effect in-Raid)";
            const string bloodGore = "3. Blood/Gore Settings (Changes have no effect in-Raid)";
            const string misc = "4. Miscellaneous Flotsam (Changes have no effect in-Raid)";

            /*
             * Effect sizing
             */
            SmallEffectEnergy = Config.Bind(effectSize, "Small impact energy upper bound (Joules)", 750f, new ConfigDescription(
                "Impacts with less or equal energy to this value trigger a small effect. A 4g bullet travelling at 550m/s has roughly 750J energy.",
                new AcceptableValueRange<float>(100f, 5000f),
                new ConfigurationManagerAttributes { Order = 30 }
            ));

            ChonkEffectEnergy = Config.Bind(effectSize, "Chonky impact energy lower bound (Joules)", 2500f, new ConfigDescription(
                "Impacts with more or equal energy to this trigger a large effect. A 5g bullet travelling at 1000m/s has 2500J energy.",
                new AcceptableValueRange<float>(100f, 5000f),
                new ConfigurationManagerAttributes { Order = 29 }
            ));

            SmallEffectSize = Config.Bind(effectSize, "Small Effect Scale", 0.5f, new ConfigDescription(
                "Scales the size of effects triggered by light ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 28 }
            ));

            MediumEffectSize = Config.Bind(effectSize, "Medium Effect Scale", 1.0f, new ConfigDescription(
                "Scales the size of effects triggered by mid-weight ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 27 }
            ));

            ChonkEffectSize = Config.Bind(effectSize, "Chonky Effect Scale", 1.25f, new ConfigDescription(
                "Scales the size of effects triggered by chonky ammo.",
                new AcceptableValueRange<float>(0.1f, 2f),
                new ConfigurationManagerAttributes { Order = 26 }
            ));

            /*
             * Battle Ambience
             */
            BattleAmbienceEnabled = Config.Bind(battleAmbience, "Enable Battle Ambience Effects", true, new ConfigDescription(
                "Toggles battle ambience effects like lingering smoke, dust and debris.",
                null,
                new ConfigurationManagerAttributes { Order = 20 }
            ));

            AmbientSimulationRange = Config.Bind(battleAmbience, "Forced Simulation Range", 25f, new ConfigDescription(
                "Ambient battle effects are simulated in this range around the player, even if not immediately visible. Helps create ambience from bot fights.",
                new AcceptableValueRange<float>(0, 250f),
                new ConfigurationManagerAttributes { Order = 19 }
            ));

            AmbientEffectDensity = Config.Bind(battleAmbience, "Ambient Effect Density", 1f, new ConfigDescription(
                "Adjusts the density of ambient effects. The bigger this number, the denser the smoke, debris, glitter etc...",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 18 }
            ));

            AmbientParticleLimit = Config.Bind(battleAmbience, "Ambient Effect Particle Limit", 1f, new ConfigDescription(
                "Scales the internal limits on the number of active particles. Since there are different limits for different components, this scales everything proportionally.",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 17 }
            ));

            AmbientParticleLifetime = Config.Bind(battleAmbience, "Ambient Effect Particle Lifetime", 1f, new ConfigDescription(
                "Scales the internal lifetime of particles. Since there are different limits for different components, this scales everything proportionally.",
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = 17 }
            ));
            
            /*
             * Blood Effects
             */
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
            
            /*
             * Misc
             */
            MiscDecalsEnabled = Config.Bind(misc, "Enable Decal Limit Adjustment", true, new ConfigDescription(
                "Toggles whether to override the built-in decal limits. If you have this enabled in Visceral Combat, you can disable it here.",
                null,
                new ConfigurationManagerAttributes { Order = 5 }
            ));
            
            MiscMaxDecalCount = Config.Bind(misc, "Decal Limits", 2048, new ConfigDescription(
                "Adjusts the maximum number of decals that the game will render. The vanilla number is a puny 200.",
                new AcceptableValueRange<int>(1, 2048),
                new ConfigurationManagerAttributes { Order = 4 }
            ));
            
            MiscMaxConcurrentParticleSys = Config.Bind(misc, "Max New Particle Systems Per Frame", 100, new ConfigDescription(
                "Adjusts how many new particle systems can be created per frame. The vanilla game sets it to 10. The performance impact is quite low, it's best to keep this number above 30 to allow HFX to work properly.",
                new AcceptableValueRange<int>(10, 1000),
                new ConfigurationManagerAttributes { Order = 3 }
            ));
        }
    }
}