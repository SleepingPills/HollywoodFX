using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Patches
{
    public class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        // ReSharper disable once InconsistentNaming
        public static void Postfix(GameWorld __instance)
        {
            var effectsInstance = Singleton<Effects>.Instance;
            // SetBloodDecals(effectsInstance);
            LoadImpactEffects(effectsInstance);
        }
        
        private static void SetBloodDecals(Effects effects)
        {
            var painter = effects.TexDecals;
            var bloodDecalTexField = typeof(TextureDecalsPainter).GetField("_bloodDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var vestDecalField = typeof(TextureDecalsPainter).GetField("_vestDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var backDecalField = typeof(TextureDecalsPainter).GetField("_backDecalTexture", BindingFlags.NonPublic | BindingFlags.Instance);

            var bloodDecals = bloodDecalTexField?.GetValue(painter);

            if (bloodDecals == null)
                return;

            Logger.LogInfo($"Overriding blood decal textures");
            vestDecalField?.SetValue(painter, bloodDecals);
            backDecalField?.SetValue(painter, bloodDecals);
        }

        private static void LoadImpactEffects(Effects effects)
        {
            Logger.LogInfo("Loading Impacts Prefab");
            var impactsPrefab = AssetRegistry.ImpactsBundle.LoadAsset<GameObject>("HFX Impacts");
            Logger.LogInfo("Instantiating Impact Effects Prefab");
            var impactInstance = Object.Instantiate(impactsPrefab);
            Logger.LogInfo("Getting Effects Component");
            var impactEffects = impactInstance.GetComponent<Effects>();

            Logger.LogInfo($"Loaded {impactEffects.EffectsArray.Length} extra effects");
            for (var i = 0; i < impactEffects.EffectsArray.Length; i++)
            {
                var effect = impactEffects.EffectsArray[i];
                Logger.LogInfo($"Effect index {i} name {effect.Name}.");
            }

            Logger.LogInfo("Replacing transform parent with internal effects instance");
            foreach (var child in impactInstance.transform.GetChildren())
            {
                child.parent = effects.transform;
            }

            Logger.LogInfo("Adding new effects to the internal effects instance");
            List<Effects.Effect> customEffectsList = [];
            customEffectsList.AddRange(effects.EffectsArray);
            customEffectsList.AddRange(impactEffects.EffectsArray);

            effects.EffectsArray = [.. customEffectsList];

            Logger.LogInfo("Constructing impact systems");

            var effectMap = impactEffects.EffectsArray.ToDictionary(x => x.Name, x => x);

            ImpactEffectsController.Instance.Setup(effectMap);
        }
    }

    public class EffectsAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Effects).GetMethod(nameof(Effects.Awake));
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance)
        {
            var decalRenderer = __instance.DeferredDecals;

            if (decalRenderer == null) return;

            var maxStaticDecalsField = decalRenderer.GetType().GetField("_maxDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxStaticDecalsField != null)
            {
                var maxStaticDecalsValue = (int)maxStaticDecalsField.GetValue(decalRenderer);

                Logger.LogWarning($"Current static decals limit is: {maxStaticDecalsValue}");
                if (maxStaticDecalsValue < 2048)
                {
                    Logger.LogWarning($"Setting max static decals to 2048");
                    maxStaticDecalsField.SetValue(decalRenderer, 2048);
                }
            }

            var maxDynamicDecalsField = decalRenderer.GetType().GetField("_maxDynamicDecals", BindingFlags.NonPublic | BindingFlags.Instance);

            if (maxDynamicDecalsField == null) return;

            var maxDynamicDecalsValue = (int)maxDynamicDecalsField.GetValue(decalRenderer);

            Logger.LogWarning($"Current dynamic decals limit is: {maxDynamicDecalsValue}");
            if (maxDynamicDecalsValue >= 2048) return;

            Logger.LogWarning($"Setting max dynamic decals to 2048");
            maxDynamicDecalsField.SetValue(decalRenderer, 2048);
            
            var maxConcurrentParticles = typeof(Effects).GetField("int_0", BindingFlags.NonPublic | BindingFlags.Static);

            if (maxConcurrentParticles != null)
            {
                var maxConcurrentParticlesValue = (int)maxConcurrentParticles.GetValue(null);

                Logger.LogWarning($"Current concurrent particle system limit is: {maxConcurrentParticlesValue}");
                if (maxConcurrentParticlesValue < 100)
                {
                    Logger.LogWarning($"Setting max concurrent particle system limit to 100");
                    maxConcurrentParticles.SetValue(null, 100);
                }
            }
            
            HashSet<MaterialType> materialsTypes =
            [
                MaterialType.Chainfence,
                MaterialType.GarbageMetal,
                MaterialType.Grate,
                MaterialType.MetalThin,
                MaterialType.MetalThick,
                MaterialType.MetalNoDecal,
                MaterialType.Concrete,
                MaterialType.Stone
            ];

            foreach (var effect in __instance.EffectsArray)
            {
                Logger.LogInfo($"Processing {effect.Name}");
                foreach (var materialType in effect.MaterialTypes)
                {
                    if (!materialsTypes.Contains(materialType)) continue;

                    var filteredParticles = new List<Effects.Effect.ParticleSys>();

                    foreach (var particle in effect.Particles)
                    {
                        if (particle.Particle.name.Contains("Spark"))
                        {
                            Logger.LogInfo($"Dropping {particle.Particle.name}");
                            continue;
                        }

                        Logger.LogInfo($"Keeping {particle.Particle.name}");
                        filteredParticles.Add(particle);
                    }

                    Logger.LogInfo(
                        $"Clearing out particles for {effect.Name} material {materialType}: {effect.Particles}, {effect.Flash}, {effect.FlareID}"
                    );

                    effect.Particles = filteredParticles.ToArray();
                    effect.Flash = false;
                    effect.FlareID = 0;
                    break;
                }
            }
        }
    }

    public class EffectsEmitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Need to disambiguate the correct emit method
            return typeof(Effects).GetMethod(nameof(Effects.Emit),
            [
                typeof(MaterialType), typeof(BallisticCollider), typeof(Vector3), typeof(Vector3), typeof(float),
                typeof(bool), typeof(bool), typeof(EPointOfView)
            ]);
        }

        [PatchPrefix]
        // ReSharper disable once InconsistentNaming
        public static void Prefix(Effects __instance, MaterialType material, BallisticCollider hitCollider,
            Vector3 position, Vector3 normal, float volume, bool isKnife, ref bool isHitPointVisible, EPointOfView pov)
        {
            // Render things within 2 meters of the camera even if the impact location is not directly in the viewport
            if (!isHitPointVisible)
            {
                if (Vector3.Distance(CameraClass.Instance.Camera.transform.position, position) > 2f)
                {
                    return;
                }

                isHitPointVisible = true;
            }

            // var bulletInfo = ImpactEffectsRegistry.Instance.BulletInfo;
            //
            // if (bulletInfo is { Player: not null } && bulletInfo.Player.iPlayer.IsYourPlayer)
            // {
            //     var camera = CameraClass.Instance.Camera;
            //     var gravAngle = Vector3.Angle(Vector3.down, normal);
            //     var camAngle = Vector3.Angle(camera.transform.forward, normal);
            //     var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, normal, Vector3.up);
            //
            //     ConsoleScreen.Log(
            //         $"Cam Angle: {camAngle} - {camAngleSigned},  Down Angle: {gravAngle}, Material: {Enum.GetName(typeof(MaterialType), material)}"
            //     );
            // }

            // if (material is MaterialType.Body or MaterialType.BodyArmor or MaterialType.Helmet)
            // {
            //     var bulletInfo = ImpactEffectsRegistry.Instance.BulletInfo;
            //     if (bulletInfo is { Player.IsAI: false })
            //     {
            //         var collider = hitCollider.GetComponent<Collider>();
            //         ConsoleScreen.Log(
            //             $"Body impact. Collider: {collider.name} Rigidbody: {collider.attachedRigidbody.name} Enabled: {collider.enabled}, Kinematic: {collider.attachedRigidbody.isKinematic} Sleeping: {collider.attachedRigidbody.IsSleeping()} DtcColl: {collider.attachedRigidbody.detectCollisions}");
            //
            //         if (collider.attachedRigidbody.isKinematic)
            //         {
            //             collider.attachedRigidbody.isKinematic = false;                        
            //         }
            //         
            //         // collider.attachedRigidbody.AddForce(-10 * normal, ForceMode.Impulse);
            //
            //         var bpCollider = hitCollider.GetComponent<BodyPartCollider>();
            //         var bpCollider2 = (BodyPartCollider)hitCollider;
            //
            //         ConsoleScreen.Log($"Attempt to get BP Collider: {bpCollider} / {bpCollider2}");
            //     }
            // }

            // var bulletInfo = ImpactEffectsRegistry.Instance.BulletInfo;
            // if (bulletInfo is { Player.IsAI: false })
            // {
            //     var collider = hitCollider.GetComponent<Collider>();
            //     var colliderAttachedRigidbody = collider?.attachedRigidbody;
            //     ConsoleScreen.Log(
            //         $"Body impact. Collider: {collider?.name} Rigidbody: {colliderAttachedRigidbody?.name} Enabled: {collider?.enabled}, Kinematic: {colliderAttachedRigidbody?.isKinematic} Sleeping: {colliderAttachedRigidbody?.IsSleeping()} DtcColl: {colliderAttachedRigidbody?.detectCollisions}"
            //     );
            // }

            var context = new EmissionContext(material, hitCollider, position, normal, volume, isKnife, pov);
            ImpactEffectsController.Instance.Emit(__instance, context);
        }
    }
}