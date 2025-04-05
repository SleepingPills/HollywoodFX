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

internal class MuzzleBlast(EffectBundle forwardJet, EffectBundle portJet, EffectBundle forwardSmoke, EffectBundle portSmoke, EffectBundle sparks)
{
    private readonly EffectBundle _forwardJet = forwardJet;
    private readonly EffectBundle _portJet = portJet;

    private readonly EffectBundle _forwardSmoke = forwardSmoke;
    private readonly EffectBundle _portSmoke = portSmoke;

    private readonly EffectBundle _sparks = sparks;

    public void Emit(MuzzleState state, AmmoItemClass ammo)
    {
        
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
        var effectMap = EffectBundle.LoadPrefab(eftEffects, muzzleBlastsPrefab, true);

        Plugin.Log.LogInfo("Constructing muzzle blasts");

        var rifleBlast = new MuzzleBlast();

        _regularMuzzleBlasts = new MuzzleBlastBundle(rifleBlast, rifleBlast, rifleBlast);
        _silencedMuzzleBlasts = new MuzzleBlastBundle(rifleBlast, rifleBlast, rifleBlast);

        // Define major building blocks for systems
        Plugin.Log.LogInfo("Building frontal puffs");
        var puffFront = effectMap["Puff_Front"];
        var puffFrontDusty = effectMap["Puff_Dusty_Front"];
        var puffFrontBody = EffectBundle.Merge(effectMap["Puff_Body_Front"], puffFrontDusty);
        var puffFrontRock = EffectBundle.Merge(puffFront, puffFrontDusty);
    }

    public bool Emit(MuzzleManager manager, bool isVisible, float sqrCameraDistance)
    {
        if (_currentShot.Handled)
        {
            // The last bullet fired was already handled. We are seeing muzzle updates for underbarrel stuff or a grenade launcher, etc... 
            return true;
        }

        _currentShot.Handled = true;

        var managerId = manager.gameObject.transform.GetInstanceID();

        if (!_muzzleStates.TryGetValue(managerId, out var state))
            return true;

        // Kinetics
        var kineticsScale = 1f;
        // _currentShot.Silenced;
        // TODO: Update the kinetics scaling
        // state.Scaling =
        // Use _currentShot.Ammo.ProjectileCount to determine whether we are a shotgun or not

        // Reach max size at 1.5m (2.25 = 1.5^2)
        var proximityScale = 1f + 0.5f * Mathf.Lerp(0f, 2.25f, sqrCameraDistance);

        var totalScale = proximityScale * kineticsScale;
        var jetScale = totalScale * Plugin.MuzzleEffectJetsSize.Value;

        var bundle = _currentShot.Silenced ? _silencedMuzzleBlasts : _regularMuzzleBlasts;

        var blast = bundle.Rifle;

        if (state.Weapon is PistolItemClass or RevolverItemClass or SmgItemClass)
        {
            blast = bundle.Handgun;
        }
        else if (state.Weapon is ShotgunItemClass)
        {
            blast = bundle.Shotgun;
        }
        
        blast.Emit(state, _currentShot.Ammo);
        
        /*
        // Fireport jet
        DebugGizmos.Ray(state.Fireport.position, -1 * state.Fireport.up, _currentShot.Silenced ? Color.green : Color.red, temporary: true,
            expiretime: 1f,
            length: jetScale * 0.25f, lineWidth: jetScale * 0.05f);

        // Side jets
        for (var i = 0; i < state.Jets.Count; i++)
        {
            var jet = state.Jets[i];


            DebugGizmos.Ray(jet.transform.position, -1 * jet.transform.up, Color.blue, temporary: true, expiretime: 1f,
                length: jetScale * 0.25f, lineWidth: jetScale * 0.05f);
        }

        // Smoke puffs
        // TODO:
        */
        
        // Smoke trail
        if (state.Smokes != null && (isVisible && sqrCameraDistance < 100.0 || !isVisible && sqrCameraDistance < 4.0))
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

        // Get or create the muzzle state entry for this manager
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