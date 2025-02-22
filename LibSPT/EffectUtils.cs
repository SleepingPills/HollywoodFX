using System.Collections.Generic;
using System.Linq;
using EFT.Particles;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

    internal static class EffectUtils
    {
        public static Dictionary<string, Effects.Effect> LoadEffects(Effects cannedEffects, GameObject impactsPrefab)
        {
            Plugin.Log.LogInfo("Instantiating Impact Effects Prefab");
            var impactInstance = Object.Instantiate(impactsPrefab);
            Plugin.Log.LogInfo("Getting Effects Component");
            var impactEffects = impactInstance.GetComponent<Effects>();
            Plugin.Log.LogInfo($"Loaded {impactEffects.EffectsArray.Length} extra effects");

            Plugin.Log.LogInfo("Replacing transform parent with internal effects instance");
            foreach (var child in impactInstance.transform.GetChildren())
            {
                child.parent = cannedEffects.transform;
            }

            Plugin.Log.LogInfo("Adding new effects to the internal effects instance");
            List<Effects.Effect> customEffectsList = [];
            customEffectsList.AddRange(cannedEffects.EffectsArray);
            customEffectsList.AddRange(impactEffects.EffectsArray);

            cannedEffects.EffectsArray = [.. customEffectsList];

            return impactEffects.EffectsArray.ToDictionary(x => x.Name, x => x);
        }

        public static void ScaleEffect(Effects.Effect effect, float sizeScaling, float emissionScaling)
        {
            var mediator = effect.BasicParticleSystemMediator;

            var particleSystems = GetMediatorParticleSystems(mediator);

            if (particleSystems == null)
                return;

            if (!Mathf.Approximately(sizeScaling, 1f))
            {
                foreach (var particleSystem in particleSystems)
                {
                    Plugin.Log.LogInfo($"Scaling size for {effect.Name} particle system {particleSystem.name}");
                    particleSystem.transform.localScale *= sizeScaling;
                }
            }

            if (Mathf.Approximately(emissionScaling, 1f)) return;

            foreach (var particleSystem in particleSystems)
            {
                Plugin.Log.LogInfo($"Scaling emission for {effect.Name} particle system {particleSystem.name}");

                var main = particleSystem.main;
                main.maxParticles = (int)(main.maxParticles * emissionScaling);

                // We skip the rateOver[X]Multiplier as these have natural scaling over distance, no need to increase the density
                var emission = particleSystem.emission;

                for (var i = 0; i < emission.burstCount; i++)
                {
                    var burst = emission.GetBurst(i);
                    burst.minCount = CalcBurstCount(burst.minCount, emissionScaling);
                    burst.maxCount = CalcBurstCount(burst.maxCount, emissionScaling);
                    emission.SetBurst(i, burst);
                }
            }
        }

        public static ParticleSystem[] GetMediatorParticleSystems(BasicParticleSystemMediator mediator)
        {
            return Traverse.Create(mediator).Field("_particleSystems").GetValue<ParticleSystem[]>();
        }

        public static short CalcBurstCount(short count, float scaling)
        {
            // Don't try to scale single particle emissions
            if (count < 2)
            {
                return count;
            }

            // Clip the lower value to 1
            return (short)Mathf.Max(count * scaling, 1f);
        }
    }
