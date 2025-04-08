using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using HollywoodFX.Helpers;
using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Muzzle;

internal class CurrentShot
{
    public bool Handled = true;

    public AmmoItemClass Ammo;
    public bool Silenced;
}

internal class MuzzleState(Transform fireport, List<MuzzleJet> jets, MuzzleSmoke[] smokes)
{
    public Transform Fireport = fireport;
    public List<MuzzleJet> Jets = jets;
    public MuzzleSmoke[] Smokes = smokes;
    public Weapon Weapon = null;
}

internal class MuzzleBlast(
    float kineticNormFactor,
    EffectBundle coreJet,
    EffectBundle mainJet,
    EffectBundle forwardJet,
    EffectBundle portJet,
    EffectBundle forwardSmoke,
    EffectBundle portSmoke,
    EffectBundle sparks,
    float chanceJet,
    float chanceSmoke,
    float chanceSparks
)
{
    private readonly EffectBundle _forwardSmoke = forwardSmoke;
    private readonly EffectBundle _portSmoke = portSmoke;
    
    private readonly EffectBundle _sparks = sparks;
    
    private readonly float _chanceSmoke = chanceSmoke;
    private readonly float _chanceSparks = chanceSparks;

    public void Emit(MuzzleState state, AmmoItemClass ammo, MuzzleLight light, float sqrCameraDistance)
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
        
        var scaleTotal = proximityScale * kineticsScale;
        var scaleJet = scaleTotal * Plugin.MuzzleEffectJetsSize.Value;

        if (Random.Range(0f, 1f) < chanceJet)
        {
            var fireportDir = -1 * state.Fireport.up;

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
            
            if (light != null)
                light.method_0();
        }
    }
}

internal class MuzzleBlastBundle(MuzzleBlast handgun, MuzzleBlast rifle, MuzzleBlast shotgun)
{
    public readonly MuzzleBlast Handgun = handgun;
    public readonly MuzzleBlast Rifle = rifle;
    public readonly MuzzleBlast Shotgun = shotgun;
}

public class MuzzleEffects
{
    private readonly CurrentShot _currentShot;
    private readonly Dictionary<int, MuzzleState> _muzzleStates;

    private readonly MuzzleBlastBundle _regularMuzzleBlasts;
    private readonly MuzzleBlastBundle _silencedMuzzleBlasts;

    public MuzzleEffects(Effects eftEffects)
    {
        _currentShot = new CurrentShot();
        _muzzleStates = new Dictionary<int, MuzzleState>();

        var muzzleBlastsPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Muzzle Blasts");
        var effectMap = EffectBundle.LoadPrefab(eftEffects, muzzleBlastsPrefab, false);

        Plugin.Log.LogInfo("Constructing muzzle blasts");

        // var regularForwardSmoke = effectMap["Regular_Forward_Smoke"];
        // var regularPortSmoke = effectMap["Regular_Port_Smoke"];
        // var regularSparks = effectMap["Regular_Sparks"];

        var rifleCoreJet = effectMap["Rifle_Core_Jet"];
        
        var riflePortJet = EffectBundle.Merge(
            effectMap["Rifle_Port_Jet"], effectMap["Rifle_Port_Jet"], effectMap["Rifle_Port_Jet_Dim"]
        );

        var rifleForwardJet = EffectBundle.Merge(
            effectMap["Rifle_Forward_Jet"], effectMap["Rifle_Forward_Jet"], effectMap["Rifle_Forward_Jet"],
            effectMap["Rifle_Forward_Jet_Dim"]
        );

        var rifleBlast = new MuzzleBlast(
            2500f,
            rifleCoreJet, effectMap["Rifle_Main_Jet"], rifleForwardJet, riflePortJet, null, null,
            null, 0.85f, 1f, 0.5f
        );
        
        var rifleBlastDim = new MuzzleBlast(
            2000f,
            rifleCoreJet, effectMap["Rifle_Main_Jet_Dim"], effectMap["Rifle_Forward_Jet_Dim"], effectMap["Rifle_Port_Jet_Dim"], null, null,
            null, 0.85f, 1f, 0.5f
        );

        _regularMuzzleBlasts = new MuzzleBlastBundle(rifleBlast, rifleBlast, rifleBlast);
        _silencedMuzzleBlasts = new MuzzleBlastBundle(rifleBlastDim, rifleBlastDim, rifleBlastDim);
    }

