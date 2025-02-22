using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT.Ballistics;
using JetBrains.Annotations;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace System.Runtime.CompilerServices
{
    // ReSharper disable once UnusedType.Global
    internal static class IsExternalInit
    {
    }
}

namespace HollywoodFX
{
    internal readonly struct DirectionalImpact(
        Effects.Effect[] effects,
        float chance = 1f,
        CamDir camDir = CamDir.None,
        WorldDir worldDir = WorldDir.None
    )
    {
        public readonly Effects.Effect[] Effects = effects;
        public readonly WorldDir WorldDir = worldDir;
        public readonly CamDir CamDir = camDir;
        public readonly float Chance = chance;
    }

    internal class ImpactSystem(
        DirectionalImpact[] directional,
        [CanBeNull] Effects.Effect[] generic = null,
        float forceGeneric = 0f
    )
    {
        public void Emit(ImpactContext context)
        {
            Effects.Effect genericEffect = null;

            if (generic != null && context.CamOrientation.HasFlag(CamDir.Angled))
            {
                genericEffect = generic[Random.Range(0, generic.Length)];

                if (Random.Range(0f, 1f) < forceGeneric)
                {
                    context.EmitEffect(genericEffect);
                    return;
                }
            }

            var hasEmitted = false;

            foreach (var impact in directional)
            {
                if (!context.CamOrientation.HasFlag(impact.CamDir) || !context.WorldOrientation.HasFlag(impact.WorldDir)) continue;
                if (!(Random.Range(0f, 1f) < impact.Chance)) continue;

                var effect = impact.Effects[Random.Range(0, impact.Effects.Length)];

                context.EmitEffect(effect);

                hasEmitted = true;
            }

            if (hasEmitted || genericEffect == null) return;

            context.EmitEffect(genericEffect);
        }
    }
    
    internal class ImpactEffects
    {
        private readonly List<ImpactSystem>[] _smallCaliberImpacts;
        private readonly List<ImpactSystem>[] _midCaliberImpacts;
        private readonly List<ImpactSystem>[] _chonkCaliberImpacts;

        public ImpactEffects(Effects cannedEffects, GameObject prefab)
        {
            _midCaliberImpacts = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];

            _smallCaliberImpacts = BuildCoreImpactSystems(cannedEffects, prefab, Plugin.SmallEffectSize.Value);
            _midCaliberImpacts = BuildCoreImpactSystems(cannedEffects, prefab, Plugin.MediumEffectSize.Value);
            _chonkCaliberImpacts = BuildCoreImpactSystems(cannedEffects, prefab, Plugin.ChonkEffectSize.Value);
        }

        public void Emit(ImpactContext context)
        {
            var impactChoice = _midCaliberImpacts;

            if (context.KineticEnergy <= Plugin.SmallEffectEnergy.Value)
            {
                impactChoice = _smallCaliberImpacts;
            }
            else if (context.KineticEnergy >= Plugin.ChonkEffectEnergy.Value)
            {
                impactChoice = _chonkCaliberImpacts;
            }

            var currentSystems = impactChoice[(int)context.Material];

            if (currentSystems == null)
                return;

            foreach (var impactSystem in currentSystems)
            {
                impactSystem.Emit(context);
            }
        }

        private static List<ImpactSystem>[] BuildCoreImpactSystems(Effects cannedEffects, GameObject impactsPrefab, float scaling)
        {
            var effectMap = EffectUtils.LoadEffects(cannedEffects, impactsPrefab);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var effect in effectMap.Values)
            {
                Singleton<LitMaterialRegistry>.Instance.Register(effect, true);
            }

            // Debris chance has linear scaling below 1, quadratic above. This ensures visible difference for large calibers without suppressing
            // things too much for smaller ones.
            var debrisChanceScale = scaling < 1f ? scaling : Mathf.Pow(scaling, 2f);

            // Emissions only scale up, and that's as square root to avoid generating too much noise
            var emissionScaling = Mathf.Max(Mathf.Sqrt(scaling), 1);

            foreach (var effect in effectMap.Values)
            {
                var sizeScaling = scaling;

                // Clip the debris size to 1 otherwise things start looking silly. We still increase the quantity.
                if (effect.Name.Contains("Debris"))
                {
                    sizeScaling = Mathf.Min(sizeScaling, 1f);
                }
                else if (effect.Name.Contains("Squirt") || effect.Name.Contains("Splash") || effect.Name.Contains("Flash"))
                {
                    sizeScaling = sizeScaling < 1f ? sizeScaling : Mathf.Sqrt(sizeScaling);
                }

                Plugin.Log.LogInfo($"Effect {effect.Name} size scaling: {sizeScaling}, emission scaling: {emissionScaling}");

                EffectUtils.ScaleEffect(effect, sizeScaling, emissionScaling);
            }

