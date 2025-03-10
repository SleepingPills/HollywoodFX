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
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodFXVersion = "1.4.3";

    public static ManualLogSource Log;

    public static ConfigEntry<float> EffectSize;

    public static ConfigEntry<bool> BattleAmbienceEnabled;
    public static ConfigEntry<float> AmbientSimulationRange;
    public static ConfigEntry<float> AmbientEffectDensity;
    public static ConfigEntry<float> AmbientParticleLimit;
    public static ConfigEntry<float> AmbientParticleLifetime;

    public static ConfigEntry<bool> BloodEnabled;
    public static ConfigEntry<float> BloodMistSize;
    public static ConfigEntry<float> BloodSpraySize;
    public static ConfigEntry<float> BloodSquibSize;
    public static ConfigEntry<float> BloodSquirtSize;
    public static ConfigEntry<float> BloodFinisherSize;
    public static ConfigEntry<bool> WoundDecalsEnabled;
    public static ConfigEntry<float> WoundDecalsSize;
    public static ConfigEntry<float> BloodSplatterDecalsSize;

    public static ConfigEntry<bool> RagdollEnabled;
    public static ConfigEntry<bool> RagdollCinematicEnabled;
    public static ConfigEntry<bool> RagdollDropWeaponEnabled;
    public static ConfigEntry<float> RagdollForceMultiplier;

    public static ConfigEntry<bool> MiscDecalsEnabled;
    public static ConfigEntry<int> MiscMaxDecalCount;
    public static ConfigEntry<int> MiscMaxConcurrentParticleSys;
    public static ConfigEntry<float> MiscShellLifetime;
    public static ConfigEntry<float> MiscShellSize;
    public static ConfigEntry<float> KineticsScaling;

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
        new GameWorldShotDelegatePrefixPatch().Enable();
        
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
            new RagdollStartPostfixPatch().Enable();

            if (RagdollDropWeaponEnabled.Value)
            {
                new AttachWeaponPostfixPatch().Enable();
                new LootItemIsRigidBodyDonePrefixPatch().Enable();
            }
            
            new PlayerOnDeadPostfixPatch().Enable();
            
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
        const string ragdoll = "4. Ragdoll Effects (disabled by Visceral Combat)";
        const string misc = "5. Miscellaneous Flotsam (changes have no effect in-raid)";
        const string debug = "6. Debug";

        /*
         * Effect sizing
         */
        EffectSize = Config.Bind(effectSize, "Dakka Scale (larger is more dakka)", 1.0f, new ConfigDescription(
            "Scales the size of effects.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 91 }
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
            new ConfigurationManagerAttributes { Order = 39 }
        ));

        BloodMistSize = Config.Bind(bloodGore, "Mist Size", 1f, new ConfigDescription(
            "Adjusts the size of blood mists/puffs.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 38 }
        ));

        BloodSpraySize = Config.Bind(bloodGore, "Spray Size", 1f, new ConfigDescription(
            "Adjusts the size of fine blood sprays.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 37 }
        ));

        BloodSquibSize = Config.Bind(bloodGore, "Squib Size", 1f, new ConfigDescription(
            "Adjusts the size of blood sqquibs.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 36 }
        ));
        
        BloodSquirtSize = Config.Bind(bloodGore, "Squirt Size", 1f, new ConfigDescription(
            "Adjusts the size of the blood squirts.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 35 }
        ));

        BloodFinisherSize = Config.Bind(bloodGore, "Finisher Gore Size", 1f, new ConfigDescription(
            "Adjusts the size of the gore generated by finisher shots.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 34 }
        ));

        WoundDecalsEnabled = Config.Bind(battleAmbience, "Enable New Wound Decals on Bodies", true, new ConfigDescription(
            "Toggles the new blood splashes appearing on bodies. If toggled off, you'll get the barely visible EFT default wound effects. Philistine.",
            null,
            new ConfigurationManagerAttributes { Order = 32 }
        ));

        WoundDecalsSize = Config.Bind(battleAmbience, "Wound Decal Size", 1f, new ConfigDescription(
            "Adjusts the size of the wound decals that appear on bodies.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 31 }
        ));
        
        BloodSplatterDecalsSize = Config.Bind(battleAmbience, "Splatter Decal Size", 1f, new ConfigDescription(
            "Adjusts the size of the blood splatters on the environment.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 30 }
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
        
        RagdollDropWeaponEnabled = Config.Bind(ragdoll, "Drop Weapon on Death", true, new ConfigDescription(
            "Toggles the enemies dropping their weapon on death.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 11, ReadOnly = visceralCombatDetected }
        ));

        RagdollForceMultiplier = Config.Bind(ragdoll, "Ragdoll Force Multiplier", 1f, new ConfigDescription(
            "Multiplies the force that is applied to ragdolls when enemies die.",
            new AcceptableValueRange<float>(0f, 100f),
            new ConfigurationManagerAttributes { Order = 10, ReadOnly = visceralCombatDetected }
        ));
        RagdollForceMultiplier.SettingChanged += (_, _) => EFTHardSettings.Instance.HIT_FORCE = 2.5f * 150f * RagdollForceMultiplier.Value;

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
        
        KineticsScaling = Config.Bind(misc, "Bullet Kinetics Scaling", 1f, new ConfigDescription(
            "Scales the overall kinetic energy, impulse, etc.",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 4 }
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