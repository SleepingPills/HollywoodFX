using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class ExplosionController
{
    private readonly ExplosionPool _handGrenadeExplosionPool;
    private readonly ExplosionPool _smallGrenadeExplosionPool;
    
    public ExplosionController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Explosion Prefabs");
        var expMidPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Mid");
        
        _handGrenadeExplosionPool = new ExplosionPool(eftEffects, expMidPrefab, BuildExplosionMid, 15, 20f);
        _smallGrenadeExplosionPool = new ExplosionPool(eftEffects, expMidPrefab, BuildExplosionSmall, 30, 10f);

        var scheduler = eftEffects.gameObject.AddComponent<ExplosionPoolScheduler>();
        scheduler.Pools.Add(_handGrenadeExplosionPool);
    }

    private Explosion BuildExplosionMid(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] explosionEffectsUp =
        [
            ScaleDensity(mainEffects["Fireball_Mid"]),
            mainEffects["Glow_Mid"],
            mainEffects["Splash_Mid"],
            mainEffects["Shockwave_Mid"],
            ScaleDensity(mainEffects["Debris_Burning_Mid"]),
            ScaleDensity(mainEffects["Dust_Linger_Mid"]),
        ];

        EffectBundle[] explosionEffectsAngled =
        [
            ScaleDensity(mainEffects["Debris_Glow_Mid"]),
            ScaleDensity(mainEffects["Debris_Rock_Mid"]),
            ScaleDensity(mainEffects["Sparks_Mid"]),
            ScaleDensity(mainEffects["Dust_Ring_Mid"]),
        ];

        return new Explosion(explosionEffectsUp, explosionEffectsAngled, 1f);
    }

    private Explosion BuildExplosionSmall(Effects eftEffects, GameObject prefab)
    {
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);

        EffectBundle[] explosionEffectsUp =
        [
            ScaleDensity(mainEffects["Fireball_Mid"], 1.5f),
            mainEffects["Splash_Mid"],
            mainEffects["Shockwave_Mid"],
        ];

        EffectBundle[] explosionEffectsAngled =
        [
            ScaleDensity(mainEffects["Debris_Glow_Mid"], 0.25f),
            ScaleDensity(mainEffects["Sparks_Mid"], 0.25f),
            ScaleDensity(mainEffects["Dust_Ring_Mid"], 0.25f),
        ];

        return new Explosion(explosionEffectsUp, explosionEffectsAngled, 0.5f);
    }
    
    private static EffectBundle ScaleDensity(EffectBundle effects, float scale=1f)
    {
        foreach (var system in effects.ParticleSystems)
        {
            foreach (var subSystem in system.GetComponentsInChildren<ParticleSystem>())
            {
                var densityScaling = 1f;
                
                if (subSystem.name.ToLower().Contains("fireball"))
                    densityScaling = Plugin.ExplosionDensityFireball.Value;
                else if (subSystem.name.ToLower().Contains("debris"))
                    densityScaling = Plugin.ExplosionDensityDebris.Value;
                else if (subSystem.name.ToLower().Contains("smoke"))
                    densityScaling = Plugin.ExplosionDensitySmoke.Value;
                else if (subSystem.name.ToLower().Contains("sparks"))
                    densityScaling = Plugin.ExplosionDensitySparks.Value;
                else if (subSystem.name.ToLower().Contains("dust"))
                    densityScaling = Plugin.ExplosionDensityDust.Value;
                
                if (Mathf.Approximately(densityScaling, 1f))
                    continue;
                
                ParticleHelpers.ScaleEmissionRate(subSystem, densityScaling * scale);
            }
        }
        
        return effects;
    }

    public void Emit(string name, Vector3 position, Vector3 normal)
    {
        if (name.StartsWith("Flashbang"))
        {
            
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