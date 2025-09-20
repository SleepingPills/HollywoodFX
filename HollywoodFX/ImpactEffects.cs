using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.Ballistics;
using HollywoodFX.Concussion;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace System.Runtime.CompilerServices
{
    // ReSharper disable once UnusedType.Global
    internal static class IsExternalInit;
}

namespace HollywoodFX
{
    /// <summary>
    /// Verily, this is a spaghetti monster class. Refactor at some point once sanity levels have been restored.
    /// </summary>
    internal class ImpactEffects
    {
        private readonly List<EffectSystem>[] _mainImpacts;
        private readonly TracerImpactEffects _tracerImpacts;

        private readonly EffectSystem _extraFlashes;
        private readonly float[] _extraFlashChances;

        public ImpactEffects(Effects eftEffects, GameObject mainPrefab, GameObject tracerPrefab)
        {
            var mainEffects = EffectBundle.LoadPrefab(eftEffects, mainPrefab, true);
            var tracerEffects = EffectBundle.LoadPrefab(eftEffects, tracerPrefab, false);

            _mainImpacts = DefineMainEffects(mainEffects);
            _tracerImpacts = new TracerImpactEffects(eftEffects, mainEffects, tracerEffects);

            _extraFlashes = DefineExtraFlashes(tracerEffects);
            _extraFlashChances = DefineExtraFlashChances();
        }

        public void Emit(ImpactKinetics kinetics)
        {
            var currentSystems = _mainImpacts[(int)kinetics.Material];

            if (currentSystems == null)
                return;

            for (var i = 0; i < currentSystems.Count; i++)
            {
                var impactSystem = currentSystems[i];
                impactSystem.Emit(kinetics, Plugin.EffectSize.Value);
            }

            if (Plugin.SuppressionEnabled.Value)
            {
                var duration = 1f * Plugin.SuppressionDuration.Value;
                var distanceNorm = 3f * kinetics.Bullet.SizeScale * Plugin.SuppressionRange.Value;
                Singleton<ConcussionController>.Instance.Apply(kinetics.DistanceToImpact, duration, distanceNorm, 2f * duration);
            }

            if (Plugin.TracerImpactsEnabled.Value && kinetics.Bullet.Info.Ammo is AmmoItemClass { Tracer: true } ammo)
                _tracerImpacts.Emit(kinetics, ammo);
            else
            {
                var chance = _extraFlashChances[(int)kinetics.Material];

                if (!(Random.Range(0f, 1f) < chance * kinetics.Bullet.ChanceScale))
                    return;

                _extraFlashes.Emit(kinetics, Plugin.EffectSize.Value);
            }
        }