            return DefineImpactSystems(effectMap, debrisChanceScale);
        }

        private static List<ImpactSystem>[] DefineImpactSystems(Dictionary<string, Effects.Effect> effectMap, float debrisChanceScale)
        {
            Plugin.Log.LogInfo("Constructing impact systems");

            // Define major building blocks for systems
            Plugin.Log.LogInfo("Building frontal puffs");
            var puffFront = new[]
            {
                effectMap["Puff_Front_1"], effectMap["Puff_Front_2"]
            };

            var puffFrontBody = new[]
            {
                effectMap["Puff_Body_Front_1"], effectMap["Puff_Body_Front_2"]
            };

            var puffFrontDusty = new[]
            {
                effectMap["Puff_Dusty_Front_1"], effectMap["Puff_Dusty_Front_2"], effectMap["Puff_Dusty_Front_3"]
            };

            var puffFrontRock = puffFront.Concat(puffFrontDusty).ToArray();

            Plugin.Log.LogInfo("Building flash sparks");
            var flashSparks = new[]
            {
                effectMap["Flash_Sparks_1"], effectMap["Flash_Sparks_2"], effectMap["Flash_Sparks_3"]
            };

            Plugin.Log.LogInfo("Building generic puffs");
            var puffGeneric = new[]
            {
                effectMap["Puff_1"], effectMap["Puff_2"], effectMap["Puff_3"], effectMap["Puff_4"]
            };

            Plugin.Log.LogInfo("Building horizontal puffs");
            var puffGenericHorRight = new[]
            {
                effectMap["Puff_Dusty_Hor_Right_1"], effectMap["Puff_Dusty_Hor_Right_2"]
            };

            var puffGenericHorLeft = new[]
            {
                effectMap["Puff_Dusty_Hor_Left_1"], effectMap["Puff_Dusty_Hor_Left_2"]
            };

            Plugin.Log.LogInfo("Building lingering puffs");
            var puffLinger = new[]
            {
                effectMap["Puff_Smoke_Linger_1"]
            };
            
            Plugin.Log.LogInfo("Building puff rings");
            var puffRing = new[]
            {
                effectMap["Puff_Smoke_Ring_1"]
            };

            Plugin.Log.LogInfo("Building mud debris");
            var debrisMudVert = new[]
            {
                effectMap["Debris_Mud_Vert_1"], effectMap["Debris_Mud_Vert_2"], effectMap["Debris_Mud_Vert_3"]
            };

            Plugin.Log.LogInfo("Building dirt debris");
            var debrisDirtVert = new[]
            {
                effectMap["Debris_Dirt_Vert_1"], effectMap["Debris_Dirt_Vert_2"], effectMap["Debris_Dirt_Vert_3"]
            };
            
            Plugin.Log.LogInfo("Building rock debris");
            var debrisRock = new[]
            {
                effectMap["Debris_Rock_1"]
            };

            Plugin.Log.LogInfo("Building fine dust");
            var debrisDust = new[]
            {
                effectMap["Fine_Dust_1"]
            };

            Plugin.Log.LogInfo("Building fine spark");
            var debrisSparksLight = new[]
            {
                effectMap["Fine_Sparks_Light_1"], effectMap["Fine_Dust_1"], effectMap["Fine_Dust_1"], effectMap["Fine_Dust_1"],
                effectMap["Fine_Dust_1"]
            };

            var debrisSparksMetal = new[]
            {
                effectMap["Fine_Sparks_Metal_1"], effectMap["Fine_Sparks_Metal_1"], effectMap["Fine_Sparks_Light_1"]
            };

            Plugin.Log.LogInfo("Building generic debris");
            var debrisGeneric = new[]
            {
                effectMap["Debris_Generic_1"]
            };

            Plugin.Log.LogInfo("Building misc debris");
            var bulletHoleSmoke = new[]
            {
                effectMap["Impact_Smoke_1"]
            };

            var debrisSparksDrip = new[]
            {
                effectMap["Debris_Sparks_Drip_1"]
            };

            var fallingDust = new[]
            {
                effectMap["Falling_Dust_1"]
            };

            Plugin.Log.LogInfo("Defining material specific impacts");
            var softRockImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFrontRock),
                        new DirectionalImpact(puffLinger, chance: 0.25f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.5f * debrisChanceScale),
                        new DirectionalImpact(debrisDust, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(debrisRock, chance: 0.33f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.2f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f * debrisChanceScale),
                    ]
                )
            };

            var hardRockImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.75f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFrontRock),
                        new DirectionalImpact(puffLinger, chance: 0.25f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(debrisSparksLight, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.15f * debrisChanceScale),
                        new DirectionalImpact(debrisRock, chance: 0.5f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.1f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f * debrisChanceScale),
                    ]
                )
            };

            var mudImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFrontDusty),
                        new DirectionalImpact(puffLinger, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.5f * debrisChanceScale),
                        new DirectionalImpact(debrisDust, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert.Concat(debrisMudVert).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up)
                    ]
                )
            };

            var grassImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFrontDusty),
                        new DirectionalImpact(puffLinger, chance: 0.25f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.45f * debrisChanceScale),
                        new DirectionalImpact(debrisDust, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Grass_1"]], chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert.Concat(debrisMudVert).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up),
                    ]
                )
            };

            var softGenericImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(puffLinger, chance: 0.25f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.5f * debrisChanceScale),
                        new DirectionalImpact(debrisDust, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.4f * debrisChanceScale),
                    ]
                )
            };

            var hardGenericImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(puffRing, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(debrisSparksLight, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f * debrisChanceScale)
                    ]
                )
            };

            var woodImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFrontDusty),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(puffLinger, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.5f * debrisChanceScale),
                        new DirectionalImpact(debrisDust, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Wood_1"]], chance: 0.45f * debrisChanceScale),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.15f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f * debrisChanceScale)
                    ]
                )
            };
            var metalImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(flashSparks),
                        new DirectionalImpact(puffLinger, chance: 0.1f * debrisChanceScale),
                        new DirectionalImpact(puffRing, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(debrisSparksMetal, chance: 0.6f * debrisChanceScale),
                        new DirectionalImpact(debrisSparksDrip, chance: 0.3f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f * debrisChanceScale)
                    ]
                )
            };

            var impactSystems = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];

            // Assign impact systems to materials
            impactSystems[(int)MaterialType.Asphalt] = softRockImpact;
            impactSystems[(int)MaterialType.Cardboard] = softGenericImpact;
            impactSystems[(int)MaterialType.Chainfence] = metalImpact;
            impactSystems[(int)MaterialType.Concrete] = hardRockImpact;
            impactSystems[(int)MaterialType.Fabric] = softGenericImpact;
            impactSystems[(int)MaterialType.GarbageMetal] = metalImpact;
            impactSystems[(int)MaterialType.GarbagePaper] = softGenericImpact;
            impactSystems[(int)MaterialType.GenericSoft] = softGenericImpact;
            impactSystems[(int)MaterialType.Glass] = hardGenericImpact;
            impactSystems[(int)MaterialType.GlassShattered] = hardGenericImpact;
            impactSystems[(int)MaterialType.Grate] = metalImpact;
            impactSystems[(int)MaterialType.GrassHigh] = grassImpact;
            impactSystems[(int)MaterialType.GrassLow] = grassImpact;
            impactSystems[(int)MaterialType.Gravel] = softRockImpact;
            impactSystems[(int)MaterialType.MetalThin] = metalImpact;
            impactSystems[(int)MaterialType.MetalThick] = metalImpact;
            impactSystems[(int)MaterialType.Mud] = mudImpact;
            impactSystems[(int)MaterialType.Pebbles] = softRockImpact;
            impactSystems[(int)MaterialType.Plastic] = softGenericImpact;
            impactSystems[(int)MaterialType.Stone] = hardRockImpact;
            impactSystems[(int)MaterialType.Soil] = mudImpact;
            impactSystems[(int)MaterialType.SoilForest] = mudImpact;
            impactSystems[(int)MaterialType.Tile] = softRockImpact;
            impactSystems[(int)MaterialType.WoodThick] = woodImpact;
            impactSystems[(int)MaterialType.WoodThin] = woodImpact;
            impactSystems[(int)MaterialType.Tyre] = softGenericImpact;
            impactSystems[(int)MaterialType.Rubber] = softGenericImpact;
            impactSystems[(int)MaterialType.GenericHard] = hardGenericImpact;
            impactSystems[(int)MaterialType.MetalNoDecal] = metalImpact;
            impactSystems[(int)MaterialType.None] = hardGenericImpact;

            DefineBodyImpactSystems(effectMap, impactSystems, puffFrontDusty.Concat(puffFrontBody).ToArray(), debrisDust, debrisSparksLight,
                debrisChanceScale);

            return impactSystems;
        }

        private static void DefineBodyImpactSystems(Dictionary<string, Effects.Effect> effectMap, List<ImpactSystem>[] impactSystems,
            Effects.Effect[] puffFront, Effects.Effect[] debrisDust, Effects.Effect[] debrisSparksLight,
            float debrisChanceScale)
        {
            List<DirectionalImpact> bodyArmorImpacts =
            [
                new(puffFront, chance: 0.3f),
                new(debrisDust, chance: 0.4f * debrisChanceScale),
                new([effectMap["Debris_Armor_Fabric_1"]], chance: 0.4f * debrisChanceScale)
            ];

            List<ImpactSystem> bodyImpact = null;

            if (Plugin.BloodEnabled.Value)
            {
                Plugin.Log.LogInfo("Defining body impact");
                List<DirectionalImpact> bodyImpacts = [];

                if (Plugin.BloodPuffsEnabled.Value)
                {
                    Effects.Effect[] puffBloodVert =
                    [
                        effectMap["Puff_Blood_1"], effectMap["Puff_Blood_2"],
                        effectMap["Puff_Blood_3"], effectMap["Puff_Blood_4"]
                    ];

                    bodyImpacts.Add(new DirectionalImpact(puffBloodVert, camDir: CamDir.Angled));
                    bodyArmorImpacts.Add(new DirectionalImpact(puffBloodVert, camDir: CamDir.Angled, chance: 0.5f * debrisChanceScale));

                    Effects.Effect[] puffBloodFront = [effectMap["Puff_Blood_Front_1"], effectMap["Puff_Blood_Front_2"]];
                    bodyImpacts.Add(new DirectionalImpact(puffBloodFront));
                    bodyArmorImpacts.Add(new DirectionalImpact(puffBloodFront, chance: 0.5f * debrisChanceScale));
                }

                if (Plugin.BloodSplatterEnabled.Value)
                {
                    // #3 is twice to increase  the chance of splatters
                    Effects.Effect[] squirts =
                    [
                        effectMap["Squirt_Blood_1"], effectMap["Squirt_Blood_2"],
                        effectMap["Squirt_Blood_3"], effectMap["Squirt_Blood_3"]
                    ];

                    foreach (var squirt in squirts)
                    {
                        var squirtParticles = EffectUtils.GetMediatorParticleSystems(squirt.BasicParticleSystemMediator);

                        foreach (var particleSystem in squirtParticles)
                        {
                            particleSystem.gameObject.AddComponent<BloodSquirtCollisionHandler>();
                        }
                    }

                    bodyImpacts.Add(
                        new DirectionalImpact(squirts,
                            chance: 0.5f * debrisChanceScale)
                    );
                    bodyImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Splash_Blood_Front_1"], effectMap["Splash_Blood_Front_2"], effectMap["Splash_Blood_Front_3"]
                        ], chance: 0.5f * debrisChanceScale)
                    );
                }

                if (Plugin.BloodSplatterFineEnabled.Value)
                {
                    bodyImpacts.Add(new DirectionalImpact([effectMap["Fine_Blood_1"], effectMap["Fine_Blood_2"]]));
                    bodyArmorImpacts.Add(new DirectionalImpact([effectMap["Fine_Blood_1"], effectMap["Fine_Blood_2"]],
                        chance: 0.25f * debrisChanceScale));
                }

                foreach (var effect in bodyImpacts.SelectMany(directionalImpact => directionalImpact.Effects))
                {
                    EffectUtils.ScaleEffect(effect, Plugin.BloodEffectSize.Value, 1);
                }

                bodyImpact = [new ImpactSystem(directional: bodyImpacts.ToArray())];
            }

            var bodyArmorImpact = new List<ImpactSystem> { new(directional: bodyArmorImpacts.ToArray()) };

            var helmetImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront, chance: 0.55f),
                        new DirectionalImpact(debrisSparksLight, chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Armor_Metal_1"]], chance: 0.4f * debrisChanceScale)
                    ]
                )
            };

            impactSystems[(int)MaterialType.BodyArmor] = bodyArmorImpact;
            impactSystems[(int)MaterialType.Helmet] = helmetImpact;
            impactSystems[(int)MaterialType.GlassVisor] = helmetImpact;
            impactSystems[(int)MaterialType.Body] = bodyImpact;
        }
    }

    public class BloodSquirtCollisionHandler : MonoBehaviour
    {
        private Effects _effects;
        private ParticleSystem _particleSystem;
        private List<ParticleCollisionEvent> _collisionEvents;

        public void Start()
        {
            _effects = Singleton<Effects>.Instance;
            _particleSystem = GetComponent<ParticleSystem>();
            _collisionEvents = [];
            Plugin.Log.LogInfo("Starting collision handler");
        }

        public void OnParticleCollision(GameObject other)
        {
            if (other == null)
                return;

            var numEvents = _particleSystem.GetCollisionEvents(other, _collisionEvents);

            for (var i = 0; i < numEvents; i++)
            {
                var hitPos = _collisionEvents[i].intersection;
                var hitNormal = _collisionEvents[i].normal;

                _effects.EmitBleeding(hitPos, hitNormal);
            }
        }
    }
}