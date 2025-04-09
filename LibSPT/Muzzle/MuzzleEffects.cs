using System.Collections.Generic;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Muzzle;

internal class MuzzleBlast(
    float kineticNormFactor,
    EffectBundle coreJet,
    EffectBundle mainJet,
    EffectBundle forwardJet,
    EffectBundle portJet,
    EffectBundle forwardSmoke,
    EffectBundle portSmoke,
    EffectBundle lingerSmoke,
    EffectBundle sparks,
    float chanceJet,
    float chanceSmoke,
    float chanceSparks
)
{

    public void Emit(MuzzleState state, AmmoItemClass ammo, float sqrCameraDistance)
    {
        // Kinetics
        var mass = Mathf.Max(ammo.BulletMassGram * ammo.ProjectileCount, 1f) / 1000;
        var speed = ammo.InitialSpeed;

        var impulse = mass * speed;
        var energy = impulse * speed / 2;

        var kineticsScale = Mathf.Clamp(Mathf.Sqrt(energy / kineticNormFactor), 0.5f, 1.2f);

        // Reach max size at 1.5m (2.25 = 1.5^2)
        var proximityScale = 0.75f + 0.5f * Mathf.Lerp(0f, 2.25f, sqrCameraDistance);
        var isThirdPerson = sqrCameraDistance > 0.5f;

        var adjustForwardJet = 1f;
        var adjustMainJet = 1f;

        // In 1st person view, the main jet is bigger and the forward jet is smaller
        if (!isThirdPerson)
        {
            adjustForwardJet = 0.5f;
        }

        var jetEmitted = false;
        var scaleTotal = proximityScale * kineticsScale;
        var fireportDir = -1 * state.Fireport.up;

        if (Random.Range(0f, 1f) < chanceJet)
        {
            jetEmitted = true;
            var scaleJet = scaleTotal * Plugin.MuzzleEffectJetsSize.Value;

            if (isThirdPerson)
            {
                var camera = CameraClass.Instance.Camera;
                var camAngle = Vector3.Angle(camera.transform.forward, fireportDir);

                var frontFacingFactor = Mathf.InverseLerp(160f, 140f, camAngle);

                // Decrease the size of the forward jet as we approach a full frontal view angle
                adjustForwardJet *= frontFacingFactor;

                // When the muzzle fully faces the camera, scale the main jet up by 25% to account for the forward jet being de-scaled 
                adjustMainJet += 0.25f * (1f - frontFacingFactor);

                // Only emit this in 3rd pov as it generates too much bloom in fpv
                coreJet.EmitDirect(state.Fireport.position, fireportDir, scaleJet * adjustMainJet);
            }

            mainJet.EmitDirect(state.Fireport.position, fireportDir, scaleJet * adjustMainJet);

            if (adjustForwardJet > 0.01f)
                forwardJet.EmitDirect(state.Fireport.position, fireportDir, scaleJet * adjustForwardJet);
            
            // Side jets
            for (var i = 0; i < state.Jets.Count; i++)
            {
                var jet = state.Jets[i];
                portJet.EmitDirect(jet.transform.position, -1 * jet.transform.up, scaleJet, 1);
            }
        }

        if (!jetEmitted || Random.Range(0f, 1f) < chanceSparks)
        {
            // Add a bit of randomness to the spark size
            var scaleSparks = scaleTotal * Plugin.MuzzleEffectSparksSize.Value * Random.Range(0.75f, 1.25f);
            sparks.EmitDirect(state.Fireport.position, fireportDir, scaleSparks);
        }
        
        var scaleSmoke = scaleTotal * Plugin.MuzzleEffectSmokeSize.Value * Random.Range(0.75f, 1.25f);
        
        if (!jetEmitted || Random.Range(0f, 1f) < chanceSmoke)
        {
            forwardSmoke.EmitDirect(state.Fireport.position, fireportDir, scaleSmoke);

            // Emit the misc smoke things like shell ejector port puffs 
            for (var i = 0; i < state.Smokes.Count; i++)
            {
                var smoke = state.Smokes[i];
                portSmoke.EmitDirect(smoke.transform.position, -1 * smoke.transform.up, scaleSmoke, 1);
            }
        }
        
        if (Random.Range(0f, 1f) < chanceSmoke)
        {   
            // Port smoke emission
            for (var i = 0; i < state.Jets.Count; i++)
            {
                var jet = state.Jets[i];
                portSmoke.EmitDirect(jet.transform.position, -1 * jet.transform.up, scaleSmoke, 1);
            }
        }
        
        if (Random.Range(0f, 1f) < (0.5 *  chanceSmoke))
        {
            lingerSmoke.EmitDirect(state.Fireport.position, fireportDir, scaleSmoke);
        }
    }
}

internal class MuzzleBlastBundle(MuzzleBlast handgun, MuzzleBlast smg, MuzzleBlast rifle, MuzzleBlast shotgun)
{
    public readonly MuzzleBlast Handgun = handgun;
    public readonly MuzzleBlast Smg = smg;
    public readonly MuzzleBlast Rifle = rifle;
    public readonly MuzzleBlast Shotgun = shotgun;
}

internal class MuzzleEffects
{
    protected readonly List<ParticleSystem> ParticleSystems;
    
    private readonly MuzzleBlastBundle _regularMuzzleBlasts;
    private readonly MuzzleBlastBundle _silencedMuzzleBlasts;

