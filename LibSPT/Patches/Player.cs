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
            var scaledImpulse = Mathf.Min(10f * GoreEffects.CalculateImpactImpulse(damage.Impulse, damage.Penetration), 575f);

            rigidbody.AddForceAtPosition(damage.Direction * scaledImpulse, damage.HitPoint, ForceMode.Impulse);

            bloodEffects.EmitFinisher(rigidbody, damage.HitPoint, damage.HitNormal, Mathf.Min(damage.SizeScale, 1.1f));
        }
        else
        {
            ConsoleScreen.Log($"Bleedout emitted");
            bloodEffects.EmitBleedout(rigidbody, damage.HitPoint, damage.HitNormal, Mathf.Min(damage.SizeScale, 1.1f));
        }
        
        // Clean out the entry from the dictionary as we no longer need it if the enemy is dead
        playerDamageRegistry.Remove(instanceId);
    }
}