        private static EffectSystem DefineExtraFlashes(Dictionary<string, EffectBundle> tracerEffects)
        {
            Plugin.Log.LogInfo("Defining extra flashes");
            var sparksGeneric = tracerEffects["Sparks_Generic"];
            var sparksHorRight = tracerEffects["Sparks_Hor_Right"];
            var sparksHorLeft = tracerEffects["Sparks_Hor_Left"];

            var sparksGroundComb = EffectBundle.Merge(tracerEffects["Sparks_Generic"], tracerEffects["Sparks_Wide"]);

            return new EffectSystem(
                directional:
                [
                    new DirectionalEffect(sparksHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                    new DirectionalEffect(sparksHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    new DirectionalEffect(sparksGroundComb, worldDir: WorldDir.Vertical | WorldDir.Up),
                ],
                generic: sparksGeneric,
                forceGeneric: 0.33f,
                useOffsetNormals: true
            );
        }

        private static float[] DefineExtraFlashChances()
        {
            var chances = new float[Enum.GetNames(typeof(MaterialType)).Length];

            chances[(int)MaterialType.Chainfence] = 0.15f;
            chances[(int)MaterialType.Concrete] = 0.2f;
            chances[(int)MaterialType.GarbageMetal] = 0.25f;
            chances[(int)MaterialType.Glass] = 0.1f;
            chances[(int)MaterialType.GlassShattered] = 0.1f;
            chances[(int)MaterialType.Grate] = 0.2f;
            chances[(int)MaterialType.Gravel] = 0.1f;
            chances[(int)MaterialType.MetalThin] = 0.2f;
            chances[(int)MaterialType.MetalThick] = 0.25f;
            chances[(int)MaterialType.Pebbles] = 0.1f;
            chances[(int)MaterialType.Stone] = 0.15f;
            chances[(int)MaterialType.Tile] = 0.15f;
            chances[(int)MaterialType.GenericHard] = 0.15f;
            chances[(int)MaterialType.MetalNoDecal] = 0.25f;

            return chances;
        }

        private static List<EffectSystem>[] DefineMainEffects(Dictionary<string, EffectBundle> effectMap)
        {
            Plugin.Log.LogInfo("Constructing impact systems");

            // Define major building blocks for systems
            Plugin.Log.LogInfo("Building frontal puffs");
            var puffFront = effectMap["Puff_Front"];
            var puffFrontDusty = effectMap["Puff_Dusty_Front"];
            var puffFrontBody = EffectBundle.Merge(effectMap["Puff_Body_Front"], puffFrontDusty);
            var puffFrontRock = EffectBundle.Merge(puffFront, puffFrontDusty);

            Plugin.Log.LogInfo("Building flash sparks");
            var flashSparks = effectMap["Flash_Sparks"];

            Plugin.Log.LogInfo("Building generic puffs");
            var puffGeneric = effectMap["Puff"];

            Plugin.Log.LogInfo("Building horizontal puffs");
            var puffGenericHorRight = effectMap["Puff_Dusty_Hor_Right"];
            var puffGenericHorLeft = effectMap["Puff_Dusty_Hor_Left"];

            Plugin.Log.LogInfo("Building lingering puffs");
            var puffLinger = effectMap["Puff_Smoke_Linger"];

            Plugin.Log.LogInfo("Building puff rings");
            var puffRing = EffectBundle.Merge(
                effectMap["Puff_Smoke_Ring"], effectMap["Puff_Smoke_Ring"],
                effectMap["Puff_Smoke_Ring"], effectMap["Puff_Smoke_Ring"],
                effectMap["Puff_Smoke_Billow"]
                );
            var puffBody = effectMap["Puff_Smoke_Body"];

            Plugin.Log.LogInfo("Building dirt debris");
            var debrisDirtVert = effectMap["Debris_Dirt_Vert"];

            Plugin.Log.LogInfo("Building mud debris");
            var debrisMudVert = EffectBundle.Merge(debrisDirtVert, effectMap["Debris_Mud_Vert"]);

            Plugin.Log.LogInfo("Building rock debris");
            var debrisRock = effectMap["Debris_Rock"];

            Plugin.Log.LogInfo("Building generic debris");
            var debrisGeneric = effectMap["Debris_Generic"];

            Plugin.Log.LogInfo("Building dust spray");
            var sprayDust = effectMap["Spray_Dust"];

            Plugin.Log.LogInfo("Building spark spray");
            var spraySparksLight = EffectBundle.Merge(
                effectMap["Spray_Sparks_Light"], effectMap["Spray_Dust"], effectMap["Spray_Dust"],
                effectMap["Spray_Dust"], effectMap["Spray_Dust"]
            );

            var spraySparksMetal = EffectBundle.Merge(
                effectMap["Spray_Sparks_Metal"], effectMap["Spray_Sparks_Metal"], effectMap["Spray_Sparks_Light"]
            );

            Plugin.Log.LogInfo("Building misc stuff");
            var bulletHoleSmoke = effectMap["Impact_Smoke"];

            var fallingDust = effectMap["Falling_Dust"];

            Plugin.Log.LogInfo("Defining material specific impacts");
            var softRockImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontRock),
                        new DirectionalEffect(puffLinger, chance: 0.3f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(sprayDust, chance: 1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisGeneric, chance: 0.2f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisRock, chance: 0.25f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalEffect(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.2f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(bulletHoleSmoke, chance: 0.05f, isChanceScaledByKinetics: true),
                    ]
                )
            };

            var hardRockImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.75f,
                    useOffsetNormals: true
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontRock),
                        new DirectionalEffect(puffLinger, chance: 0.3f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(spraySparksLight, chance: 1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisGeneric, chance: 0.1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisRock, chance: 0.35f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalEffect(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(bulletHoleSmoke, chance: 0.05f, isChanceScaledByKinetics: true),
                    ]
                )
            };

            var mudImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontDusty),
                        new DirectionalEffect(puffLinger, chance: 0.4f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(sprayDust, chance: 1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisGeneric, chance: 0.25f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisMudVert, worldDir: WorldDir.Vertical | WorldDir.Up)
                    ]
                )
            };

            var grassImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                // Various debris and splashes
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontDusty),
                        new DirectionalEffect(puffLinger, chance: 0.5f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 0.75f),
                        new DirectionalEffect(sprayDust, chance: 0.75f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(effectMap["Debris_Grass"], chance: 0.4f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisMudVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                    ]
                )
            };

            var softGenericImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFront),
                        new DirectionalEffect(puffLinger, chance: 0.25f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(sprayDust, chance: 0.75f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(debrisGeneric, chance: 0.3f, isChanceScaledByKinetics: true),
                    ]
                )
            };

            var hardGenericImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFront),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(spraySparksLight, chance: 0.75f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(bulletHoleSmoke, chance: 0.05f, isChanceScaledByKinetics: true)
                    ]
                )
            };

            var woodImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional:
                    [
                        new DirectionalEffect(puffGenericHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(puffGenericHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                    ],
                    generic: puffGeneric,
                    forceGeneric: 0.33f,
                    useOffsetNormals: true
                ),
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontDusty),
                        new DirectionalEffect(puffLinger, chance: 0.3f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 1f),
                        new DirectionalEffect(sprayDust, chance: 0.75f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(effectMap["Debris_Wood"], chance: 0.45f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.15f,
                            isChanceScaledByKinetics: true),
                        new DirectionalEffect(bulletHoleSmoke, chance: 0.05f, isChanceScaledByKinetics: true)
                    ]
                )
            };
            var metalImpact = new List<EffectSystem>
            {
                // Main puff
                new(
                    directional: [],
                    generic: puffGeneric,
                    forceGeneric: 1.0f,
                    useOffsetNormals: true
                ),
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFront),
                        new DirectionalEffect(flashSparks),
                        new DirectionalEffect(puffLinger, chance: 0.15f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(puffRing, chance: 0.65f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(spraySparksMetal, chance: 0.8f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(bulletHoleSmoke, chance: 0.05f, isChanceScaledByKinetics: true)
                    ]
                )
            };

            var bodyArmorImpact = new List<EffectSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontBody, chance: 0.1f),
                        new DirectionalEffect(puffBody, chance: 0.75f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(sprayDust, chance: 0.5f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(EffectBundle.Merge(effectMap["Debris_Armor_Metal"], effectMap["Debris_Armor_Fabric"]),
                            chance: 0.5f, isChanceScaledByKinetics: true)
                    ]
                )
            };

            var helmetImpact = new List<EffectSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontBody, chance: 0.15f),
                        new DirectionalEffect(puffBody, chance: 0.55f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(spraySparksLight, chance: 0.4f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(effectMap["Debris_Armor_Metal"], chance: 0.5f, isChanceScaledByKinetics: true)
                    ]
                )
            };

            var bodyImpact = new List<EffectSystem>
            {
                new(
                    directional:
                    [
                        new DirectionalEffect(puffFrontBody, chance: 0.2f),
                        new DirectionalEffect(puffBody, chance: 0.4f, isChanceScaledByKinetics: true),
                    ]
                )
            };

            var impactSystems = new List<EffectSystem>[Enum.GetNames(typeof(MaterialType)).Length];

            // Assign impact systems to materials
            impactSystems[(int)MaterialType.Asphalt] = softRockImpact;
            impactSystems[(int)MaterialType.Cardboard] = softGenericImpact;
            impactSystems[(int)MaterialType.Chainfence] = metalImpact;
            impactSystems[(int)MaterialType.Concrete] = hardRockImpact;
            impactSystems[(int)MaterialType.None] = hardRockImpact;
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
            impactSystems[(int)MaterialType.BodyArmor] = bodyArmorImpact;
            impactSystems[(int)MaterialType.Helmet] = helmetImpact;
            impactSystems[(int)MaterialType.GlassVisor] = helmetImpact;
            impactSystems[(int)MaterialType.Body] = bodyImpact;

            return impactSystems;
        }
    }
}