using System.Collections;
using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT.Communications;
using EFT.UI;
using HollywoodFX.Lighting;
using HollywoodFX.Lighting.Patches;
using HollywoodFX.Muzzle.Patches;
using HollywoodFX.Patches;
using UnityEngine;

namespace HollywoodFX;

[BepInPlugin("com.janky.hollywoodfx", "Janky's HollywoodFX", HollywoodFXVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodFXVersion = "1.7.0";

    public static ManualLogSource Log;

    public static ConfigEntry<float> EffectSize;
    public static ConfigEntry<bool> TracerImpactsEnabled;

    public static ConfigEntry<bool> MuzzleEffectsEnabled;
    public static ConfigEntry<float> MuzzleEffectJetsSize;
    public static ConfigEntry<float> MuzzleEffectSparksSize;
    public static ConfigEntry<float> MuzzleEffectSparksEmission;
    public static ConfigEntry<float> MuzzleEffectSmokeSize;
    public static ConfigEntry<float> MuzzleEffectSmokeEmission;
    public static ConfigEntry<bool> MuzzleLightShadowEnabled;

    public static ConfigEntry<bool> BattleAmbienceEnabled;
    public static ConfigEntry<float> AmbientSimulationRange;
    public static ConfigEntry<float> AmbientEffectDensity;
    public static ConfigEntry<float> AmbientParticleLimit;
    public static ConfigEntry<float> AmbientParticleLifetime;

    public static ConfigEntry<bool> GoreEnabled;

    public static ConfigEntry<float> BloodMistSize;
    public static ConfigEntry<float> BloodMistEmission;

    public static ConfigEntry<float> BloodSquibSize;

    public static ConfigEntry<float> BloodSpraySize;
    public static ConfigEntry<float> BloodSprayEmission;

    public static ConfigEntry<float> BloodBleedSize;
    public static ConfigEntry<float> BloodBleedEmission;

    public static ConfigEntry<float> BloodSquirtSize;
    public static ConfigEntry<float> BloodSquirtEmission;

    public static ConfigEntry<float> BloodFinisherSize;
    public static ConfigEntry<float> BloodFinisherEmission;

    public static ConfigEntry<bool> WoundDecalsEnabled;
    public static ConfigEntry<float> WoundDecalsSize;
    public static ConfigEntry<bool> BloodSplatterDecalsEnabled;
    public static ConfigEntry<float> BloodSplatterDecalsSize;

    public static ConfigEntry<bool> RagdollEnabled;
    public static ConfigEntry<bool> RagdollCinematicEnabled;
    public static ConfigEntry<bool> RagdollDropWeaponEnabled;
    public static ConfigEntry<float> RagdollForceMultiplier;

    public static ConfigEntry<bool> MiscDecalsEnabled;
    public static ConfigEntry<int> MiscMaxDecalCount;
    public static ConfigEntry<int> MiscMaxConcurrentParticleSys;
    public static ConfigEntry<bool> LightFlareEnabled;
    public static ConfigEntry<float> LightFlareIntensity;
    public static ConfigEntry<float> LightFlareSize;
    public static ConfigEntry<float> MiscShellLifetime;
    public static ConfigEntry<float> MiscShellSize;
    public static ConfigEntry<float> MiscShellVelocity;
    public static ConfigEntry<bool> MiscShellPhysicsEnabled;

    public static ConfigEntry<float> KineticsScaling;

    public static ConfigEntry<float> MipBias;
    public static ConfigEntry<bool> LodOverrideEnabled;
    public static ConfigEntry<float> LodBias;
    public static ConfigEntry<bool> TerrainDetailOverrideEnabled;
    public static ConfigEntry<float> TerrainDetailDistance;
    public static ConfigEntry<float> TerrainDetailDensityScaling;

    private static ConfigEntry<bool> _michelinManEnabled;
    private static ConfigEntry<bool> _peenEnabled;

    private static ConfigEntry<bool> _loggingEnabled;

    private static MichelinManPatch _michelinManPatch;

    private void Awake()
    {
        Log = Logger;

        AssetRegistry.LoadBundles();

        _michelinManPatch = new MichelinManPatch();
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

        new GameWorldDisposePostfixPatch().Enable();

        new GameWorldAwakePrefixPatch().Enable();
        new GameWorldStartedPostfixPatch().Enable();
        new GameWorldShotDelegatePrefixPatch().Enable();

        new EffectsAwakePrefixPatch().Enable();
        new EffectsAwakePostfixPatch().Enable();
        new BulletImpactPatch().Enable();
        new EffectsEmitPatch().Enable();
        new TextureDecalsPainterVisCheckPatch().Enable();
        new AmmoPoolObjectAutoDestroyPostfixPatch().Enable();

        if (LightFlareEnabled.Value)
            new LampControllerAwakePostfixPatch().Enable();

        if (MiscShellPhysicsEnabled.Value && !visceralCombatDetected)
            new ShellOnBouncePrefixPatch().Enable();

        if (MuzzleEffectsEnabled.Value)
        {
            new FirearmControllerInitiateShotPrefixPatch().Enable();
            new MuzzleManagerShotPrefixPatch().Enable();
            new WeaponPrefabInitHotObjectsPostfixPatch().Enable();
        }

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

            EFTHardSettings.Instance.CorpseEnergyToSleep = -1;
        }

        if (GoreEnabled.Value && !visceralCombatDetected)
        {
            new PlayerOnDeadPostfixPatch().Enable();
        }

        // if (_detailOverride.Value)
        new GPUInstancerDetailManagerAwakePostfixPatch().Enable();

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
        const string general = "01. Impact Effects";
        const string muzzleEffects = "02. Muzzle Blast Effects";
        const string battleAmbience = "03. Ambient Battle Effects (RESTART)";
        const string goreEmission = "04. Gore Emission (RESTART)";
        const string goreSize = "05. Gore Size";
        const string goreDecals = "06. Gore Decals";
        const string ragdoll = "07. Ragdoll Effects (DISABLED BY VISCERAL COMBAT)";
        const string misc = "08. Miscellaneous Flotsam";
        const string gfx = "09. Graphics";
        const string whimsy = "10. Whimsy";
        const string debug = "11. Debug";

        /*
         * General
         */
        EffectSize = Config.Bind(general, "Impact Effect Size", 1.0f, new ConfigDescription(
            "Scales the size of impact effects.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        TracerImpactsEnabled = Config.Bind(general, "Enable Tracer Round Impacts", true, new ConfigDescription(
            "Toggles special impact effects for tracer rounds.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Muzzle Effects
         */
        MuzzleEffectsEnabled = Config.Bind(muzzleEffects, "Enable Muzzle Effects (RESTART)", true, new ConfigDescription(
            "Toggles new muzzle blast effects.",
            null,
            new ConfigurationManagerAttributes { Order = 7 }
        ));

        MuzzleEffectJetsSize = Config.Bind(muzzleEffects, "Muzzle Jet Size", 1f, new ConfigDescription(
            "Adjusts the size of the muzzle flame jets.",
            new AcceptableValueRange<float>(0, 10f),
            new ConfigurationManagerAttributes { Order = 6 }
        ));

        MuzzleEffectSparksSize = Config.Bind(muzzleEffects, "Muzzle Sparks Size", 1f, new ConfigDescription(
            "Adjusts the size of the muzzle sparks.",
            new AcceptableValueRange<float>(0, 10f),
            new ConfigurationManagerAttributes { Order = 5 }
        ));

        MuzzleEffectSparksEmission = Config.Bind(muzzleEffects, "Muzzle Sparks Emission Rate (RESTART)", 1f, new ConfigDescription(
            "Adjusts the amount of muzzle sparks generated.",
            new AcceptableValueRange<float>(0.1f, 10f),
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        MuzzleEffectSmokeSize = Config.Bind(muzzleEffects, "Muzzle Smoke Size", 1f, new ConfigDescription(
            "Adjusts the size of the muzzle smoke.",
            new AcceptableValueRange<float>(0, 10f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        MuzzleEffectSmokeEmission = Config.Bind(muzzleEffects, "Muzzle Smoke Emission Rate (RESTART)", 1f, new ConfigDescription(
            "Adjusts the amount of muzzle smoke generated. If you are looking to hotbox with some scavs, set it to 3 or something.",
            new AcceptableValueRange<float>(0.1f, 10f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        MuzzleLightShadowEnabled = Config.Bind(muzzleEffects, "Enable Muzzle Light Shadow (RESTART)", true, new ConfigDescription(
            "Toggles shadow casting for muzzle lights.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Battle Ambience
         */
        BattleAmbienceEnabled = Config.Bind(battleAmbience, "Enable Battle Ambience Effects", true, new ConfigDescription(
            "Toggles battle ambience effects like lingering smoke, dust and debris.",
            null,
            new ConfigurationManagerAttributes { Order = 5 }
        ));

        AmbientSimulationRange = Config.Bind(battleAmbience, "Forced Simulation Range", 25f, new ConfigDescription(
            "Ambient battle effects are simulated in this range around the player, even if not immediately visible. Helps create ambience from bot fights.",
            new AcceptableValueRange<float>(0, 250f),
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        AmbientEffectDensity = Config.Bind(battleAmbience, "Ambient Effect Emission Rate", 1f, new ConfigDescription(
            "Adjusts the density of ambient effects. The bigger this number, the denser the smoke, debris, glitter etc...",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        AmbientParticleLimit = Config.Bind(battleAmbience, "Ambient Effect Particle Limit", 1f, new ConfigDescription(
            "Scales the internal limits on the number of active particles. Since there are different limits for different components, this scales everything proportionally.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        AmbientParticleLifetime = Config.Bind(battleAmbience, "Ambient Effect Particle Lifetime", 1f, new ConfigDescription(
            "Scales the internal lifetime of particles. Since there are different limits for different components, this scales everything proportionally.",
            new AcceptableValueRange<float>(0.1f, 5f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Gore Emission
         */
        GoreEnabled = Config.Bind(goreEmission, "Enable Gore Effects", true, new ConfigDescription(
            "Toggles whether gore effects are rendered at all. When toggled off, only the default BSG blood effects will show.",
            null,
            new ConfigurationManagerAttributes { Order = 6 }
        ));

        BloodMistEmission = Config.Bind(goreEmission, "Blood Mist Emission Rate", 1f, new ConfigDescription(
            "Adjusts the quantity of mists & puffs of blood.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 5 }
        ));

        BloodSprayEmission = Config.Bind(goreEmission, "Blood Spray Emission Rate", 0.5f, new ConfigDescription(
            "Adjusts the quantity of fine blood spray particles. Reduce if you get stutters. Above 1 gets quite CPU heavy.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        BloodBleedEmission = Config.Bind(goreEmission, "Bleed Emission Rate", 1f, new ConfigDescription(
            "Adjusts the quantity of particles open wound bleeding effects on live targets. Above 2 gets quite CPU heavy.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        BloodSquirtEmission = Config.Bind(goreEmission, "Squirt Emission Rate", 0.5f, new ConfigDescription(
            "Adjusts the quantity of the blood squirt particles. Reduce if you get stutters. Above 1 gets quite CPU heavy.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        BloodFinisherEmission = Config.Bind(goreEmission, "Finisher Emission Rate", 0.3f, new ConfigDescription(
            "Adjusts the quantity of particles in finisher effects. Reduce if you get stutters. Above 0.5 gets quite CPU heavy.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Gore Size
         */
        BloodMistSize = Config.Bind(goreSize, "Mist Size", 1f, new ConfigDescription(
            "Adjusts the size of blood mists/puffs.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 6 }
        ));

        BloodSpraySize = Config.Bind(goreSize, "Spray Size", 1f, new ConfigDescription(
            "Adjusts the size of fine blood sprays.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 5 }
        ));

        BloodBleedSize = Config.Bind(goreSize, "Bleed Drop Size", 1f, new ConfigDescription(
            "Adjusts the size of open wound bleeding on live targets.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        BloodSquibSize = Config.Bind(goreSize, "Squib Size", 1f, new ConfigDescription(
            "Adjusts the size of blood sqquibs.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        BloodSquirtSize = Config.Bind(goreSize, "Squirt Size", 1f, new ConfigDescription(
            "Adjusts the size of the blood squirts.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        BloodFinisherSize = Config.Bind(goreSize, "Finisher Gore Size", 1f, new ConfigDescription(
            "Adjusts the size of the gore generated by finisher shots.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Gore Decals
         */
        WoundDecalsEnabled = Config.Bind(goreDecals, "Wound Decals on Bodies", true, new ConfigDescription(
            "Toggles the new blood splashes appearing on bodies. If toggled off, you'll get the barely visible EFT default wound effects. Philistine.",
            null,
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        WoundDecalsSize = Config.Bind(goreDecals, "Wound Decal Size", 1f, new ConfigDescription(
            "Adjusts the size of the wound decals that appear on bodies.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        BloodSplatterDecalsEnabled = Config.Bind(goreDecals, "Blood Splatter on Environment", true, new ConfigDescription(
            "Toggles the new blood splashes appearing on the environment for penetrating hits. If toggled off, you'll get the shonky EFT defaults. Philistine.",
            null,
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        BloodSplatterDecalsSize = Config.Bind(goreDecals, "Blood Splatter Decal Size", 1f, new ConfigDescription(
            "Adjusts the size of the blood splatters on the environment.",
            new AcceptableValueRange<float>(0f, 5f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Ragdolls
         */
        bool[] ragdollAcceptableValues = visceralCombatDetected ? [false] : [false, true];
        RagdollEnabled = Config.Bind(ragdoll, "Enable Ragdoll Effects (RESTART)", !visceralCombatDetected, new ConfigDescription(
            "Toggles whether ragdoll effects will be enabled.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 4, ReadOnly = visceralCombatDetected }
        ));

        RagdollCinematicEnabled = Config.Bind(ragdoll, "Enable Cinematic Ragdolls (RESTART)", true, new ConfigDescription(
            "Adjusts the skeletal and joint characteristics of ragdolls for a more Cinematic (TM) experience.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 3, ReadOnly = visceralCombatDetected }
        ));

        RagdollDropWeaponEnabled = Config.Bind(ragdoll, "Drop Weapon on Death (RESTART)", true, new ConfigDescription(
            "Toggles the enemies dropping their weapon on death.",
            new AcceptableValueList<bool>(ragdollAcceptableValues),
            new ConfigurationManagerAttributes { Order = 2, ReadOnly = visceralCombatDetected }
        ));

        RagdollForceMultiplier = Config.Bind(ragdoll, "Ragdoll Force Multiplier", 1f, new ConfigDescription(
            "Multiplies the force that is applied to ragdolls when enemies die.",
            new AcceptableValueRange<float>(0f, 100f),
            new ConfigurationManagerAttributes { Order = 1, ReadOnly = visceralCombatDetected }
        ));

        /*
         * Misc
         */
        MiscDecalsEnabled = Config.Bind(misc, "Enable Decal Limit Adjustment (RESTART)", true, new ConfigDescription(
            "Toggles whether to override the built-in decal limits. If you have this enabled in Visceral Combat, you can disable it here.",
            null,
            new ConfigurationManagerAttributes { Order = 11 }
        ));

        MiscMaxDecalCount = Config.Bind(misc, "Decal Limits (RESTART)", 2048, new ConfigDescription(
            "Adjusts the maximum number of decals that the game will render. The vanilla number is a puny 200.",
            new AcceptableValueRange<int>(1, 2048),
            new ConfigurationManagerAttributes { Order = 10 }
        ));

        MiscMaxConcurrentParticleSys = Config.Bind(misc, "Max New Particle Systems Per Frame (RESTART)", 100, new ConfigDescription(
            "Adjusts how many new particle systems can be created per frame. The vanilla game sets it to 10. The performance impact is quite low, it's best to keep this number above 30 to allow HFX to work properly.",
            new AcceptableValueRange<int>(10, 1000),
            new ConfigurationManagerAttributes { Order = 9 }
        ));

        MiscShellLifetime = Config.Bind(misc, "Spent Shells Lifetime (seconds)", 60f, new ConfigDescription(
            "How long do spent shells stay on the ground before despawning (game default is 1 second).",
            new AcceptableValueRange<float>(0f, 3600f),
            new ConfigurationManagerAttributes { Order = 8 }
        ));

        MiscShellSize = Config.Bind(misc, "Spent Shells Size", 1.5f, new ConfigDescription(
            "Adjusts the size of spent shells multiplicatively (2 means 2x the size).",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 7 }
        ));
        MiscShellSize.SettingChanged += (_, _) => EFTHardSettings.Instance.Shells.radius = MiscShellSize.Value / 1000f;
        EFTHardSettings.Instance.Shells.radius = MiscShellSize.Value / 1000f;

        MiscShellVelocity = Config.Bind(misc, "Shell Ejection Velocity", 1.5f, new ConfigDescription(
            "Adjusts the velocity of the spent shells multiplicatively (2 means 2x the speed).",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 6 }
        ));
        MiscShellVelocity.SettingChanged += (_, _) => EFTHardSettings.Instance.Shells.velocityMult = MiscShellVelocity.Value;
        EFTHardSettings.Instance.Shells.velocityMult = MiscShellVelocity.Value;

        MiscShellPhysicsEnabled = Config.Bind(misc, "Enhanced Shell Physics (RESTART)", true, new ConfigDescription(
            "Toggles whether to enhance the spent shell physics, resulting in finer grained simulation of bouncing and rolling.",
            null,
            new ConfigurationManagerAttributes { Order = 5 }
        ));
        MiscShellPhysicsEnabled.SettingChanged += (_, _) => UpdateShellPhysics();
        UpdateShellPhysics();

        LightFlareEnabled = Config.Bind(misc, "Env. Light Flares Changes (RESTART)", true, new ConfigDescription(
            "Makes the environmental light flares more prominent and appropriate. Bright lights have bright flares, dim lights have dim flares.",
            null,
            new ConfigurationManagerAttributes { Order = 4 }
        ));

        LightFlareIntensity = Config.Bind(misc, "Env. Light Flare Intensity (RESTART)", 1f, new ConfigDescription(
            "Adjusts the intensity of environment light lens flares. Yes, I identify as a Hasselblad H6D-400C camera, thank you.",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 3 }
        ));

        LightFlareSize = Config.Bind(misc, "Env. Light Flare Size (RESTART)", 1f, new ConfigDescription(
            "Adjusts the size of environment light lens flares. Yes, I identify as a Hasselblad H6D-400C camera, thank you.",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 2 }
        ));

        KineticsScaling = Config.Bind(misc, "Bullet Kinetics Scaling", 1f, new ConfigDescription(
            "Scales the overall kinetic energy, impulse, etc.",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 1 }
        ));

        /*
         * Graphics
         */
        MipBias = Config.Bind(gfx, "Effect Quality Bias", 0f, new ConfigDescription(
            "Positive values force higher quality effect textures at a distance, lower values force lower quality. Numbers above 4 can heavy *heavy*" +
            "VRAM impact and cause stuttering.",
            new AcceptableValueRange<float>(0f, 10f),
            new ConfigurationManagerAttributes { Order = 6}
        ));
        MipBias.SettingChanged += (_, _) =>
        {
            var materialRegistry = Singleton<MaterialRegistry>.Instance;
            if (materialRegistry == null)
                return;
            materialRegistry.SetMipBias(MipBias.Value);
        };
        
            
        LodOverrideEnabled = Config.Bind(gfx, "Override LOD Settings (RESTART)", false, new ConfigDescription(
            "Toggles whether the standard LOD settings should be overridden. ",
            null,
            new ConfigurationManagerAttributes { Order = 5 }
        ));
        LodOverrideEnabled.SettingChanged += (_, _) =>
        {
            if (LodOverrideEnabled.Value)
                QualitySettings.lodBias = LodBias.Value;

            if (LodBias.Description.Tags[0] is not ConfigurationManagerAttributes configAttr) return;

            configAttr.Browsable = LodOverrideEnabled.Value;
        };
        LodBias = Config.Bind(gfx, "LOD Bias", QualitySettings.lodBias, new ConfigDescription(
            "Adjust the LOD bias in a wider range than what the game allows.",
            new AcceptableValueRange<float>(1f, 20f),
            new ConfigurationManagerAttributes { Order = 4, Browsable = LodOverrideEnabled.Value }
        ));
        LodBias.SettingChanged += (_, _) =>
        {
            if (LodOverrideEnabled.Value)
                QualitySettings.lodBias = LodBias.Value;
        };
        if (LodOverrideEnabled.Value)
            QualitySettings.lodBias = LodBias.Value;

        TerrainDetailOverrideEnabled = Config.Bind(gfx, "Override Terrain Detail (RESTART)", false, new ConfigDescription(
            "Toggles whether the terrain details settings should be overridden.",
            null,
            new ConfigurationManagerAttributes { Order = 3 }
        ));
        TerrainDetailOverrideEnabled.SettingChanged += (_, _) =>
        {
            if (TerrainDetailDistance.Description.Tags[0] is ConfigurationManagerAttributes distanceConfigAttr)
            {
                distanceConfigAttr.Browsable = TerrainDetailOverrideEnabled.Value;
            }

            if (TerrainDetailDensityScaling.Description.Tags[0] is ConfigurationManagerAttributes densityConfigAttr)
            {
                densityConfigAttr.Browsable = TerrainDetailOverrideEnabled.Value;
            }
        };
        TerrainDetailDistance = Config.Bind(gfx, "Terrain Detail LOD Scaling", 2.5f, new ConfigDescription(
            "Set the maximum visible distance for terrain detail like rocks and foliage. For some unfathomable reason this is separate" +
            "from the regular LOD",
            new AcceptableValueRange<float>(0.5f, 10f),
            new ConfigurationManagerAttributes { Order = 2, Browsable = TerrainDetailOverrideEnabled.Value }
        ));
        TerrainDetailDensityScaling = Config.Bind(gfx, "Terrain Detail Density", 2f, new ConfigDescription(
            "Scales the density of terrain detail like rocks and foliage.",
            new AcceptableValueRange<float>(0.5f, 5f),
            new ConfigurationManagerAttributes { Order = 1, Browsable = TerrainDetailOverrideEnabled.Value }
        ));

        /*
         * Whimsy
         */
        _michelinManEnabled = Config.Bind(whimsy, "AcidPhantasm Michelin Man Mode", false, new ConfigDescription(
            "Nunc est Bibendum.",
            null,
            new ConfigurationManagerAttributes { Order = 2 }
        ));
        _michelinManEnabled.SettingChanged += (_, _) =>
        {
            if (_michelinManEnabled.Value)
                _michelinManPatch.Enable();
            else
                _michelinManPatch.Disable();
        };
        if (_michelinManEnabled.Value)
            _michelinManPatch.Enable();

        _peenEnabled = Config.Bind(whimsy, "Peen", false, new ConfigDescription(
            "Made you look.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));
        _peenEnabled.SettingChanged += (_, _) => ErrorPlayerFeedback("Made you look!");

        /*
         * Deboog
         */
        _loggingEnabled = Config.Bind(debug, "Enable Debug Logging (RESTART)", false, new ConfigDescription(
            "Duh. Requires restarting the game to take effect.",
            null,
            new ConfigurationManagerAttributes { Order = 1 }
        ));
    }

    public static void ErrorPlayerFeedback(string message)
    {
        NotificationManagerClass.DisplayWarningNotification(message, ENotificationDurationType.Long);
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
    }

    public static void UpdateShellPhysics()
    {
        if (MiscShellPhysicsEnabled.Value)
        {
            Log.LogInfo("Enabling Enhanced Shell Physics");
            EFTHardSettings.Instance.Shells.maxCastCount = 100;
            EFTHardSettings.Instance.Shells.deltaTimeStep = 0.15f;
            // EFTHardSettings.Instance.Shells.bounceSpeedMult = 1.0f;
        }
        else
        {
            Log.LogInfo("Disabling Enhanced Shell Physics");
            EFTHardSettings.Instance.Shells.maxCastCount = 10;
            EFTHardSettings.Instance.Shells.deltaTimeStep = 0.3f;
            // EFTHardSettings.Instance.Shells.bounceSpeedMult = 1.0f;
        }
    }
}