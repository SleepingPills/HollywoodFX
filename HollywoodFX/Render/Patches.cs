using System.Reflection;
using Comfort.Common;
using EFT;
using HollywoodFX.Patches;
using SPT.Reflection.Patching;

namespace HollywoodFX.Render;

public class GameWorldInitConcussionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        if (GameWorldAwakePrefixPatch.IsHideout)
            return;

        
        var postProcessing = __instance.gameObject.AddComponent<PostProcessing>();
        postProcessing.Init();
        Singleton<PostProcessing>.Create(postProcessing);
    }
}
