using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Gore;
using SPT.Reflection.Patching;
using UnityEngine;
using MemoryExtensions = System.MemoryExtensions;

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

        var bloodEffects = Singleton<BodyImpactEffects>.Instance;

        if (Time.fixedTime - damage.FrameTime <= 0.3f)
        {
            var scaledImpulse = Mathf.Min(5f * GoreController.CalculateImpactImpulse(damage.Impulse, damage.PenetrationPower), 200f);
            rigidbody.AddForceAtPosition(damage.Direction * scaledImpulse, damage.HitPoint, ForceMode.Impulse);

            if (rigidbody.name.Length >= 11)
            {
                var nameSubset = MemoryExtensions.AsSpan(rigidbody.name, 10);

                if (damage.Penetrated && MemoryExtensions.StartsWith(nameSubset, "Spine")
                    || MemoryExtensions.StartsWith(nameSubset, "Pelvis")
                    || MemoryExtensions.StartsWith(nameSubset, "Head")
                    || MemoryExtensions.StartsWith(nameSubset, "Neck"))
                {
                    // Average the normal and the opposite of the hit direction (normal + (-1 * direction)) = normal - direction
                    var damageHitNormal = damage.HitNormal - damage.Direction;
                    damageHitNormal.Normalize();

                    var sizeScale = Mathf.Min(damage.SizeScale, 1f);

                    bloodEffects.EmitFinisher(rigidbody, damage.HitPoint, damageHitNormal, sizeScale);
                    bloodEffects.EmitBleedout(rigidbody, damage.HitPoint, damageHitNormal, sizeScale);
                }
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