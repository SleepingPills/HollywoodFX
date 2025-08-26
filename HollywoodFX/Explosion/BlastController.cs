using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

// Unity doesn't support generic typed components
internal class DynamicBlastPoolScheduler : BlastPoolScheduler<ConfinedBlast>;
internal class StaticBlastPoolScheduler : BlastPoolScheduler<Blast>;

public class BlastController
{
    private readonly BlastPool<ConfinedBlast> _dynamicBlastPool;
    private readonly BlastPool<Blast> _premadeBlastPool;
    private readonly BlastPool<Blast> _flashbangBlastPool;
    
    public BlastController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Explosion Prefabs");
        var dynamicPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Dynamic");
        var premadePrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Small");
        var flashbangPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Flash");

        Plugin.Log.LogInfo("Creating blast pools");
        _dynamicBlastPool = new BlastPool<ConfinedBlast>(eftEffects, dynamicPrefab, BuildDynamicExplosion, 15, 20f);
        _premadeBlastPool = new BlastPool<Blast>(eftEffects, premadePrefab, BuildPremadeExplosion, 30, 10f);
        _flashbangBlastPool = new BlastPool<Blast>(eftEffects, flashbangPrefab, BuildFlashbang, 15, 10f);

        Plugin.Log.LogInfo("Creating dynamic blast scheduler");
        var schedulerDynamic = eftEffects.gameObject.AddComponent<DynamicBlastPoolScheduler>();
        schedulerDynamic.Add(_dynamicBlastPool);
        
        Plugin.Log.LogInfo("Creating static blast scheduler");
        var schedulerStatic = eftEffects.gameObject.AddComponent<StaticBlastPoolScheduler>();
        schedulerStatic.Add(_premadeBlastPool);
        schedulerStatic.Add(_flashbangBlastPool);
    }

    private static ConfinedBlast BuildDynamicExplosion(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] premade =
        [
            ScaleDensity(mainEffects["Fireball"]),
            mainEffects["Glow"],
            mainEffects["Shockwave"],
            ScaleDensity(mainEffects["Debris_Burning"]),
            ScaleDensity(mainEffects["Debris_Generic"]),
            ScaleDensity(mainEffects["Dust_Linger"]),
        ];

        return new ConfinedBlast(
            eftEffects, 6f, Mathf.Sqrt(0.125f),
            premade, mainEffects["Splash_Up"], mainEffects["Splash_Generic"], mainEffects["Splash_Front"],
            // These are pre-baked effects and we apply the density scaling here
            ScaleDensity(mainEffects["Dyn_Trail_Smoke"]), ScaleDensity(mainEffects["Dyn_Trail_Sparks"]),
            mainEffects["Dyn_Dust"], mainEffects["Dyn_Dust_Ring"], mainEffects["Dyn_Sparks"],
            mainEffects["Dyn_Sparks_Bright"]
        );
    }

    // private static Blast BuildExplosionMid(Effects eftEffects, GameObject prefab)
    // {
    //     var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);
    //
    //     EffectBundle[] explosionEffectsUp =
    //     [
    //         ScaleDensity(mainEffects["Fireball"]),
    //         mainEffects["Glow"],
    //         mainEffects["Splash"],
    //         mainEffects["Shockwave"],
    //         ScaleDensity(mainEffects["Debris_Burning"]),
    //         ScaleDensity(mainEffects["Dust_Linger"]),
    //     ];
    //
    //     EffectBundle[] explosionEffectsAngled =
    //     [
    //         ScaleDensity(mainEffects["Debris_Glow"]),
    //         ScaleDensity(mainEffects["Debris_Generic"]),
    //         ScaleDensity(mainEffects["Sparks"]),
    //         ScaleDensity(mainEffects["Dust_Ring"]),
    //     ];
    //
    //     return new Blast(explosionEffectsUp, explosionEffectsAngled);
    // }

    private static Blast BuildPremadeExplosion(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] explosionEffectsUp =
        [
            ScaleDensity(mainEffects["Fireball"]),
            mainEffects["Splash"],
            mainEffects["Shockwave"],
        ];

        EffectBundle[] explosionEffectsAngled =
        [
            ScaleDensity(mainEffects["Debris_Glow"]),
            ScaleDensity(mainEffects["Debris_Generic"]),
            ScaleDensity(mainEffects["Dust"]),
            ScaleDensity(mainEffects["Dust_Linger"]),
            ScaleDensity(mainEffects["Sparks"]),
        ];

        return new Blast(explosionEffectsUp, explosionEffectsAngled);
    }

    private static Blast BuildFlashbang(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] explosionEffectsUp =
        [
            ScaleDensity(mainEffects["Fireball"]),
            mainEffects["Splash"]
        ];

        EffectBundle[] explosionEffectsAngled =
        [
            ScaleDensity(mainEffects["Debris_Glow"]),
            ScaleDensity(mainEffects["Dust"]),
            ScaleDensity(mainEffects["Dust_Linger"]),
            ScaleDensity(mainEffects["Sparks"]),
            ScaleDensity(mainEffects["Sparks Bright"]),
        ];

        return new Blast(explosionEffectsUp, explosionEffectsAngled);
    }

    private static EffectBundle ScaleDensity(EffectBundle effects, float scale = 1f)
    {
        foreach (var system in effects.ParticleSystems)
        {
            foreach (var subSystem in system.GetComponentsInChildren<ParticleSystem>())
            {
                var densityScaling = 1f;

                var name = subSystem.name.ToLower();

                if (name.Contains("fireball"))
                    densityScaling = Plugin.ExplosionDensityFireball.Value;
                else if (name.Contains("debris"))
                    densityScaling = Plugin.ExplosionDensityDebris.Value;
                else if (name.Contains("smoke"))
                    densityScaling = Plugin.ExplosionDensitySmoke.Value;
                else if (name.Contains("sparks"))
                    densityScaling = Plugin.ExplosionDensitySparks.Value;
                else if (name.Contains("dust"))
                    densityScaling = Plugin.ExplosionDensityDust.Value;

                densityScaling *= scale;

                Plugin.Log.LogInfo($"Scaling explosion effect: {subSystem.name} {densityScaling}");

                if (Mathf.Approximately(densityScaling, 1f))
                    continue;

                ParticleHelpers.ScaleEmissionRate(subSystem, densityScaling);
            }
        }

        return effects;
    }

    public void Emit(string name, Vector3 position, Vector3 normal)
    {
        if (name.StartsWith("big_round"))
            return;

        // if (name.StartsWith("Flashbang"))
        // {
        //     _flashbangBlastPool.Emit(position, normal);
        // }
        // else if (name.StartsWith("small") || name.StartsWith("Small"))
        // {
        //     _smallGrenadeBlastPool.Emit(position, normal);
        // }
        // else
        // {
        //     _handGrenadeBlastPool.Emit(position, normal);
        // }

        _dynamicBlastPool.Emit(position, normal);
    }
}