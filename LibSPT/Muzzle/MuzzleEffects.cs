using EFT.UI;
using HollywoodFX.Helpers;
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
        }
        
        if (Random.Range(0f, 1f) < chanceSmoke)
        {   
            // Side jets
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

    public void SetParent(Transform parent)
    {
        coreJet.SetParent(parent);
        mainJet.SetParent(parent);
        forwardJet.SetParent(parent);
        portJet.SetParent(parent);
        sparks.SetParent(parent);
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
    protected readonly MuzzleBlastBundle RegularMuzzleBlasts;
    protected readonly MuzzleBlastBundle SilencedMuzzleBlasts;

    public MuzzleEffects(Effects eftEffects, bool forceWorldSim)
    {
        var muzzleBlastsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Muzzle Blasts");
        var effectMap = EffectBundle.LoadPrefab(eftEffects, muzzleBlastsPrefab, true);

        if (forceWorldSim)
        {
            foreach (var bundle in effectMap.Values)
            {
                foreach (var particleSystem in bundle.ParticleSystems)
                {
                    foreach (var subSystem in particleSystem.GetComponentsInChildren<ParticleSystem>())
                    {
                        Plugin.Log.LogInfo($"Forcing {subSystem.name} to world space simulation");
                        var main = subSystem.main;
                        main.simulationSpace = ParticleSystemSimulationSpace.World;                        
                    }
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

        RegularMuzzleBlasts = new MuzzleBlastBundle(rifleBlast, smgBlast, rifleBlast, rifleBlast);
        SilencedMuzzleBlasts = new MuzzleBlastBundle(rifleBlastDim, smgBlastDim, rifleBlastDim, rifleBlastDim);
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
            var bundle = currentShot.Silenced ? SilencedMuzzleBlasts : RegularMuzzleBlasts;

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
        if (state.Smokes != null && (isVisible || sqrCameraDistance < 16f))
        {
            for (var i = 0; i < state.Smokes.Length; i++)
            {
                var t = state.Smokes[i];
                t.Shot();
            }
        }

        return false;
    }
}

internal class LocalPlayerMuzzleEffects(Effects eftEffects) : MuzzleEffects(eftEffects, false)
{
    public void UpdateParents(MuzzleState state)
    {
        var parent = state.Fireport.transform;
        
        SetParent(RegularMuzzleBlasts, parent);
        SetParent(SilencedMuzzleBlasts, parent);
    }

    private static void SetParent(MuzzleBlastBundle bundle, Transform parent)
    {
        bundle.Handgun.SetParent(parent);
        bundle.Smg.SetParent(parent);
        bundle.Rifle.SetParent(parent);
        bundle.Shotgun.SetParent(parent);
    }
}