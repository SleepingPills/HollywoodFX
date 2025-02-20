using System.Collections;
using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodFX.Patches;
using UnityEngine;

namespace HollywoodFX;

[BepInPlugin("com.janky.hollywoodfx", "Janky's HollywoodFX", HollywoodFXVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodFXVersion = "1.3.0";

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
    public static ConfigEntry<bool> WoundDecalsEnabled;
    public static ConfigEntry<float> WoundDecalsSize;

    public static ConfigEntry<bool> RagdollEnabled;
    public static ConfigEntry<bool> RagdollCinematicEnabled;
    public static ConfigEntry<float> RagdollLifetime;
    public static ConfigEntry<float> RagdollForceMultiplier;

    public static ConfigEntry<bool> MiscDecalsEnabled;
    public static ConfigEntry<int> MiscMaxDecalCount;
    public static ConfigEntry<int> MiscMaxConcurrentParticleSys;
    public static ConfigEntry<float> MiscShellLifetime;
    public static ConfigEntry<float> MiscShellSize;

    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;

        AssetRegistry.LoadBundles();

        StartCoroutine(DelayedLoad());
    }

    private IEnumerator DelayedLoad()
    {
        // We wait for 5 seconds to allow all the 500 shonky mods (incl. this one) an average user installs to load 
        yield return new WaitForSeconds(5);

        var visceralCombatDetected = false;

        if (Chainloader.PluginInfos.ContainsKey("com.servph.VisceralCombat"))
        {
            Log.LogInfo("Visceral Combat detected, disabling ragdolls");
            visceralCombatDetected = true;
        }

        SetupConfig(visceralCombatDetected);

        new GameWorldAwakePrefixPatch().Enable();
        new GameWorldStartedPostfixPatch().Enable();
        
        new EffectsAwakePrefixPatch().Enable();
        new EffectsAwakePostfixPatch().Enable();
        new BulletImpactPatch().Enable();
        new EffectsEmitPatch().Enable();
        new AmmoPoolObjectAutoDestroyPostfixPatch().Enable();

        if (RagdollEnabled.Value && !visceralCombatDetected)
        {
            if (RagdollCinematicEnabled.Value)
                new PlayerPoolObjectRoleModelPostfixPatch().Enable();
            
            new RagdollStartPrefixPatch().Enable();
            new RagdollApplyImpulsePrefixPatch().Enable();
            new AttachWeaponPostfixPatch().Enable();
            new LootItemIsRigidBodyDonePrefixPatch().Enable();
            
            EFTHardSettings.Instance.CorpseEnergyToSleep = -1;
        }
        
        Log.LogInfo("Initialization finished");
        
        if (_loggingEnabled.Value)
        {
            Log.LogInfo("Logging enabled");
        }
        else
        {
            Log.LogInfo("Logging disabled");
            BepInEx.Logging.Logger.Sources.Remove(Log);
        }
    }

    private void SetupConfig(bool visceralCombatDetected)
    {
        const string effectSize = "1. Effect Size (changes have no effect in-raid)";
        const string battleAmbience = "2. Ambient Battle Effects (changes have no effect in-raid)";
        const string bloodGore = "3. Blood/Gore Settings (changes have no effect in-raid)";
        const string ragdoll = "4. Ragdoll Effects (DISABLED WHEN VISCERAL COMBAT IS LOADED)";
        const string misc = "5. Miscellaneous Flotsam (changes have no effect in-raid)";
        const string debug = "6. Debug";

        /*
         * Effect sizing
         */
        SmallEffectEnergy = Config.Bind(effectSize, "Small impact energy upper bound (Joules)", 750f, new ConfigDescription(
            "Impacts with less or equal energy to this value trigger a small effect. A 4g bullet travelling at 550m/s has roughly 750J energy.",
            new AcceptableValueRange<float>(100f, 5000f),
            new ConfigurationManagerAttributes { Order = 94 }
        ));

        ChonkEffectEnergy = Config.Bind(effectSize, "Chonky impact energy lower bound (Joules)", 2500f, new ConfigDescription(
            "Impacts with more or equal energy to this trigger a large effect. A 5g bullet travelling at 1000m/s has 2500J energy.",
            new AcceptableValueRange<float>(100f, 5000f),
            new ConfigurationManagerAttributes { Order = 93 }
        ));

        SmallEffectSize = Config.Bind(effectSize, "Small Effect Scale", 0.5f, new ConfigDescription(
            "Scales the size of effects triggered by light ammo.",
            new AcceptableValueRange<float>(0.1f, 2f),
            new ConfigurationManagerAttributes { Order = 92 }
        ));

        MediumEffectSize = Config.Bind(effectSize, "Medium Effect Scale", 1.0f, new ConfigDescription(
            "Scales the size of effects triggered by mid-weight ammo.",
            new AcceptableValueRange<float>(0.1f, 2f),
            new ConfigurationManagerAttributes { Order = 91 }
        ));

        ChonkEffectSize = Config.Bind(effectSize, "Chonky Effect Scale", 1.25f, new ConfigDescription(
            "Scales the size of effects triggered by chonky ammo.",
            new AcceptableValueRange<float>(0.1f, 2f),
            new ConfigurationManagerAttributes { Order = 90 }
        ));

        /*
         * Battle Ambience
         */
        BattleAmbienceEnabled = Config.Bind(battleAmbience, "Enable Battle Ambience Effects", true, new ConfigDescription(
            "Toggles battle ambience effects like lingering smoke, dust and debris.",
            null,
            new ConfigurationManagerAttributes { Order = 64 }
        ));

        AmbientSimulationRange = Config.Bind(battleAmbience, "Forced Simulation Range", 25f, new ConfigDescription(
            "Ambient battle effects are simulated in this range around the player, even if not immediately visible. Helps create ambience from bot fights.",
            new AcceptableValueRange<float>(0, 250f),
            new ConfigurationManagerAttributes { Order = 63 }
        ));

        AmbientEffectDensity = Config.Bind(battleAmbience, "Ambient Effect Density", 1f, new ConfigDescription(
            "Adjusts the density of ambient effects. The bigger this number, the denser the smoke, debris, glitter etc...",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 62 }
        ));

        AmbientParticleLimit = Config.Bind(battleAmbience, "Ambient Effect Particle Limit", 1f, new ConfigDescription(
            "Scales the internal limits on the number of active particles. Since there are different limits for different components, this scales everything proportionally.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 61 }
        ));

        AmbientParticleLifetime = Config.Bind(battleAmbience, "Ambient Effect Particle Lifetime", 1f, new ConfigDescription(
            "Scales the internal lifetime of particles. Since there are different limits for different components, this scales everything proportionally.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 60 }
        ));

        /*
         * Blood Effects
         */
        BloodEnabled = Config.Bind(bloodGore, "Enable Blood Effects", true, new ConfigDescription(
            "Toggles whether blood effects are rendered at all. When toggled off, only the default BSG blood effects will show.",
            null,
            new ConfigurationManagerAttributes { Order = 37 }
        ));

        BloodSplatterEnabled = Config.Bind(bloodGore, "Enable Blood Splatters", true, new ConfigDescription(
            "Toggles the major blood splatters. Some people have Views. This toggle is for them.",
            null,
            new ConfigurationManagerAttributes { Order = 36 }
        ));

        BloodSplatterFineEnabled = Config.Bind(bloodGore, "Enable Blood Fine Splatters", true, new ConfigDescription(
            "Toggles the fine blood splatter that artisanally flies around following a 3d perlin noise.",
            null,
            new ConfigurationManagerAttributes { Order = 35 }
        ));

        BloodPuffsEnabled = Config.Bind(bloodGore, "Enable Blood Puffs/Clouds", true, new ConfigDescription(
            "Toggles the fine mist/cloudy effect.",
            null,
            new ConfigurationManagerAttributes { Order = 34 }
        ));

        BloodEffectSize = Config.Bind(bloodGore, "Blood Effect Size", 1f, new ConfigDescription(
            "Adjusts the size (not the quantity or quality) of blood effects. Multiplicative with the general effect scaling!",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 33 }
        ));

        WoundDecalsEnabled = Config.Bind(battleAmbience, "Enable New Wound Decals on Bodies", true, new ConfigDescription(
            "Toggles the new blood splashes appearing on bodies. If toggled off, you'll get the barely visible EFT default wound effects.",
            null,
            new ConfigurationManagerAttributes { Order = 32 }
        ));

        WoundDecalsSize = Config.Bind(battleAmbience, "Wound Decal Size", 1f, new ConfigDescription(
            "Adjusts the size of the wound decals that appear on bodies.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 31 }
        ));

        /*
         * Ragdolls
         */
        bool[] ragdollAcceptableValues = visceralCombatDetected ? [false] : [false, true];
        RagdollEnabled = Config.Bind(ragdoll, "Enable Ragdoll Effects (requires game restart)", !visceralCombatDetected, new ConfigDescription(
            "Toggles whether ragdoll effects will be enabled.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 13, ReadOnly = visceralCombatDetected }
        ));
        
        RagdollCinematicEnabled = Config.Bind(ragdoll, "Enable Cinematic Ragdolls", true, new ConfigDescription(
            "Adjusts the skeletal and joint characteristics of ragdolls for a more Cinematic (TM) experience.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 12, ReadOnly = visceralCombatDetected }
        ));

        RagdollLifetime = Config.Bind(ragdoll, "Ragdoll Lifetime (seconds)", 15f, new ConfigDescription(
            "Determines how long will ragdolls stay active for. Setting this to a very large number will likely cause increasing CPU load as more corpses pile up.",
            new AcceptableValueRange<float>(0f, 500f),
            new ConfigurationManagerAttributes { Order = 11, ReadOnly = visceralCombatDetected}
        ));

        RagdollForceMultiplier = Config.Bind(ragdoll, "Ragdoll Force Multiplier", 1f, new ConfigDescription(
            "Multiplies the force that is applied to ragdolls when enemies die.",
            new AcceptableValueRange<float>(0f, 100f),
            new ConfigurationManagerAttributes { Order = 10, ReadOnly = visceralCombatDetected }
        ));

        /*
         * Misc
         */
        MiscDecalsEnabled = Config.Bind(misc, "Enable Decal Limit Adjustment", true, new ConfigDescription(
            "Toggles whether to override the built-in decal limits. If you have this enabled in Visceral Combat, you can disable it here.",
            null,
            new ConfigurationManagerAttributes { Order = 9 }
        ));

        MiscMaxDecalCount = Config.Bind(misc, "Decal Limits", 2048, new ConfigDescription(
            "Adjusts the maximum number of decals that the game will render. The vanilla number is a puny 200.",
            new AcceptableValueRange<int>(1, 2048),
            new ConfigurationManagerAttributes { Order = 8 }
        ));

        MiscMaxConcurrentParticleSys = Config.Bind(misc, "Max New Particle Systems Per Frame", 100, new ConfigDescription(
            "Adjusts how many new particle systems can be created per frame. The vanilla game sets it to 10. The performance impact is quite low, it's best to keep this number above 30 to allow HFX to work properly.",
            new AcceptableValueRange<int>(10, 1000),
            new ConfigurationManagerAttributes { Order = 7 }
        ));

        MiscShellLifetime = Config.Bind(misc, "Spent Shells Lifetime (seconds)", 60f, new ConfigDescription(
            "How long do spent shells stay on the ground before despawning (game default is 1 second).",
            new AcceptableValueRange<float>(0f, 3600f),
            new ConfigurationManagerAttributes { Order = 6 }
        ));
        
        MiscShellSize = Config.Bind(misc, "Spent Shells Size", 1.5f, new ConfigDescription(
            "Adjusts the size of spent shells multiplicatively (2 means 2x the size).",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 5 }
        ));
        MiscShellSize.SettingChanged += (s, e) => EFTHardSettings.Instance.Shells.radius = MiscShellSize.Value / 1000f;

        /*
         * Deboog
         */
        _loggingEnabled = Config.Bind(debug, "Enable Debug Logging", false, new ConfigDescription(
            "Duh. Requires restarting the game to take effect.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));
    }
}