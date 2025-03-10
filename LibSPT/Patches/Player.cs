using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Gore;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodFX.Patches;

internal class PlayerOnDeadPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.OnDead));
    }

    [PatchPostfix]
    // ReSharper disable InconsistentNaming
    public static void Postfix(Player __instance)
    {
        var instanceId = __instance.gameObject.transform.GetInstanceID();

        var playerDamageRegistry = Singleton<PlayerDamageRegistry>.Instance;

        if (!playerDamageRegistry.TryGetValue(instanceId, out var damage)) return;
        
        if (Time.fixedTime - damage.FrameTime > 0.3f)
            return;

        if (damage.HitCollider == null)
            return;

        var rigidbody = damage.HitCollider.attachedRigidbody;

        if (rigidbody == null)
            return;

        var scaledImpulse = Mathf.Min(10f * GoreEffects.CalculateImpactImpulse(damage.Impulse, damage.Penetration), 350f);

        rigidbody.AddForceAtPosition(damage.Direction * scaledImpulse, damage.HitPoint, ForceMode.Impulse);
        Singleton<BloodEffects>.Instance.EmitFinisher(rigidbody, damage.HitPoint, damage.HitNormal, damage.SizeScale);

        // Clean out the entry from the dictionary as we no longer need it if the enemy is dead
        playerDamageRegistry.Remove(instanceId);
    }
}
