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

        public static void ScaleMediator(BasicParticleSystemMediator mediator, float scaling)
        {
            var particleSystemsField = typeof(BasicParticleSystemMediator).GetField("_particleSystems", BindingFlags.NonPublic | BindingFlags.Instance);
            var particleSystems = (ParticleSystem[])particleSystemsField?.GetValue(mediator);
            
            if (particleSystems == null)
                return;

            foreach (var particleSystem in particleSystems)
            {
                particleSystem.transform.localScale *= scaling;
            }
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

    internal class ImpactEffectsController
    {
        public static readonly ImpactEffectsController Instance = new();

        private readonly List<ImpactSystem>[] _coreImpactSystems = new List<ImpactSystem>[Enum.GetNames(typeof(MaterialType)).Length];

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

            var currentSystems = _coreImpactSystems[(int)context.Material];

            if (currentSystems == null)
                return;

            foreach (var impactSystem in currentSystems)
            {
                impactSystem.Emit(effects, context, camOrientation, worldOrientation);
            }
        }

        public void Setup(Dictionary<string, Effects.Effect> effectMap)
        {
            // Clear out any systems remaining from a previous run
            for (var i = 0; i < _coreImpactSystems.Length; i++)
            {
                _coreImpactSystems[i] = null;
            }

            // Define major building blocks for systems
            var puffGeneric = new[]
            {
                effectMap["Generic_Hit_Vert_1"], effectMap["Generic_Hit_Vert_2"], effectMap["Generic_Hit_Vert_2"], effectMap["Generic_Hit_Vert_2"],
                effectMap["Generic_Hit_Vert_2"], effectMap["Generic_Hit_Vert_3"], effectMap["Generic_Hit_Vert_4"]
            };

            var puffGenericDusty = new[]
            {
                effectMap["Dusty_Hit_Vert_1"], effectMap["Dusty_Hit_Vert_2"], effectMap["Dusty_Hit_Vert_3"], effectMap["Dusty_Hit_Vert_4"],
                effectMap["Dusty_Hit_Vert_1"], effectMap["Dusty_Hit_Vert_2"], effectMap["Dusty_Hit_Vert_3"], effectMap["Dusty_Hit_Vert_4"],
                effectMap["Dusty_Hit_Wide_Vert_1"], effectMap["Dusty_Hit_Wide_Vert_2"], effectMap["Dusty_Hit_Wide_Vert_3"],
                effectMap["Dusty_Hit_Wide_Vert_4"], effectMap["Dusty_Hit_Wide_Vert_5"], effectMap["Dusty_Hit_Wide_Vert_6"]
            };

            var puffFront = new[]
            {
                effectMap["Generic_Front_1"], effectMap["Generic_Front_2"], effectMap["Generic_Front_3"],
                effectMap["Generic_Front_4"], effectMap["Generic_Front_5"]
            };

            var sparksFront = new[]
            {
                effectMap["Sparks_Front_1"], effectMap["Sparks_Front_1"], effectMap["Sparks_Front_1"], effectMap["Sparks_Front_2"],
                effectMap["Generic_Front_3"],
            };

            var horRight = new[]
            {
                effectMap["Generic_Hit_Hor_1"], effectMap["Generic_Hit_Hor_2"], effectMap["Generic_Hit_Hor_3"], effectMap["Generic_Hit_Hor_4"],
                effectMap["Generic_Hit_Hor_5"]
            };

            var horLeft = new[]
            {
                effectMap["Generic_Hit_Hor_Left_1"], effectMap["Generic_Hit_Hor_Left_2"], effectMap["Generic_Hit_Hor_Left_3"],
                effectMap["Generic_Hit_Hor_Left_4"], effectMap["Generic_Hit_Hor_Left_5"]
            };

            var horDirtRight = new[]
            {
                effectMap["Dirt_Splash_Hor_1"], effectMap["Dirt_Splash_Hor_2"], effectMap["Dirt_Splash_Hor_3"], effectMap["Dirt_Splash_Hor_4"],
                effectMap["Dirt_Splash_Hor_5"], effectMap["Dirt_Splash_Hor_6"], effectMap["Dirt_Splash_Hor_7"], effectMap["Dirt_Splash_Hor_8"],
                effectMap["Dirt_Splash_Hor_9"], effectMap["Dirt_Splash_Hor_10"]
            };

            var horDirtLeft = new[]
            {
                effectMap["Dirt_Splash_Hor_Left_1"], effectMap["Dirt_Splash_Hor_Left_2"], effectMap["Dirt_Splash_Hor_Left_3"],
                effectMap["Dirt_Splash_Hor_Left_4"], effectMap["Dirt_Splash_Hor_Left_5"], effectMap["Dirt_Splash_Hor_Left_6"],
                effectMap["Dirt_Splash_Hor_Left_7"], effectMap["Dirt_Splash_Hor_Left_8"], effectMap["Dirt_Splash_Hor_Left_9"],
                effectMap["Dirt_Splash_Hor_Left_10"]
            };

            var vUpMud = new[]
            {
                effectMap["Debris_Mud_Vert_1"], effectMap["Debris_Mud_Vert_2"], effectMap["Debris_Mud_Vert_3"]
            };

            var vUpDirt = new[]
            {
                effectMap["Debris_Dirt_Vert_1"], effectMap["Debris_Dirt_Vert_2"], effectMap["Debris_Dirt_Vert_3"]
            };

            var debrisDustSplash = new[]
            {
                effectMap["Debris_Dust_Splash_1"]
            };

            var debrisSparksLight = new[]
            {
                effectMap["Debris_Sparks_Light_1"], effectMap["Debris_Dust_Splash_1"], effectMap["Debris_Dust_Splash_1"]
            };

            var debrisSparksMetal = new[]
            {
                effectMap["Debris_Sparks_Metal_1"], effectMap["Debris_Sparks_Metal_1"], effectMap["Debris_Sparks_Light_1"]
            };

            var debrisGeneric = new[]
            {
                effectMap["Debris_Generic_1"]
            };

            var bulletHoleSmoke = new[]
            {
                effectMap["Impact_Smoke_1"]
            };

            var debrisSparksDrip = new[]
            {
                effectMap["Debris_Sparks_Drip_1"]
            };

            // var debrisSparksBig = new[]
            // {
            //     effectMap["Debris_Sparks_Big_1"]
            // };

            var softRockImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(horRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(horLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGenericDusty.Concat(puffGeneric).ToArray(),
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDustSplash, chance: 0.75f),
                        new DirectionalImpact(debrisGeneric, chance: 0.25f),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.33f),
                        new DirectionalImpact(vUpDirt, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(horDirtRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.33f),
                        new DirectionalImpact(horDirtLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.33f),
                    ]
                )
            };

            var hardRockImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(horRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(horLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.75f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisSparksLight, chance: 0.75f),
                        new DirectionalImpact(debrisGeneric, chance: 0.2f),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.33f),
                        new DirectionalImpact(vUpDirt, worldDir: WorldDir.Vertical | WorldDir.Up),
                    ]
                )
            };

            var mudImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(horRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(horLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGenericDusty,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDustSplash, chance: 0.75f),
                        new DirectionalImpact(debrisGeneric, chance: 0.25f),
                        new DirectionalImpact(vUpDirt.Concat(vUpMud).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(horDirtRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.33f),
                        new DirectionalImpact(horDirtLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.33f),
                    ]
                )
            };

            var grassImpact = new List<ImpactSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalImpact(horRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalImpact(horLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGenericDusty,
                    forceGeneric: 0.33f
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(debrisDustSplash, chance: 0.5f),
                        new DirectionalImpact([effectMap["Debris_Grass_1"]], chance: 0.3f),
                        new DirectionalImpact(vUpDirt.Concat(vUpMud).ToArray(), worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(horDirtRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal, chance: 0.33f),
                        new DirectionalImpact(horDirtLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal, chance: 0.33f),
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
                        new DirectionalImpact(debrisDustSplash, chance: 0.5f),
                        new DirectionalImpact(debrisGeneric, chance: 0.3f),
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
                        new DirectionalImpact(debrisSparksLight, chance: 0.5f),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.33f)
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
                        new DirectionalImpact(debrisDustSplash, chance: 0.5f),
                        new DirectionalImpact([effectMap["Debris_Wood_1"]], chance: 0.35f),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.33f)
                    ]
                )
            };

            var metalImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront),
                        new DirectionalImpact(sparksFront),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(debrisSparksMetal, chance: 0.8f),
                        new DirectionalImpact(debrisSparksDrip, chance: 0.25f),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.33f)
                    ]
                )
            };

            var bodyArmorImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront, chance: 0.5f),
                        new DirectionalImpact(debrisDustSplash, chance: 0.33f),
                        new DirectionalImpact([effectMap["Debris_Armor_Fabric_1"]], chance: 0.33f)
                    ]
                )
            };

            var helmetImpact = new List<ImpactSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalImpact(puffFront, chance: 0.5f),
                        new DirectionalImpact(debrisSparksLight, chance: 0.33f),
                        new DirectionalImpact([effectMap["Debris_Armor_Metal_1"]], chance: 0.33f)
                    ]
                )
            };

            List<ImpactSystem> bodyImpact = null;

            if (Plugin.BloodEnabled.Value)
            {
                List<DirectionalImpact> directionalImpacts = [];

                if (Plugin.BloodPuffsEnabled.Value)
                {
                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Blood_Puff_Vert_1"], effectMap["Blood_Puff_Vert_2"],
                            effectMap["Blood_Puff_Vert_3"], effectMap["Blood_Puff_Vert_4"]
                        ], camDir: CamDir.Angled)
                    );

                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Blood_Puff_Front_1"], effectMap["Blood_Puff_Front_2"], effectMap["Blood_Puff_Front_3"],
                            effectMap["Blood_Puff_Front_4"], effectMap["Blood_Puff_Front_5"]
                        ])
                    );
                }

                if (Plugin.BloodSplatterEnabled.Value)
                {
                    directionalImpacts.Add(
                        new DirectionalImpact([effectMap["Blood_Splash_Spurt_1"], effectMap["Blood_Splash_Spurt_2"]], chance: 0.5f)
                    );
                    directionalImpacts.Add(
                        new DirectionalImpact([
                            effectMap["Blood_Splash_Front_1"], effectMap["Blood_Splash_Front_2"], effectMap["Blood_Splash_Front_3"]
                        ], chance: 1f)
                    );
                }
                
                if (Plugin.BloodSplatterFineEnabled.Value)
                {
                    directionalImpacts.Add(new DirectionalImpact([effectMap["Debris_Blood_Splash_1"], effectMap["Debris_Blood_Splash_2"]]));
                }

                foreach (var directionalImpact in directionalImpacts)
                {
                    foreach (var effect in directionalImpact.Effects)
                    {
                        EffectUtils.ScaleMediator(effect.BasicParticleSystemMediator, Plugin.BloodEffectSize.Value);
                    }
                }

                bodyImpact = [new ImpactSystem(directional: directionalImpacts.ToArray())];
            }

            // Assign impact systems to materials
            _coreImpactSystems[(int)MaterialType.Asphalt] = softRockImpact;
            _coreImpactSystems[(int)MaterialType.Cardboard] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.Chainfence] = metalImpact;
            _coreImpactSystems[(int)MaterialType.Concrete] = hardRockImpact;
            _coreImpactSystems[(int)MaterialType.Fabric] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.GarbageMetal] = metalImpact;
            _coreImpactSystems[(int)MaterialType.GarbagePaper] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.GenericSoft] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.Glass] = hardGenericImpact;
            _coreImpactSystems[(int)MaterialType.GlassShattered] = hardGenericImpact;
            _coreImpactSystems[(int)MaterialType.Grate] = metalImpact;
            _coreImpactSystems[(int)MaterialType.GrassHigh] = grassImpact;
            _coreImpactSystems[(int)MaterialType.GrassLow] = grassImpact;
            _coreImpactSystems[(int)MaterialType.Gravel] = softRockImpact;
            _coreImpactSystems[(int)MaterialType.MetalThin] = metalImpact;
            _coreImpactSystems[(int)MaterialType.MetalThick] = metalImpact;
            _coreImpactSystems[(int)MaterialType.Mud] = mudImpact;
            _coreImpactSystems[(int)MaterialType.Pebbles] = softRockImpact;
            _coreImpactSystems[(int)MaterialType.Plastic] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.Stone] = hardRockImpact;
            _coreImpactSystems[(int)MaterialType.Soil] = mudImpact;
            _coreImpactSystems[(int)MaterialType.SoilForest] = mudImpact;
            _coreImpactSystems[(int)MaterialType.Tile] = softRockImpact;
            _coreImpactSystems[(int)MaterialType.WoodThick] = woodImpact;
            _coreImpactSystems[(int)MaterialType.WoodThin] = woodImpact;
            _coreImpactSystems[(int)MaterialType.Tyre] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.Rubber] = softGenericImpact;
            _coreImpactSystems[(int)MaterialType.GenericHard] = hardGenericImpact;
            _coreImpactSystems[(int)MaterialType.MetalNoDecal] = metalImpact;
            _coreImpactSystems[(int)MaterialType.BodyArmor] = bodyArmorImpact;
            _coreImpactSystems[(int)MaterialType.Helmet] = helmetImpact;
            _coreImpactSystems[(int)MaterialType.GlassVisor] = helmetImpact;
            _coreImpactSystems[(int)MaterialType.Body] = bodyImpact;
        }
    }
}
/*
 * None,
    Asphalt,
    Body,
    Cardboard,
    Chainfence,
    Concrete,
    Fabric,
    GarbageMetal,
    GarbagePaper,
    GenericSoft,
    Glass,
    GlassShattered,
    Grate,
    GrassHigh,
    GrassLow,
    Gravel,
    MetalThin,
    MetalThick,
    Mud,
    Pebbles,
    Plastic,
    Stone,
    Soil,
    SoilForest,
    Tile,
    Water,
    WaterPuddle,
    WoodThin,
    WoodThick,
    Tyre,
    Rubber,
    GenericHard,
    BodyArmor,
    Swamp,
    Helmet,
    GlassVisor,
    HelmetRicochet,
    MetalNoDecal,
*/