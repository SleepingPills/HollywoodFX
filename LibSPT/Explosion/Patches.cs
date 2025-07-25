using System.Reflection;
using Comfort.Common;
using EFT.UI;
using HollywoodFX.Patches;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class EffectsEmitGrenadePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Need to disambiguate the correct emit method
        return typeof(Effects).GetMethod(nameof(Effects.EmitGrenade));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(string ename, Vector3 position, Vector3 normal)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        ConsoleScreen.Log($"Grenade: {ename}");

        Singleton<ExplosionController>.Instance.Emit(position);
    }
}