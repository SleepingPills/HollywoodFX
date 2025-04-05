using System;
using System.Collections.Generic;
using Comfort.Common;
using DeferredDecals;
using EFT.Ballistics;
using HarmonyLib;
using HollywoodFX.Decal;
using HollywoodFX.Particles;
using JsonType;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX;

internal class TracerImpact(EffectSystem[] systems, float chance, float ricochetChance, bool decal = false)
{
    public readonly EffectSystem[] Systems = systems;
    public readonly float Chance = chance;
    public readonly float RicochetChance = ricochetChance;
    public readonly bool Decal = decal;
}

internal class TracerImpactEffects
{
    private readonly TracerImpact[] _impacts;

    private readonly EffectBundle _tracerGreen;
    private readonly EffectBundle _tracerRed;
    private readonly EffectBundle _tracerYellow;
    private readonly EffectBundle _tracerWhite;

    private readonly GClass998 _lightPool;

    public TracerImpactEffects(Effects eftEffects, Dictionary<string, EffectBundle> mainEffects, Dictionary<string, EffectBundle> tracerEffects)
    {
        Plugin.Log.LogInfo("Defining effect bundles");
        var sparksWide = tracerEffects["Sparks_Wide"];
        var sparksGeneric = tracerEffects["Sparks_Generic"];
        var sparksGround = tracerEffects["Sparks_Ground"];
        var sparksFlammable = tracerEffects["Sparks_Flammable"];
        var sparksFlammableGround = tracerEffects["Sparks_Flammable_Ground"];
        var sparksHorRight = tracerEffects["Sparks_Hor_Right"];
        var sparksHorLeft = tracerEffects["Sparks_Hor_Left"];
        var sparksFalling = tracerEffects["Sparks_Falling"];
        var debrisFlammable = tracerEffects["Debris_Flammable"];

        var sparksGroundComb = EffectBundle.Merge(sparksGround, sparksWide);

        var flashSparks = mainEffects["Flash_Sparks"];

        var flame = tracerEffects["Flame"];
        _tracerGreen = tracerEffects["Tracer_Green"];
        _tracerRed = tracerEffects["Tracer_Red"];
        _tracerYellow = tracerEffects["Tracer_Yellow"];
        _tracerWhite = tracerEffects["Tracer_White"];

        var lowFlammable =
            new[]
            {
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(sparksHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksGroundComb, worldDir: WorldDir.Vertical | WorldDir.Up),
                    ],
                    generic: sparksGeneric,
                    forceGeneric: 0.33f
                ),
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(flashSparks),
                        new DirectionalEffect(sparksFalling, worldDir: WorldDir.Horizontal | WorldDir.Down, chance: 0.4f,
                            isChanceScaledByKinetics: true),
                    ]
                )
            };

        var midFlammable =
            new[]
            {
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(sparksHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksGroundComb, worldDir: WorldDir.Vertical | WorldDir.Up),
                    ],
                    generic: sparksGeneric,
                    forceGeneric: 0.33f
                ),
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(flashSparks),
                        new DirectionalEffect(debrisFlammable, chance: 0.1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(flame, worldDir: WorldDir.Vertical | WorldDir.Up, chance: 0.1f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(sparksFalling, worldDir: WorldDir.Horizontal | WorldDir.Down, chance: 0.1f,
                            isChanceScaledByKinetics: true),
                    ]
                )
            };

        var highFlammable =
            new[]
            {
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(sparksHorRight, camDir: CamDir.Angled | CamDir.Right, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksHorLeft, camDir: CamDir.Angled | CamDir.Left, worldDir: WorldDir.Horizontal),
                        new DirectionalEffect(sparksFlammableGround, worldDir: WorldDir.Vertical | WorldDir.Up),
                    ],
                    generic: sparksGeneric,
                    forceGeneric: 0.33f
                ),
                new EffectSystem(
                    directional:
                    [
                        new DirectionalEffect(sparksFlammable),
                        new DirectionalEffect(debrisFlammable, chance: 0.35f, isChanceScaledByKinetics: true),
                        new DirectionalEffect(flame, worldDir: WorldDir.Vertical | WorldDir.Up, chance: 0.5f, isChanceScaledByKinetics: true),
                    ]
                )
            };

        _impacts = new TracerImpact[Enum.GetNames(typeof(MaterialType)).Length];

        // Assign impact systems to materials
        _impacts[(int)MaterialType.Asphalt] = new TracerImpact(midFlammable, 0.45f, 0.6f, decal: true);
        _impacts[(int)MaterialType.Cardboard] = new TracerImpact(highFlammable, 0.6f, 0.1f, decal: true);
        _impacts[(int)MaterialType.Chainfence] = new TracerImpact(lowFlammable, 0.35f, 0.35f);
        _impacts[(int)MaterialType.Concrete] = new TracerImpact(midFlammable, 0.6f, 0.75f);
        _impacts[(int)MaterialType.Fabric] = new TracerImpact(highFlammable, 0.5f, 0.1f, decal: true);
        _impacts[(int)MaterialType.GarbageMetal] = new TracerImpact(lowFlammable, 0.5f, 0.7f);
        _impacts[(int)MaterialType.GarbagePaper] = new TracerImpact(highFlammable, 0.6f, 0.1f, decal: true);
        _impacts[(int)MaterialType.GenericSoft] = new TracerImpact(highFlammable, 0.4f, 0.1f, decal: true);
        _impacts[(int)MaterialType.Glass] = new TracerImpact(lowFlammable, 0.35f, 0.35f);
        _impacts[(int)MaterialType.GlassShattered] = new TracerImpact(lowFlammable, 0.35f, 0.35f);
        _impacts[(int)MaterialType.Grate] = new TracerImpact(lowFlammable, 0.35f, 0.6f);
        _impacts[(int)MaterialType.GrassHigh] = new TracerImpact(highFlammable, 0.3f, 0.2f, decal: true);
        _impacts[(int)MaterialType.GrassLow] = new TracerImpact(highFlammable, 0.3f, 0.3f, decal: true);
        _impacts[(int)MaterialType.Gravel] = new TracerImpact(lowFlammable, 0.5f, 0.4f);
        _impacts[(int)MaterialType.MetalThin] = new TracerImpact(lowFlammable, 0.6f, 0.6f);
        _impacts[(int)MaterialType.MetalThick] = new TracerImpact(lowFlammable, 0.6f, 0.8f);
        // _impacts[(int)MaterialType.Mud] = ;
        _impacts[(int)MaterialType.Pebbles] = new TracerImpact(lowFlammable, 0.35f, 0.4f);
        _impacts[(int)MaterialType.Plastic] = new TracerImpact(highFlammable, 0.5f, 0.1f, decal: true);
        _impacts[(int)MaterialType.Stone] = new TracerImpact(lowFlammable, 0.45f, 0.5f);
        // _impacts[(int)MaterialType.Soil] = ;
        // _impacts[(int)MaterialType.SoilForest] = ;
        _impacts[(int)MaterialType.Tile] = new TracerImpact(lowFlammable, 0.5f, 0.5f);
        _impacts[(int)MaterialType.WoodThick] = new TracerImpact(highFlammable, 0.6f, 0.1f, decal: true);
        _impacts[(int)MaterialType.WoodThin] = new TracerImpact(highFlammable, 0.45f, 0.1f, decal: true);
        _impacts[(int)MaterialType.Tyre] = new TracerImpact(highFlammable, 0.5f, 0.1f, decal: true);
        _impacts[(int)MaterialType.Rubber] = new TracerImpact(highFlammable, 0.5f, 0.1f, decal: true);
        _impacts[(int)MaterialType.GenericHard] = new TracerImpact(lowFlammable, 0.35f, 0.5f);
        _impacts[(int)MaterialType.MetalNoDecal] = new TracerImpact(lowFlammable, 0.45f, 0.6f);

        _lightPool = Traverse.Create(eftEffects).Field("gclass998_0").GetValue<GClass998>();
    }

    public void Emit(ImpactKinetics kinetics, AmmoItemClass ammo)
    {
        var impactDef = _impacts[(int)kinetics.Material];

        if (impactDef == null)
            return;

        if (!(Random.Range(0f, 1f) < impactDef.Chance * kinetics.Bullet.ChanceScale))
            return;

        if (impactDef.Decal)
        {
            Singleton<DecalPainter>.Instance.DrawDecal(
                Decals.TracerScrorchMark, kinetics.Position, kinetics.Normal, kinetics.Bullet.Info.HittedBallisticCollider
            );
        }

        for (var i = 0; i < impactDef.Systems.Length; i++)
        {
            var system = impactDef.Systems[i];
            system.Emit(kinetics, Plugin.EffectSize.Value);
        }

        var lightColor = Color.white;
        var tracer = _tracerWhite; 
        
        switch (ammo.TracerColor)
        {
            case TaxonomyColor.green or TaxonomyColor.tracerGreen:
                tracer = _tracerGreen;
                lightColor = new Color(0.9132687f, 1f, 0.7955974f);
                break;
            case TaxonomyColor.red or TaxonomyColor.tracerRed:
                tracer = _tracerRed;
                lightColor = new Color(1f, 0.8307356f, 0.7960784f);
                break;
            case TaxonomyColor.yellow or TaxonomyColor.tracerYellow:
                tracer = _tracerYellow;
                lightColor = new Color(1f, 0.9540824f, 0.7960784f);
                break;
        }
        
        if (!(Random.Range(0f, 1f) < impactDef.RicochetChance * kinetics.Bullet.ChanceScale))
        {
            tracer.Emit(kinetics.Position, kinetics.Normal, kinetics.Bullet.SizeScale * Plugin.EffectSize.Value);
        }

        if (kinetics.DistanceToImpact <= 50f)
        {
            _lightPool.Add(kinetics.Position, lightColor, 2.5f);
        }
    }
}