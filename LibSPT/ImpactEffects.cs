using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EFT;
using EFT.Ballistics;
using EFT.Particles;
using JetBrains.Annotations;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;
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
    [Flags]
    internal enum CamDir
    {
        None = 0,
        Front = 1 << 0,
        Angled = 1 << 1,
        Left = 1 << 6,
        Right = 1 << 7,
        All = ~0
    }

    [Flags]
    internal enum WorldDir
    {
        None = 0,
        Horizontal = 1 << 1,
        Vertical = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
        All = ~0
    }

    internal static class EffectUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Emit(Effects effects, EmissionContext context, Effects.Effect effect)
        {
            effects.AddEffectEmit(
                effect, context.Position, context.Normal, context.Collider, false, context.Volume,
                context.IsKnife, true, false, context.Pov
            );
        }

        public static void ScaleEffect(Effects.Effect effect, float sizeScaling, float emissionScaling)
        {
            var mediator = effect.BasicParticleSystemMediator;
            
            var particleSystemsField =
                typeof(BasicParticleSystemMediator).GetField("_particleSystems", BindingFlags.NonPublic | BindingFlags.Instance);
            var particleSystems = (ParticleSystem[])particleSystemsField?.GetValue(mediator);

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

        private static short CalcBurstCount(short count, float scaling)
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

    internal struct EmissionContext(
        MaterialType material,
        BallisticCollider collider,
        Vector3 position,
        Vector3 normal,
        float volume,
        bool isKnife,
        EPointOfView pov
    )
    {
        public readonly MaterialType Material = material;
        public readonly BallisticCollider Collider = collider;
        public readonly Vector3 Position = position;
        public readonly Vector3 Normal = normal;
        public readonly float Volume = volume;
        public readonly bool IsKnife = isKnife;
        public readonly EPointOfView Pov = pov;
    }

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
        public void Emit(Effects effects, EmissionContext context, CamDir camDir, WorldDir worldDir)
        {
            Effects.Effect genericEffect = null;

            if (generic != null && camDir.HasFlag(CamDir.Angled))
            {
                genericEffect = generic[Random.Range(0, generic.Length)];

                if (Random.Range(0f, 1f) < forceGeneric)
                {
                    EffectUtils.Emit(effects, context, genericEffect);
                    return;
                }
            }

            var hasEmitted = false;

            foreach (var impact in directional)
            {
                if (!camDir.HasFlag(impact.CamDir) || !worldDir.HasFlag(impact.WorldDir)) continue;
                if (!(Random.Range(0f, 1f) < impact.Chance)) continue;

                var effect = impact.Effects[Random.Range(0, impact.Effects.Length)];

                EffectUtils.Emit(effects, context, effect);

                hasEmitted = true;
            }

            if (hasEmitted || genericEffect == null) return;

            EffectUtils.Emit(effects, context, genericEffect);
        }
    }

    internal class BattleAmbience
    {
        
    }

    internal class ImpactEffectsController
    {
        public static readonly ImpactEffectsController Instance = new();

        private List<ImpactSystem>[] _smallCaliberImpacts = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];
        private List<ImpactSystem>[] _midCaliberImpacts = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];
        private List<ImpactSystem>[] _chonkCaliberImpacts = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];

        [CanBeNull] public EftBulletClass BulletInfo = null;
        [CanBeNull] public ShotInfoClass PlayerHitInfo = null;
        
        public void Emit(Effects effects, EmissionContext context)
        {
            var camera = CameraClass.Instance.Camera;
            var worldAngle = Vector3.Angle(Vector3.down, context.Normal);
            var camAngle = Vector3.Angle(camera.transform.forward, context.Normal);
            var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, context.Normal, Vector3.up);

            var camOrientation = CamDir.None;
            var worldOrientation = WorldDir.None;

            if (camAngle > 107.5)
            {
                camOrientation |= CamDir.Front;
            }

            if (camAngle < 160)
            {
                camOrientation |= CamDir.Angled;
            }

            switch (camAngleSigned)
            {
                case > 0:
                    camOrientation |= CamDir.Right;
                    break;
                case < 0:
                    camOrientation |= CamDir.Left;
                    break;
            }

            if (worldAngle > 45 & worldAngle < 135)
            {
                worldOrientation |= WorldDir.Horizontal;
            }
            else
            {
                worldOrientation |= WorldDir.Vertical;

                if (worldAngle >= 135)
                {
                    worldOrientation |= WorldDir.Up;
                }
                else
                {
                    worldOrientation |= WorldDir.Down;
                }
            }

            var impactChoice = _midCaliberImpacts;

            if (BulletInfo != null)
            {
                // KE = 1/2 * m * v^2, but EFT bullet weight is in g instead of kg so we need to divide by 1000 as well
                var kineticEnergy = BulletInfo.BulletMassGram * Mathf.Pow(BulletInfo.Speed, 2) / 2000;
                
                if (kineticEnergy <= Plugin.SmallEffectEnergy.Value)
                {
                    impactChoice = _smallCaliberImpacts;
                }
                else if (kineticEnergy >= Plugin.ChonkEffectEnergy.Value)
                {
                    impactChoice = _chonkCaliberImpacts;
                }
            }

            var currentSystems = impactChoice[(int)context.Material];

            if (currentSystems == null)
                return;

            foreach (var impactSystem in currentSystems)
            {
                impactSystem.Emit(effects, context, camOrientation, worldOrientation);
            }
        }

        public void Setup(Effects cannedEffects)
        {
            Plugin.Log.LogInfo("Loading Impacts Prefab");
            var impactsPrefab = AssetRegistry.ImpactsBundle.LoadAsset<GameObject>("HFX Impacts");

            _smallCaliberImpacts = BuildCoreImpactSystems(cannedEffects, impactsPrefab, Plugin.SmallEffectSize.Value);
            _midCaliberImpacts = BuildCoreImpactSystems(cannedEffects, impactsPrefab, Plugin.MediumEffectSize.Value);
            _chonkCaliberImpacts = BuildCoreImpactSystems(cannedEffects, impactsPrefab, Plugin.ChonkEffectSize.Value);
        }

        private static List<ImpactSystem>[] BuildCoreImpactSystems(Effects cannedEffects, GameObject impactsPrefab, float scaling)
        {
            var effectMap = LoadEffects(cannedEffects, impactsPrefab);

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
            Plugin.Log.LogInfo("Building generic puffs");
            var puffGeneric = new[]
            {
                effectMap["Puff_Generic_1"], effectMap["Puff_Generic_2"], effectMap["Puff_Generic_2"], effectMap["Puff_Generic_2"],
                effectMap["Puff_Generic_2"], effectMap["Puff_Generic_3"], effectMap["Puff_Generic_4"]
            };

            Plugin.Log.LogInfo("Building dusty puffs");
            var puffGenericDusty = new[]
            {
                effectMap["Puff_Dusty_1"], effectMap["Puff_Dusty_2"], effectMap["Puff_Dusty_3"], effectMap["Puff_Dusty_4"],
                effectMap["Puff_Dusty_1"], effectMap["Puff_Dusty_2"], effectMap["Puff_Dusty_3"], effectMap["Puff_Dusty_4"],
                effectMap["Puff_Dusty_Wide_1"], effectMap["Puff_Dusty_Wide_2"], effectMap["Puff_Dusty_Wide_3"],
                effectMap["Puff_Dusty_Wide_4"], effectMap["Puff_Dusty_Wide_5"], effectMap["Puff_Dusty_Wide_6"]
            };

            Plugin.Log.LogInfo("Building frontal puffs");
            var puffFront = new[]
            {
                effectMap["Puff_Front_1"], effectMap["Puff_Front_2"], effectMap["Puff_Front_3"],
                effectMap["Puff_Front_4"], effectMap["Puff_Front_5"]
            };

            Plugin.Log.LogInfo("Building flash sparks");
            var flashSparks = new[]
            {
                effectMap["Flash_Sparks_1"], effectMap["Flash_Sparks_1"], effectMap["Flash_Sparks_1"], effectMap["Flash_Sparks_2"],
                effectMap["Flash_Sparks_3"],
            };
            
            Plugin.Log.LogInfo("Building horizontal puffs");
            var puffGenericHorRight = new[]
            {
                effectMap["Puff_Generic_Hor_Right_1"], effectMap["Puff_Generic_Hor_Right_2"], effectMap["Puff_Generic_Hor_Right_3"],
                effectMap["Puff_Generic_Hor_Right_4"], effectMap["Puff_Generic_Hor_Right_5"]
            };

            var puffGenericHorLeft = new[]
            {
                effectMap["Puff_Generic_Hor_Left_1"], effectMap["Puff_Generic_Hor_Left_2"], effectMap["Puff_Generic_Hor_Left_3"],
                effectMap["Puff_Generic_Hor_Left_4"], effectMap["Puff_Generic_Hor_Left_5"]
            };

            Plugin.Log.LogInfo("Building horizontal dirt splashes");
            var splashDirtHorRight = new[]
            {
                effectMap["Splash_Dirt_Hor_Right_1"], effectMap["Splash_Dirt_Hor_Right_2"], effectMap["Splash_Dirt_Hor_Right_3"],
                effectMap["Splash_Dirt_Hor_Right_4"], effectMap["Splash_Dirt_Hor_Right_5"], effectMap["Splash_Dirt_Hor_Right_6"],
                effectMap["Splash_Dirt_Hor_Right_7"], effectMap["Splash_Dirt_Hor_Right_8"], effectMap["Splash_Dirt_Hor_Right_9"],
                effectMap["Splash_Dirt_Hor_Right_10"]
            };

            var splashDirtHorLeft = new[]
            {
                effectMap["Splash_Dirt_Hor_Left_1"], effectMap["Splash_Dirt_Hor_Left_2"], effectMap["Splash_Dirt_Hor_Left_3"],
                effectMap["Splash_Dirt_Hor_Left_4"], effectMap["Splash_Dirt_Hor_Left_5"], effectMap["Splash_Dirt_Hor_Left_6"],
                effectMap["Splash_Dirt_Hor_Left_7"], effectMap["Splash_Dirt_Hor_Left_8"], effectMap["Splash_Dirt_Hor_Left_9"],
                effectMap["Splash_Dirt_Hor_Left_10"]
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

            Plugin.Log.LogInfo("Building fine dust");
            var debrisDust = new[]
            {
                effectMap["Fine_Dust_1"]
            };

            Plugin.Log.LogInfo("Building fine spark");
            var debrisSparksLight = new[]
            {
                effectMap["Fine_Sparks_Light_1"], effectMap["Fine_Dust_1"], effectMap["Fine_Dust_1"]
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
                    generic: puffGenericDusty.Concat(puffGeneric).ToArray(),
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDust, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.3f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(splashDirtHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact(splashDirtHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.4f * debrisChanceScale),
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
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisSparksLight, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.3f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.3f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
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
                    generic: puffGenericDusty,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDust, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert.Concat(debrisMudVert).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(splashDirtHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.4f),
                        new DirectionalImpact(splashDirtHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.4f),
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
                    generic: puffGenericDusty,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDust, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Grass_1"]], chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact(debrisDirtVert.Concat(debrisMudVert).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(splashDirtHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.4f),
                        new DirectionalImpact(splashDirtHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.4f),
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
                        new DirectionalImpact(debrisSparksLight, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.3f * debrisChanceScale)
                    ]
                )
            };

            var woodImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(debrisDust, chance: 0.75f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Wood_1"]], chance: 0.45f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.5f * debrisChanceScale)
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
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(debrisSparksMetal, chance: 1f * debrisChanceScale),
                        new DirectionalImpact(debrisSparksDrip, chance: 0.3f * debrisChanceScale),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.4f * debrisChanceScale)
                    ]
                )
            };

            var bodyArmorImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront, chance: 0.5f),
                        new DirectionalImpact(debrisDust, chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Armor_Fabric_1"]], chance: 0.4f * debrisChanceScale)
                    ]
                )
            };

            var helmetImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront, chance: 0.5f),
                        new DirectionalImpact(debrisSparksLight, chance: 0.4f * debrisChanceScale),
                        new DirectionalImpact([effectMap["Debris_Armor_Metal_1"]], chance: 0.4f * debrisChanceScale)
                    ]
                )
            };

            List<ImpactSystem> bodyImpact = null;

            if (Plugin.BloodEnabled.Value)
            {
                Plugin.Log.LogInfo("Defining body impact");
                List<DirectionalImpact> directionalImpacts = [];

                if (Plugin.BloodPuffsEnabled.Value)
                {
                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Puff_Blood_1"], effectMap["Puff_Blood_2"],
                            effectMap["Puff_Blood_3"], effectMap["Puff_Blood_4"]
                        ], camDir: CamDir.Angled)
                    );

                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Puff_Blood_Front_1"], effectMap["Puff_Blood_Front_2"], effectMap["Puff_Blood_Front_3"],
                            effectMap["Puff_Blood_Front_4"], effectMap["Puff_Blood_Front_5"]
                        ])
                    );
                }

                if (Plugin.BloodSplatterEnabled.Value)
                {
                    directionalImpacts.Add(
                        new DirectionalImpact([effectMap["Squirt_Blood_1"], effectMap["Squirt_Blood_2"]],
                            chance: 0.5f * debrisChanceScale)
                    );
                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Splash_Blood_Front_1"], effectMap["Splash_Blood_Front_2"], effectMap["Splash_Blood_Front_3"]
                        ], chance: 0.5f * debrisChanceScale)
                    );
                }

                if (Plugin.BloodSplatterFineEnabled.Value)
                {
                    directionalImpacts.Add(new DirectionalImpact([effectMap["Fine_Blood_1"], effectMap["Fine_Blood_2"]]));
                }

                foreach (var effect in directionalImpacts.SelectMany(directionalImpact => directionalImpact.Effects))
                {
                    EffectUtils.ScaleEffect(effect, Plugin.BloodEffectSize.Value, 1);
                }

                bodyImpact = [new ImpactSystem(directional: directionalImpacts.ToArray())];
            }

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
            impactSystems[(int)MaterialType.BodyArmor] = bodyArmorImpact;
            impactSystems[(int)MaterialType.Helmet] = helmetImpact;
            impactSystems[(int)MaterialType.GlassVisor] = helmetImpact;
            impactSystems[(int)MaterialType.Body] = bodyImpact;

            return impactSystems;
        }

        private static Dictionary<string, Effects.Effect> LoadEffects(Effects cannedEffects, GameObject impactsPrefab)
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
    }
}