using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.Ballistics;
using EFT.UI;
using HollywoodFX.Particles;
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
    internal readonly struct DirectionalImpact(
        EffectBundle effect,
        float chance = 1f,
        bool chanceEnergyScale = false,
        CamDir camDir = CamDir.None,
        WorldDir worldDir = WorldDir.None
    )
    {
        public readonly EffectBundle Effect = effect;
        public readonly WorldDir WorldDir = worldDir;
        public readonly CamDir CamDir = camDir;
        public readonly float Chance = chance;
        public readonly bool ChanceEnergyScale = chanceEnergyScale;
    }

    internal class ImpactSystem(
        DirectionalImpact[] directional,
        [CanBeNull] EffectBundle generic = null,
        float forceGeneric = 0f
    )
    {
        public void Emit(ImpactContext context, float chanceScaling, float sizeScaling)
        {
            EffectBundle genericImpact = null;
            
            if (generic != null && context.CamDir.HasFlag(CamDir.Angled))
            {
                genericImpact = generic;
                
                if (Random.Range(0f, 1f) < forceGeneric)
                {
                    genericImpact.EmitRandom(context.Position, context.RandNormal, sizeScaling);
                    return;
                }
            }

            var hasEmitted = false;

            foreach (var impact in directional)
            {
                if (!context.CamDir.HasFlag(impact.CamDir) || !context.WorldDir.HasFlag(impact.WorldDir)) continue;

                var impactChance = impact.ChanceEnergyScale ? impact.Chance * chanceScaling : impact.Chance;
                if (!(Random.Range(0f, 1f) < impactChance)) continue;

                impact.Effect.EmitRandom(context.Position, context.RandNormal, sizeScaling);
                hasEmitted = true;
            }

            if (hasEmitted || genericImpact == null) return;

            genericImpact.EmitRandom(context.Position, context.RandNormal, sizeScaling);
        }
    }

    internal class ImpactEffects(Effects eftEffects, GameObject prefab)
    {
        private readonly List<ImpactSystem>[] _impacts = BuildCoreImpactSystems(eftEffects, prefab);

        public void Emit(ImpactContext context)
        {
            var currentSystems = _impacts[(int)context.Material];

            if (currentSystems == null)
                return;

            var scale = Mathf.Clamp(Mathf.Sqrt(context.KineticEnergy / 1500f), 0.5f, 1.25f) * Plugin.EffectSize.Value;

            // Chance scaling has linear scaling below 1, quadratic above. This ensures visible difference for large calibers without suppressing
            // things too much for smaller ones.
            var chanceScale = scale < 1f ? scale : Mathf.Pow(scale, 2f);

            foreach (var impactSystem in currentSystems)
            {
                impactSystem.Emit(context, chanceScale, scale);
            }
        }

        private static List<ImpactSystem>[] BuildCoreImpactSystems(Effects eftEffects, GameObject prefab)
        {
            Plugin.Log.LogInfo("Instantiating Impact Effects Prefab");
            var rootInstance = Object.Instantiate(prefab);

            var effectMap = new Dictionary<string, EffectBundle>();

            foreach (var group in rootInstance.transform.GetChildren())
            {
                var groupName = group.name;
                var effects = new List<ParticleSystem>();

                foreach (var child in group.GetChildren())
                {
                    if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;

                    child.parent = eftEffects.transform;
                    Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, false);
                    effects.Add(particleSystem);
                }

                effectMap[groupName] = new EffectBundle(effects.ToArray());
                Plugin.Log.LogInfo($"Added impact effect `{groupName}` with {effects.Count} particle systems");
            }

            return DefineImpactSystems(effectMap);
        }

        private static List<ImpactSystem>[] DefineImpactSystems(Dictionary<string, EffectBundle> effectMap)
        {
            Plugin.Log.LogInfo("Constructing impact systems");

            // Define major building blocks for systems
            Plugin.Log.LogInfo("Building frontal puffs");
            var puffFront = effectMap["Puff_Front"];
            var puffFrontBody = effectMap["Puff_Body_Front"];
            var puffFrontDusty = effectMap["Puff_Dusty_Front"];
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
            var puffRing = effectMap["Puff_Smoke_Ring"];

            Plugin.Log.LogInfo("Building dirt debris");
            var debrisDirtVert = effectMap["Debris_Dirt_Vert"];

            Plugin.Log.LogInfo("Building mud debris");
            var debrisMudVert = EffectBundle.Merge(debrisDirtVert, effectMap["Debris_Mud_Vert"]);

            Plugin.Log.LogInfo("Building rock debris");
            var debrisRock = effectMap["Debris_Rock"];

            Plugin.Log.LogInfo("Building fine dust");
            var debrisDust = effectMap["Fine_Dust"];

            Plugin.Log.LogInfo("Building fine spark");
            var debrisSparksLight = EffectBundle.Merge(
                effectMap["Fine_Sparks_Light"], effectMap["Fine_Dust"], effectMap["Fine_Dust"],
                effectMap["Fine_Dust"], effectMap["Fine_Dust"]
            );

            var debrisSparksMetal = EffectBundle.Merge(
                effectMap["Fine_Sparks_Metal"], effectMap["Fine_Sparks_Metal"], effectMap["Fine_Sparks_Light"]
            );

            Plugin.Log.LogInfo("Building generic debris");
            var debrisGeneric = effectMap["Debris_Generic"];

            Plugin.Log.LogInfo("Building misc debris");
            var bulletHoleSmoke = effectMap["Impact_Smoke"];

            var debrisSparksDrip = effectMap["Drip_Sparks"];

            var fallingDust = effectMap["Falling_Dust"];

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
                        new DirectionalImpact(puffLinger, chance: 0.25f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.5f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDust, chance: 1f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisRock, chance: 0.33f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.2f, chanceEnergyScale: true),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f, chanceEnergyScale: true),
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
                        new DirectionalImpact(puffLinger, chance: 0.25f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisSparksLight, chance: 1f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisGeneric, chance: 0.15f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisRock, chance: 0.5f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDirtVert, worldDir: WorldDir.Vertical | WorldDir.Up),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.1f, chanceEnergyScale: true),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f, chanceEnergyScale: true),
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
                        new DirectionalImpact(puffLinger, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.5f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDust, chance: 1f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisGeneric, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisMudVert, worldDir: WorldDir.Vertical | WorldDir.Up)
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
                        new DirectionalImpact(puffLinger, chance: 0.25f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.45f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDust, chance: 0.75f, chanceEnergyScale: true),
                        new DirectionalImpact(effectMap["Debris_Grass"], chance: 0.4f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisMudVert, worldDir: WorldDir.Vertical | WorldDir.Up),
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
                        new DirectionalImpact(puffLinger, chance: 0.25f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.5f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDust, chance: 0.75f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisGeneric, chance: 0.4f, chanceEnergyScale: true),
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
                        new DirectionalImpact(puffRing, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisSparksLight, chance: 0.75f, chanceEnergyScale: true),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f, chanceEnergyScale: true)
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
                        new DirectionalImpact(puffLinger, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.5f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisDust, chance: 0.75f, chanceEnergyScale: true),
                        new DirectionalImpact(effectMap["Debris_Wood"], chance: 0.45f, chanceEnergyScale: true),
                        new DirectionalImpact(fallingDust, worldDir: WorldDir.Vertical | WorldDir.Down, chance: 0.15f, chanceEnergyScale: true),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f, chanceEnergyScale: true)
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
                        new DirectionalImpact(puffLinger, chance: 0.1f, chanceEnergyScale: true),
                        new DirectionalImpact(puffRing, chance: 0.35f, chanceEnergyScale: true),
                        new DirectionalImpact(puffGeneric, camDir: CamDir.Angled),
                        new DirectionalImpact(debrisSparksMetal, chance: 0.6f, chanceEnergyScale: true),
                        new DirectionalImpact(debrisSparksDrip, chance: 0.3f, chanceEnergyScale: true),
                        new DirectionalImpact(bulletHoleSmoke, chance: 0.05f, chanceEnergyScale: true)
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

            DefineBodyImpactSystems(
                effectMap, impactSystems, EffectBundle.Merge(puffFrontDusty, puffFrontBody), debrisDust, debrisSparksLight
            );

            return impactSystems;
        }

        private static void DefineBodyImpactSystems(Dictionary<string, EffectBundle> effectMap, List<ImpactSystem>[] impactSystems,
            EffectBundle puffFront, EffectBundle debrisDust, EffectBundle debrisSparksLight)
        {
            List<DirectionalImpact> bodyArmorImpacts =
            [
                new(puffFront, chance: 0.3f),
                new(debrisDust, chance: 0.4f, chanceEnergyScale: true),
                new(effectMap["Debris_Armor_Fabric"], chance: 0.4f, chanceEnergyScale: true)
            ];

            List<ImpactSystem> bodyImpact = null;

            if (Plugin.BloodEnabled.Value)
            {
                Plugin.Log.LogInfo("Defining body impact");
                List<DirectionalImpact> bodyImpacts = [];

                if (Plugin.BloodPuffsEnabled.Value)
                {
                    var puffBloodVert = effectMap["Puff_Blood"];
                    bodyImpacts.Add(new DirectionalImpact(puffBloodVert, camDir: CamDir.Angled));
                    bodyArmorImpacts.Add(new DirectionalImpact(puffBloodVert, camDir: CamDir.Angled, chance: 0.5f, chanceEnergyScale: true));

                    var puffBloodFront = effectMap["Puff_Blood_Front"];
                    bodyImpacts.Add(new DirectionalImpact(puffBloodFront));
                    bodyArmorImpacts.Add(new DirectionalImpact(puffBloodFront, chance: 0.5f, chanceEnergyScale: true));
                }

                if (Plugin.BloodSplatterEnabled.Value)
                {
                    var squirts = effectMap["Squirt_Blood"];

                    foreach (var squirt in squirts.ParticleSystems)
                    {
                        squirt.gameObject.AddComponent<BloodSquirtCollisionHandler>();
                    }

                    bodyImpacts.Add(new DirectionalImpact(squirts, chance: 0.5f, chanceEnergyScale: true));
                    bodyImpacts.Add(new DirectionalImpact(effectMap["Splash_Blood_Front"], chance: 0.5f, chanceEnergyScale: true));
                }

                if (Plugin.BloodSplatterFineEnabled.Value)
                {
                    var fineBlood = effectMap["Fine_Blood"];
                    bodyImpacts.Add(new DirectionalImpact(fineBlood));
                    bodyArmorImpacts.Add(new DirectionalImpact(fineBlood, chance: 0.25f, chanceEnergyScale: true));
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
                        new DirectionalImpact(debrisSparksLight, chance: 0.4f, chanceEnergyScale: true),
                        new DirectionalImpact(effectMap["Debris_Armor_Metal"], chance: 0.4f, chanceEnergyScale: true)
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