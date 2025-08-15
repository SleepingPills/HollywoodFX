using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class BlastController
{
    private readonly BlastPool _handGrenadeBlastPool;
    private readonly BlastPool _smallGrenadeBlastPool;
    private readonly BlastPool _flashbangBlastPool;

    private readonly ConfinedBlast _testBlast;

    public BlastController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Explosion Prefabs");
        var expMidPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Mid");
        var expSmallPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Small");
        var expFlashbangPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Flash");

        _handGrenadeBlastPool = new BlastPool(eftEffects, expMidPrefab, BuildExplosionMid, 15, 20f);
        _smallGrenadeBlastPool = new BlastPool(eftEffects, expSmallPrefab, BuildExplosionSmall, 30, 10f);
        _flashbangBlastPool = new BlastPool(eftEffects, expFlashbangPrefab, BuildExplosionFlashbang, 15, 10f);

        _testBlast = new ConfinedBlast(eftEffects, 6f, Mathf.Sqrt(0.125f));

        var scheduler = eftEffects.gameObject.AddComponent<BlastPoolScheduler>();
        scheduler.Pools.Add(_handGrenadeBlastPool);
    }

    private static Blast BuildExplosionMid(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] explosionEffectsUp =
        [
            ScaleDensity(mainEffects["Fireball"]),
            mainEffects["Glow"],
            mainEffects["Splash"],
            mainEffects["Shockwave"],
            ScaleDensity(mainEffects["Debris_Burning"]),
            ScaleDensity(mainEffects["Dust_Linger"]),
        ];

        EffectBundle[] explosionEffectsAngled =
        [
            ScaleDensity(mainEffects["Debris_Glow"]),
            ScaleDensity(mainEffects["Debris_Generic"]),
            ScaleDensity(mainEffects["Sparks"]),
            ScaleDensity(mainEffects["Dust_Ring"]),
        ];

        return new Blast(explosionEffectsUp, explosionEffectsAngled);
    }

    private static Blast BuildExplosionSmall(Effects eftEffects, GameObject prefab)
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

    private static Blast BuildExplosionFlashbang(Effects eftEffects, GameObject prefab)
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

        if (name.StartsWith("Flashbang"))
        {
            _flashbangBlastPool.Emit(position, normal);
        }
        else if (name.StartsWith("small") || name.StartsWith("Small"))
        {
            _smallGrenadeBlastPool.Emit(position, normal);
        }
        else
        {
            _handGrenadeBlastPool.Emit(position, normal);
        }
        
        _testBlast.Emit(position, normal);
    }
}