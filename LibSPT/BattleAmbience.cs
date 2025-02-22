using Comfort.Common;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX;

    internal class BattleAmbience
    {
        private readonly Effects.Effect[] _cloudSmoke;
        private readonly Effects.Effect[] _suspendedDust;
        private readonly float _kineticEnergyNormFactor;

        public BattleAmbience(Effects cannedEffects, GameObject prefab)
        {
            Plugin.Log.LogInfo("Building Battle Ambience Effects");

            var effectMap = EffectUtils.LoadEffects(cannedEffects, prefab);

            foreach (var effect in effectMap.Values)
            {
                Plugin.Log.LogInfo($"Effect {effect.Name} emission scaling: {Plugin.AmbientEffectDensity.Value}");
                ScaleEffect(effect, Plugin.AmbientParticleLifetime.Value, Plugin.AmbientParticleLimit.Value, Plugin.AmbientEffectDensity.Value);
                Singleton<LitMaterialRegistry>.Instance.Register(effect, false);
            }

            _cloudSmoke = [effectMap["Cloud_Smoke_1"]];
            _suspendedDust = [effectMap["Suspended_Dust_1"], effectMap["Suspended_Glitter_1"]];
            _kineticEnergyNormFactor = Plugin.ChonkEffectEnergy.Value;
        }

        public void Emit(ImpactContext context)
        {
            var emissionChance = 0.3 * (context.KineticEnergy / _kineticEnergyNormFactor);

            if (Random.Range(0f, 1f) < emissionChance)
            {
                var smokeEffect = _cloudSmoke[Random.Range(0, _cloudSmoke.Length)];
                context.EmitEffect(smokeEffect);
            }

            if (!(Random.Range(0f, 1f) < emissionChance)) return;

            var dustEffect = _suspendedDust[Random.Range(0, _suspendedDust.Length)];
            context.EmitEffect(dustEffect);
        }

        private static void ScaleEffect(Effects.Effect effect, float lifetimeScaling, float limitScaling, float emissionScaling)
        {
            var particleSystems = EffectUtils.GetMediatorParticleSystems(effect.BasicParticleSystemMediator);

            if (particleSystems == null)
                return;

            if (Mathf.Approximately(emissionScaling, 1f)) return;

            foreach (var particleSystem in particleSystems)
            {
                var main = particleSystem.main;

                if (!Mathf.Approximately(limitScaling, 1))
                {
                    main.maxParticles = (int)(main.maxParticles * limitScaling);
                }

                if (!Mathf.Approximately(lifetimeScaling, 1))
                {
                    var lifetime = main.startLifetime;
                    lifetime.constant *= lifetimeScaling;
                    lifetime.constantMin *= lifetimeScaling;
                    lifetime.constantMax *= lifetimeScaling;
                    lifetime.curveMultiplier = lifetimeScaling;
                }

                if (Mathf.Approximately(emissionScaling, 1)) continue;

                var emission = particleSystem.emission;

                for (var i = 0; i < emission.burstCount; i++)
                {
                    var burst = emission.GetBurst(i);
                    burst.minCount = EffectUtils.CalcBurstCount(burst.minCount, emissionScaling);
                    burst.maxCount = EffectUtils.CalcBurstCount(burst.maxCount, emissionScaling);
                    emission.SetBurst(i, burst);
                }
            }
        }
    }
