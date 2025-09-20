using Comfort.Common;
using HollywoodFX.Concussion;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class BlastController : MonoBehaviour
{
    private BlastPool<ConfinedBlast> _dynamicBlastPool;
    private BlastPool<Blast> _flashbangBlastPool;
    
    public void Init(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading explosion prefabs");
        var dynamicPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Dynamic");
        var flashbangPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Explosion Flash");

        Plugin.Log.LogInfo("Creating blast pools");
        _dynamicBlastPool = new BlastPool<ConfinedBlast>(eftEffects, dynamicPrefab, BuildDynamicExplosion, 15, 20f);
        _flashbangBlastPool = new BlastPool<Blast>(eftEffects, flashbangPrefab, BuildFlashbang, 15, 10f);
        
        Plugin.Log.LogInfo("Creating concussion handling");
    }

    private void Update()
    {
        _dynamicBlastPool.Update();
        _flashbangBlastPool.Update();
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
            eftEffects, 6f, Mathf.Sqrt(0.125f / Plugin.ComputeFidelity.Value),
            premade, mainEffects["Splash_Up"], mainEffects["Splash_Generic"], mainEffects["Splash_Front"], mainEffects["Splash_Dust"],
            // These are pre-baked effects and we apply the density scaling here
            ScaleDensity(mainEffects["Dyn_Trail_Smoke"]), ScaleDensity(mainEffects["Dyn_Trail_Sparks"]),
            mainEffects["Dyn_Dust"], mainEffects["Dyn_Dust_Ring"], mainEffects["Dyn_Sparks"],
            mainEffects["Dyn_Debris_Rock"], mainEffects["Dyn_Debris_Generic"]
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

    // private static Blast BuildPremadeExplosion(Effects eftEffects, GameObject prefab)
    // {
    //     var mainEffects = EffectBundle.LoadPrefab(eftEffects, prefab, true);
    //
    //     EffectBundle[] explosionEffectsUp =
    //     [
    //         ScaleDensity(mainEffects["Fireball"]),
    //         mainEffects["Splash"],
    //         mainEffects["Shockwave"],
    //     ];
    //
    //     EffectBundle[] explosionEffectsAngled =
    //     [
    //         ScaleDensity(mainEffects["Debris_Glow"]),
    //         ScaleDensity(mainEffects["Debris_Generic"]),
    //         ScaleDensity(mainEffects["Dust"]),
    //         ScaleDensity(mainEffects["Dust_Linger"]),
    //         ScaleDensity(mainEffects["Sparks"]),
    //     ];
    //
    //     return new Blast(explosionEffectsUp, explosionEffectsAngled);
    // }

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
        foreach (var emitter in effects.Emitters)
        {
            foreach (var subSystem in emitter.Main.GetComponentsInChildren<ParticleSystem>())
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

                if (Mathf.Approximately(densityScaling, 1f))
                    continue;

                ParticleHelpers.ScaleEmissionRate(subSystem, densityScaling);
            }
        }

        return effects;
    }

    public void Emit(string id, Vector3 position, Vector3 normal)
    {
        if (id.StartsWith("big_round") || id.StartsWith("grenade_smoke"))
            return;

        if (id.StartsWith("Flashbang"))
        {
            _flashbangBlastPool.Emit(position, normal);
        }
        // else if (id.StartsWith("small") || name.StartsWith("Small"))
        // {
        //     _dynamicBlastPool.Emit(position, normal);
        // }
        else
        {
            _dynamicBlastPool.Emit(position, normal);
        }
        
        if (!Plugin.ConcussionEnabled.Value || Singleton<ConcussionController>.Instance == null)
            return;

        var duration = 4f * Plugin.ConcussionDuration.Value;
        Singleton<ConcussionController>.Instance.Apply(position, duration, 12f * Plugin.ConcussionRange.Value, duration);
    }
}