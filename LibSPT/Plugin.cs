using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using HollywoodFX.Patches;
using SPT.Reflection.Patching;

namespace HollywoodFX
{
    [BepInPlugin("com.janky.HollywoodFX", "Janky's HollywoodFX", "1.0.0")]
    [SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        
        private void Awake()
        {
            Log = Logger;
            
            AssetRegistry.LoadBundles();

            new EffectsAwakePatch().Enable();
            new OnGameStartedPatch().Enable();
            new BulletImpactPatch().Enable();
            new EffectsEmitPatch().Enable();
            new ApplyDamageInfoPatch().Enable();

            Logger.LogInfo("HollywoodFX Loaded, praise the destruction!");
        }
    }
    
    public class ApplyDamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod(nameof(Player.ApplyDamageInfo));
        }

        [PatchPrefix]
        public static void Prefix(ref DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
        {
            damageInfo.Damage *= 0.1f;
            damageInfo.ArmorDamage *= 0.1f;
        }
    }

}
