using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class GoreEffects
{
    private readonly BloodEffects _bloodEffects;
    
    public GoreEffects(Effects eftEffects, GameObject prefabMain, GameObject prefabSquirts, GameObject prefabFinishers)
    {
        _bloodEffects = new BloodEffects(eftEffects, prefabMain, prefabSquirts, prefabFinishers);
        Singleton<BloodEffects>.Create(_bloodEffects);
    }

    public void Apply(ImpactKinetics kinetics)
    {
        var bulletInfo = ImpactStatic.Kinetics.Bullet.Info;

        if (bulletInfo == null) return;

        var rigidbody = bulletInfo.HitCollider.attachedRigidbody;

        if (rigidbody == null)
            return;

        // Find the root transform
        var root = rigidbody.transform;
        while (root.parent != null)
        {
            root = root.parent;
        }

        Player player = null;
        
        // Don't render blood effects on the local player
        if (kinetics.DistanceToImpact <= 2f && root.TryGetComponent(out player) && player.IsYourPlayer)
            return;
        
        if (rigidbody.gameObject.layer == LayerMaskClass.DeadbodyLayer)
        {
            if (Plugin.RagdollEnabled.Value)
            {
                // Reactivate static (kinematic) ragdolls
                if (rigidbody.isKinematic)
                {
                    if (player == null)
                        player = root.GetComponent<Player>();
            
                    if (!player.IsYourPlayer && player.TryGetComponent(out Corpse corpse))
                    {
                        corpse.Ragdoll.Start();
                    }                    
                }
                
                // Apply the impulse to all dead bodies
                ApplyRagdollImpulse(kinetics, bulletInfo, root, rigidbody);                
            }
            
            // Hackery for adding decals to dead bodies 
            if (player == null)
                player = root.GetComponent<Player>();
            
            var playerTraverse = Traverse.Create(player);
            var preAllocatedRenderersList = playerTraverse.Field("_preAllocatedRenderersList").GetValue<List<GStruct56>>();
            var playerBody = playerTraverse.Field("_playerBody").GetValue<PlayerBody>();
            
            preAllocatedRenderersList.Clear();
            playerBody.GetBodyRenderersNonAlloc(preAllocatedRenderersList);
            Singleton<Effects>.Instance.EffectsCommutator.PlayerMeshesHit(preAllocatedRenderersList, kinetics.Position, -kinetics.Normal);
        }

        if (Plugin.BloodEnabled.Value)
        {
            _bloodEffects.Emit(kinetics, rigidbody);
        }

        // if (Plugin.BulletWoundsEnabled.Value)
        // {
        //     ApplyBulletWounds(context);
        // }
    }

    // private void ApplyBulletWounds(ImpactKinetics kinetics)
    // {
    // }

    public static float CalculateImpactImpulse(BulletKinetics bullet)
    {
        var penetrationFactor = 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, bullet.Info.PenetrationPower);
        return 11.12f * bullet.Impulse * penetrationFactor * Plugin.RagdollForceMultiplier.Value;
    }

    private static void ApplyRagdollImpulse(ImpactKinetics kinetics, EftBulletClass bulletInfo, Transform root, Rigidbody rigidbody)
    {
        var impactImpulse = Mathf.Min(CalculateImpactImpulse(kinetics.Bullet), 100f);

        // Generate an upwards force depending on how far up the hit point is compared to the base of the ragdoll.
        // Head is ~1.6, we scale progressively from 0.8 upwards and achieve maximum upthrust at 1.2.
        var upThrust = (bulletInfo.HitPoint - root.position);
        upThrust.y = Mathf.InverseLerp(1.2f, 1.6f, upThrust.y);

        var direction = (bulletInfo.Direction + upThrust).normalized;
        rigidbody.AddForceAtPosition(direction * impactImpulse, bulletInfo.HitPoint, ForceMode.Impulse);
    }
}