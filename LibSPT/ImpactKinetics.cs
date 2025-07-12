using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX;

public class BulletKinetics
{
    private const float SizeNormFactor = 2000f;
    
    public float Impulse = 3.6f;
    public float Energy = 1620f;
    public float SizeScale = 1f;
    public float ChanceScale = 1f;
    public EftBulletClass Info;
    public Transform HitColliderRoot;
    
    public void Update(EftBulletClass bulletInfo)
    {
        Impulse = 3.6f;
        Energy = 1620f;
        HitColliderRoot = null;

        if (bulletInfo == null) return;
        
        Info = bulletInfo;
        
        // KE = 1/2 * m * v^2, but EFT bullet weight is in g instead of kg so we need to divide by 1000 as well
        // NB: We floor the bullet weight for KE calculations as BSG specified that buckshot pellets weigh 0.1g for example. IRL it's 3.5g
        var mass = Mathf.Max(bulletInfo.BulletMassGram, 3.5f) / 1000;
        var speed = bulletInfo.VelocityMagnitude;

        // Only apply this to the local player
        if (bulletInfo.Player.iPlayer.IsYourPlayer)
            speed *= Plugin.KineticsScaling.Value;
        
        Impulse = mass * speed;
        Energy = Impulse * speed / 2;
        
        SizeScale = Mathf.Clamp(Mathf.Sqrt(Energy / SizeNormFactor), 0.6f, 1.2f);
        // Chance scaling has linear scaling below 1, quadratic above. This ensures visible difference for large calibers without suppressing
        // things too much for smaller ones.
        ChanceScale = SizeScale < 1f ? SizeScale : Mathf.Pow(SizeScale, 2f);

        if (bulletInfo.HitCollider == null) return;
        
        HitColliderRoot = bulletInfo.HitCollider.transform.root;
    }
}

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class ImpactKinetics
{
    public MaterialType Material;
    public Vector3 Position;
    public Vector3 Normal;
    public bool IsHitPointVisible;

    public float DistanceToImpact;

    public CamDir CamDir;
    public WorldDir WorldDir;
    
    public Vector3 RandNormal;
    public Vector3 RandNormalOffset;

    public readonly BulletKinetics Bullet = new();

    public void Update(MaterialType material,
        Vector3 position,
        Vector3 normal,
        bool isHitPointVisible)
    {
        Material = material;
        Position = position;
        Normal = normal;
        IsHitPointVisible = isHitPointVisible;
    
        DistanceToImpact = Vector3.Distance(CameraClass.Instance.Camera.transform.position, Position);

        // Render things closer than 3 meters, so that impacts near the player add to the ambience
        if (DistanceToImpact <= 3f)
        {
            IsHitPointVisible = true;
        }
        
        // Add a small amount of randomization to simulate hitting rough surfaces and reduce the jarring uniformity
        RandNormal = (0.85f * Normal + 0.15f * Random.onUnitSphere).normalized;

        CamDir = Orientation.GetCamDir(RandNormal);
        WorldDir = Orientation.GetWorldDir(RandNormal);
        
        RandNormalOffset = Orientation.GetNormOffset(RandNormal, CamDir);
    }
}