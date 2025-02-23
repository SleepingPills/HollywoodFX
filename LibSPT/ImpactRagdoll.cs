using EFT.Ballistics;
using UnityEngine;

namespace HollywoodFX;

internal static class RagdollEffects
{
    public static void Apply(ImpactContext context)
    {
        if (!Plugin.RagdollEnabled.Value) return;

        var bulletInfo = ImpactStatic.BulletInfo;

        if (bulletInfo == null) return;

        var attachedRigidbody = bulletInfo.HitCollider.attachedRigidbody;

        if (attachedRigidbody == null)
            return;
        
        // Kinematic means it's not physics controlled
        if (attachedRigidbody.isKinematic)
            return;

        var scalingBase = 0.025f;

        // These are generally the loot items like guns on the ground, decrease the force to avoid yeeting them to the stratosphere
        if (context.Material == MaterialType.None)
            scalingBase *= 0.1f;

        var penetrationFactor = 0.6f;

        if (ImpactStatic.BulletInfo != null)
        {
            penetrationFactor = (0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, ImpactStatic.BulletInfo.PenetrationPower));
        }

        var bulletForce = scalingBase * context.KineticEnergy;
        var impactImpulse = penetrationFactor * bulletForce * Plugin.RagdollForceMultiplier.Value;

        // Find the root transform
        var cur = attachedRigidbody.transform;
        while (cur.parent != null)
        {
            cur = cur.parent;
        }
        
        // Generate an upwards force depending on how far up the hit point is compared to the base of the ragdoll.
        // Head is ~1.6, we scale progressively from 0.8 upwards and achieve maximum upthrust at 1.2.
        var upThrust = (bulletInfo.HitPoint - cur.position);
        upThrust.y = Mathf.InverseLerp(1.2f, 1.6f, upThrust.y);

        var direction = (bulletInfo.Direction + upThrust).normalized;
        attachedRigidbody.AddForceAtPosition(direction * impactImpulse, bulletInfo.HitPoint, ForceMode.Impulse);
    }
}