    public bool Emit(MuzzleManager manager, bool isVisible, float sqrCameraDistance)
    {
        if (!isVisible)
            return true;
        
        if (_currentShot.Handled)
        {
            // The last bullet fired was already handled. We are seeing muzzle updates for underbarrel stuff or a grenade launcher, etc... 
            return true;
        }

        _currentShot.Handled = true;

        var managerId = manager.gameObject.transform.GetInstanceID();

        if (!_muzzleStates.TryGetValue(managerId, out var state))
            return true;

        var bundle = _currentShot.Silenced ? _silencedMuzzleBlasts : _regularMuzzleBlasts;

        var blast = state.Weapon switch
        {
            PistolItemClass or RevolverItemClass or SmgItemClass => bundle.Handgun,
            ShotgunItemClass => bundle.Shotgun,
            _ => bundle.Rifle
        };

        blast.Emit(state, _currentShot.Ammo, manager.Light, sqrCameraDistance);

        // Smoke puffs
        // TODO:

        // Smoke trail
        if (state.Smokes != null && (sqrCameraDistance < 100.0))
        {
            for (var i = 0; i < state.Smokes.Length; i++)
            {
                var t = state.Smokes[i];
                t.Shot();
            }
        }

        return false;
    }

    public void UpdateCurrentShot(AmmoItemClass ammo, bool silenced)
    {
        _currentShot.Handled = false;

        _currentShot.Ammo = ammo;
        _currentShot.Silenced = silenced;
    }

    public void UpdateWeapon(MuzzleManager manager, Weapon weapon)
    {
        var managerId = manager.gameObject.transform.GetInstanceID();

        if (_muzzleStates.TryGetValue(managerId, out var state))
        {
            state.Weapon = weapon;
        }
    }

    public void UpdateMuzzle(MuzzleManager manager, MuzzleJet[] jets, MuzzleFume[] fumes, MuzzleSmoke[] smokes)
    {
        // Grab the fireport of the current weapon
        Transform fireport = null;

        var children = manager.Hierarchy.GetComponentsInChildren<Transform>();
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];

            if (child.name == "fireport")
                fireport = child;
        }

        if (fireport == null)
            return;

        var fireportDir = -1 * fireport.up;

        // It seems like the smoke emitters are the best way to determine the actual muzzle opening as the fireport doesn't take silencers into account 
        var candidateAngle = 10f;
        Transform candidateFireport = null;

        for (var i = 0; i < fumes.Length; i++)
        {
            var fume = fumes[i];

            var angle = Vector3.Angle(fireportDir, -1 * fume.transform.up);

            if (!(angle < candidateAngle)) continue;

            candidateFireport = fume.transform;
            candidateAngle = angle;
        }

        if (candidateFireport != null)
            fireport = candidateFireport;

        var managerId = manager.gameObject.transform.GetInstanceID();

        // Get or create the muzzle state entry for this manager
        if (_muzzleStates.TryGetValue(managerId, out var state))
        {
            state.Fireport = fireport;
        }
        else
        {
            _muzzleStates[managerId] = state = new MuzzleState(fireport, [], smokes);
        }

        // Update the jets
        state.Jets.Clear();

        if (jets != null)
        {
            for (var i = 0; i < jets.Length; i++)
            {
                var jet = jets[i];
                var jetDir = -1 * jet.transform.up;
                var jetAngle = Vector3.Angle(jetDir, fireportDir);

                if (jetAngle > 20)
                    state.Jets.Add(jet);
            }
        }

        // Update the smokes
        state.Smokes = smokes;
    }
}