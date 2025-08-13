using System.Reflection;
using SPT.Reflection.Patching;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace HollywoodFX.Patches;

/*
 * This patch is required because BSG now applies switching between player model LOD levels. Unfortunately decals are tied to renderers which are
 * themselves tied to specific LODs. If a decal is placed on a far away LOD level, it will not be visible up close (and vice versa). So we simply
 * apply decals irrespective of visibility. This will also ensure that dead bodies from firefights far away get covered in blood as appropriate.
 */
class TextureDecalsPainterVisCheckPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TextureDecalsPainter).GetMethod(nameof(TextureDecalsPainter.method_5));
    }

    [PatchPrefix]
    public static bool Prefix(TextureDecalsPainter __instance, Renderer objRenderer, ref bool __result)
    {
        // objRenderer.isVisible nixed
        
        __result = objRenderer.enabled && objRenderer.gameObject.activeSelf &&
                   objRenderer.material.shader.name.Contains("_Decal");
        
        return false;
    }
}