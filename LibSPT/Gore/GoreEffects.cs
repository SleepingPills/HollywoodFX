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

    public GoreEffects(
        Effects eftEffects, GameObject prefabMain, GameObject prefabSquirts, GameObject prefabBleeds, GameObject prefabBleedouts,
        GameObject prefabFinishers
    )
    {
        Singleton<BloodEffects>.Create(_bloodEffects = new BloodEffects(
            eftEffects, prefabMain, prefabSquirts, prefabBleeds, prefabBleedouts, prefabFinishers
        ));
    }

    public void Apply(ImpactKinetics kinetics)
    {
        var bullet = kinetics.Bullet;
        var bulletInfo = bullet.Info;

        if (bulletInfo == null)
            return;

        if (bulletInfo.HitCollider == null)
            return;

        var hitColliderRoot = bullet.HitColliderRoot;

        // Don't render blood effects on the local player
        if (hitColliderRoot == ImpactStatic.LocalPlayerTransform)
            return;

        var rigidbody = bulletInfo.HitCollider.attachedRigidbody;

        if (rigidbody == null)
            return;

        if (rigidbody.gameObject.layer == LayerMaskClass.DeadbodyLayer)
        {
            Player player = null;

            if (Plugin.RagdollEnabled.Value)
            {
                // Reactivate static (kinematic) ragdolls
                if (rigidbody.isKinematic)
                {
                    if (hitColliderRoot.TryGetComponent(out player))
                    {
                        if (!player.IsYourPlayer && player.TryGetComponent(out Corpse corpse))
                        {
                            corpse.Ragdoll.Start();
                        }
                    }
                }

                // Apply the impulse to dead bodies
                ApplyRagdollImpulse(kinetics, bulletInfo, hitColliderRoot, rigidbody);
            }

            if (Plugin.WoundDecalsEnabled.Value)
            {
                // Hackery for adding decals to dead bodies 
                if (player != null || hitColliderRoot.TryGetComponent(out player))
                {
                    var playerTraverse = Traverse.Create(player);
                    var preAllocatedRenderersList = playerTraverse.Field("_preAllocatedRenderersList").GetValue<List<GStruct58>>();
                    var playerBody = playerTraverse.Field("_playerBody").GetValue<PlayerBody>();

                    preAllocatedRenderersList.Clear();
                    playerBody.GetBodyRenderersNonAlloc(preAllocatedRenderersList);
                    Singleton<Effects>.Instance.EffectsCommutator.PlayerMeshesHit(preAllocatedRenderersList, kinetics.Position, -kinetics.Normal);
                }
            }
        }

        if (Plugin.BloodEnabled.Value)
        {
            _bloodEffects.Emit(kinetics, rigidbody);
        }
    }

    public static float CalculateImpactImpulse(float impulse, float penetration)
    {
        var penetrationFactor = 0.7f + 0.3f * Mathf.InverseLerp(50f, 20f, penetration);
        return 8f * impulse * penetrationFactor * Plugin.RagdollForceMultiplier.Value;
    }

    private static void ApplyRagdollImpulse(ImpactKinetics kinetics, EftBulletClass bulletInfo, Transform root, Rigidbody rigidbody)
    {
        var impactImpulse = Mathf.Min(CalculateImpactImpulse(kinetics.Bullet.Impulse, kinetics.Bullet.Info.PenetrationPower), 100f);

        // Generate an upwards force depending on how far up the hit point is compared to the base of the ragdoll.
        // Head is ~1.6, we scale progressively from 0.8 upwards and achieve maximum upthrust at 1.2.
        var upThrust = (bulletInfo.HitPoint - root.position);
        upThrust.y = Mathf.InverseLerp(1.2f, 1.6f, upThrust.y);

        var direction = (bulletInfo.Direction + upThrust).normalized;
        rigidbody.AddForceAtPosition(direction * impactImpulse, bulletInfo.HitPoint, ForceMode.Impulse);
    }
}