# Snippets
## Debug code in Effects.Emit
```csharp
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


if (bulletInfo is { Player.IsAI: false })
{
    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    sphere.transform.position = context.Position;
    // sphere.transform.localScale = Vector3.one * 0.2f;
    sphere.transform.parent = attachedRigidbody.transform;
    
    // foreach (var collider in sphere.GetComponentsInChildren<Collider>())
    // {
    //     Object.DestroyImmediate(collider);
    // }
}

var gm = cur.gameObject;
var player = gm.GetComponent<Player>();

if (player != null)
{
    var playerTraverse = Traverse.Create(player);

    var preAllocatedRenderersList = playerTraverse.Field("_preAllocatedRenderersList").GetValue<List<GStruct56>>();
    var playerBody = playerTraverse.Field("_playerBody").GetValue<PlayerBody>();

    preAllocatedRenderersList.Clear();
    playerBody.GetBodyRenderersNonAlloc(preAllocatedRenderersList);
    Singleton<Effects>.Instance.EffectsCommutator.PlayerMeshesHit(preAllocatedRenderersList, context.Position, -context.Normal);
}

```