    public MuzzleEffects(Effects eftEffects, bool forceWorldSim)
    {
        var muzzleBlastsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Muzzle Blasts");
        var effectMap = EffectBundle.LoadPrefab(eftEffects, muzzleBlastsPrefab, true);
        
        ParticleSystems = [];

        foreach (var bundle in effectMap.Values)
        {
            foreach (var particleSystem in bundle.ParticleSystems)
            {
                // We only add the top level particle systems as modifying the sub-system parent-child hierarchy breaks things
                ParticleSystems.Add(particleSystem);
                
                if (!forceWorldSim) continue;
                
                foreach (var subSystem in particleSystem.GetComponentsInChildren<ParticleSystem>())
                {
                    Plugin.Log.LogInfo($"Forcing {subSystem.name} to world space simulation");
                    var main = subSystem.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                }
            }
        }

        Plugin.Log.LogInfo("Constructing muzzle blasts");

        var rifleCoreJet = effectMap["Rifle_Core_Jet"];
        var rifleMainJet = effectMap["Rifle_Main_Jet"];
        var rifleMainJetDim = effectMap["Rifle_Main_Jet_Dim"];

        var riflePortJetDim = effectMap["Rifle_Port_Jet_Dim"];
        var riflePortJet = EffectBundle.Merge(effectMap["Rifle_Port_Jet"], effectMap["Rifle_Port_Jet"], riflePortJetDim);

        var rifleForwardJetDim = effectMap["Rifle_Forward_Jet_Dim"];
        var rifleForwardJet = EffectBundle.Merge(
            effectMap["Rifle_Forward_Jet"], effectMap["Rifle_Forward_Jet"], effectMap["Rifle_Forward_Jet"], rifleForwardJetDim
        );
        
        var rifleForwardSmoke = effectMap["Rifle_Forward_Smoke"];
        var riflePortSmoke = effectMap["Rifle_Port_Smoke"];
        var rifleLingerSmoke = effectMap["Rifle_Linger_Smoke"];

        var rifleSparks = effectMap["Rifle_Sparks"];
        var rifleSparksDim = effectMap["Rifle_Sparks_Dim"];

        var rifleBlast = new MuzzleBlast(
            2500f,
            rifleCoreJet, rifleMainJet, rifleForwardJet, riflePortJet, rifleForwardSmoke, riflePortSmoke, rifleLingerSmoke, rifleSparks,
            0.85f, 0.5f, 0.5f
        );

        var rifleBlastDim = new MuzzleBlast(
            2000f,
            rifleCoreJet, rifleMainJetDim, rifleForwardJetDim, riflePortJetDim, rifleForwardSmoke, riflePortSmoke, rifleLingerSmoke, rifleSparksDim,
            0.5f, 0.85f,0.85f
        );

        var smgBlast = new MuzzleBlast(
            1000f,
            rifleCoreJet, rifleMainJet, rifleForwardJet, riflePortJet, rifleForwardSmoke, riflePortSmoke, rifleLingerSmoke, rifleSparks,
            0.85f, 0.5f, 0.5f
        );

        var smgBlastDim = new MuzzleBlast(
            500f,
            rifleCoreJet, rifleMainJetDim, rifleForwardJetDim, riflePortJetDim, rifleForwardSmoke, riflePortSmoke, rifleLingerSmoke, rifleSparksDim,
            0.5f, 0.85f, 0.85f
        );

        _regularMuzzleBlasts = new MuzzleBlastBundle(rifleBlast, smgBlast, rifleBlast, rifleBlast);
        _silencedMuzzleBlasts = new MuzzleBlastBundle(rifleBlastDim, smgBlastDim, rifleBlastDim, rifleBlastDim);
    }

    public bool Emit(CurrentShot currentShot, MuzzleState state, bool isVisible, float sqrCameraDistance)
    {
        if (currentShot.Handled)
        {
            // The last bullet fired was already handled. We are seeing muzzle updates for underbarrel stuff or a grenade launcher, etc... 
            return true;
        }

        currentShot.Handled = true;

        if (isVisible || sqrCameraDistance < 200f)
        {
            var bundle = currentShot.Silenced ? _silencedMuzzleBlasts : _regularMuzzleBlasts;

            var blast = state.Weapon switch
            {
                AssaultRifleItemClass or MarksmanRifleItemClass or SniperRifleItemClass => bundle.Rifle,
                PistolItemClass or RevolverItemClass => bundle.Handgun,
                SmgItemClass => bundle.Smg,
                ShotgunItemClass => bundle.Shotgun,
                _ => bundle.Rifle
            };

            blast.Emit(state, currentShot.Ammo, sqrCameraDistance);            
        }

        // Smoke trail
        if (state.Trails == null || (!isVisible && !(sqrCameraDistance < 16f))) return false;
        
        for (var i = 0; i < state.Trails.Length; i++)
        {
            var t = state.Trails[i];
            t.Shot();
        }

        return false;
    }
}

internal class LocalPlayerMuzzleEffects(Effects eftEffects) : MuzzleEffects(eftEffects, false)
{
    public void UpdateParents(MuzzleState state)
    {
        var parent = state.Fireport.transform;
        
        // For the local player, locally or custom simulated particle systems need to get the parents and transforms assigned.
        // This is needed because the particles will lag behind the gun barrel during fast camera movements in first person view.  
        for (var i = 0; i < ParticleSystems.Count; i++)
        {
            var particleSystem = ParticleSystems[i];
            var main = particleSystem.main;

            if (main.simulationSpace == ParticleSystemSimulationSpace.World) continue;
            
            particleSystem.transform.SetParent(parent);
            
            // Custom space simulated systems also need the simulation space transform assigned 
            if (main.simulationSpace == ParticleSystemSimulationSpace.Custom)
            {
                main.customSimulationSpace = parent;
            }
        }
    }
}