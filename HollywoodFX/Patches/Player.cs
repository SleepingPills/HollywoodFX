using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
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

        if (damage.HitCollider == null)
            return;

        var rigidbody = damage.HitCollider.attachedRigidbody;

        if (rigidbody == null)
            return;

        var bloodEffects = Singleton<BloodEffects>.Instance;

        if (Time.fixedTime - damage.FrameTime <= 0.3f)
        {
            var scaledImpulse = Mathf.Min(5f * GoreEffects.CalculateImpactImpulse(damage.Impulse, damage.PenetrationPower), 200f);

            rigidbody.AddForceAtPosition(damage.Direction * scaledImpulse, damage.HitPoint, ForceMode.Impulse);

            // Average the normal and the opposite of the hit direction (normal + (-1 * direction)) = normal - direction
            var damageHitNormal = damage.HitNormal - damage.Direction;
            damageHitNormal.Normalize();

            var sizeScale = Mathf.Min(damage.SizeScale, 1f);
            bloodEffects.EmitFinisher(rigidbody, damage.HitPoint, damageHitNormal, sizeScale);

            ConsoleScreen.Log($"Rigidbody name: {rigidbody.name} penetrated: {damage.Penetrated}");
            if (damage.Penetrated && (rigidbody.name.Contains("Spine") || rigidbody.name.Contains("Head")
                                                                       || rigidbody.name.Contains("Neck")
                                                                       || rigidbody.name.Contains("Pelvis")))
            {
                bloodEffects.EmitBleedout(rigidbody, damage.HitPoint, damageHitNormal, sizeScale);
            }
        }
        else
        {
            bloodEffects.EmitBleedout(rigidbody, rigidbody.transform.position, damage.HitNormal, Mathf.Min(damage.SizeScale, 1f));
        }

        // Clean out the entry from the dictionary as we no longer need it if the enemy is dead
        playerDamageRegistry.Remove(instanceId);
    }
}