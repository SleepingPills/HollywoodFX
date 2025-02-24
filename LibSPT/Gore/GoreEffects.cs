using System.Collections.Generic;
using Comfort.Common;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Gore;

public class GoreEffects(Effects eftEffects, GameObject prefab)
{
    private readonly BloodEffects _bloodEffects = new(eftEffects, prefab);

    public void Apply(ImpactKinetics kinetics)
    {
        var bulletInfo = ImpactStatic.BulletInfo;

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
        
        if (Plugin.RagdollEnabled.Value && !rigidbody.isKinematic)
        {
            ApplyRagdollImpulse(kinetics, bulletInfo, root, rigidbody);
        }

        if (Plugin.BloodEnabled.Value)
        {
            _bloodEffects.Emit(kinetics);
        }
        
        // if (Plugin.BulletWoundsEnabled.Value)
        // {
        //     ApplyBulletWounds(context);
        // }
    }

    private void ApplyBulletWounds(ImpactKinetics kinetics)
    {
        // TODO
    }
    
    private static void ApplyRagdollImpulse(ImpactKinetics kinetics, EftBulletClass bulletInfo, Transform root, Rigidbody rigidbody)
    {
        var penetrationFactor = 0.6f;

        if (ImpactStatic.BulletInfo != null)
        {
            penetrationFactor = (0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, ImpactStatic.BulletInfo.PenetrationPower));
        }

        var impactImpulse = 11.12f * kinetics.Impulse * penetrationFactor * Plugin.RagdollForceMultiplier.Value;
        
        // Generate an upwards force depending on how far up the hit point is compared to the base of the ragdoll.
        // Head is ~1.6, we scale progressively from 0.8 upwards and achieve maximum upthrust at 1.2.
        var upThrust = (bulletInfo.HitPoint - root.position);
        upThrust.y = Mathf.InverseLerp(1.2f, 1.6f, upThrust.y);

        var direction = (bulletInfo.Direction + upThrust).normalized;
        rigidbody.AddForceAtPosition(direction * impactImpulse, bulletInfo.HitPoint, ForceMode.Impulse);
    }
}
