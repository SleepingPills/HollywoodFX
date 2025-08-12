using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class ExplosionController
{
    private readonly ExplosionPool _handGrenadeExplosionPool;
    private readonly ExplosionPool _smallGrenadeExplosionPool;
    private readonly ExplosionPool _flashbangExplosionPool;

    public ExplosionController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Explosion Prefabs");
        var expMidPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Mid");
        var expSmallPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Small");
        var expFlashbangPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Flash");

        _handGrenadeExplosionPool = new ExplosionPool(eftEffects, expMidPrefab, BuildExplosionMid, 15, 20f);
        _smallGrenadeExplosionPool = new ExplosionPool(eftEffects, expSmallPrefab, BuildExplosionSmall, 30, 10f);
        _flashbangExplosionPool = new ExplosionPool(eftEffects, expFlashbangPrefab, BuildExplosionFlashbang, 15, 10f);

        var scheduler = eftEffects.gameObject.AddComponent<ExplosionPoolScheduler>();
        scheduler.Pools.Add(_handGrenadeExplosionPool);
    }

    private static Explosion BuildExplosionMid(Effects eftEffects, GameObject prefab)
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

        return new Explosion(explosionEffectsUp, explosionEffectsAngled);
    }

    private static Explosion BuildExplosionSmall(Effects eftEffects, GameObject prefab)
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

        return new Explosion(explosionEffectsUp, explosionEffectsAngled);
    }

    private static Explosion BuildExplosionFlashbang(Effects eftEffects, GameObject prefab)
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

        return new Explosion(explosionEffectsUp, explosionEffectsAngled);
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
            _flashbangExplosionPool.Emit(position, normal);
        }
        else if (name.StartsWith("small") || name.StartsWith("Small"))
        {
            _smallGrenadeExplosionPool.Emit(position, normal);
        }
        else
        {
            _handGrenadeExplosionPool.Emit(position, normal);
        }
    }
}