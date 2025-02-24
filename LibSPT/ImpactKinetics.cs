using System.Diagnostics.CodeAnalysis;
using EFT.Ballistics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX;

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
    
    public float Impulse;
    public float Energy;
    public float SizeScale;
    public float ChanceScale;

    public Vector3 RandNormal;
    
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

        // Render things closer than 3 meters but further than 1 of the camera even if the impact location is not directly in the viewport
        if (DistanceToImpact is <= 3f and >= 1f)
        {
            IsHitPointVisible = true;
        }
        
        // Add a small amount of randomization to simulate hitting rough surfaces and reduce the jarring uniformity
        RandNormal = (0.85f * Normal + 0.15f * Random.onUnitSphere).normalized;

        UpdateKinetics();
        CamDir = Orientation.GetCamDir(RandNormal);
        WorldDir = Orientation.GetWorldDir(RandNormal);
    }
    
    private void UpdateKinetics()
    {
        Impulse = 3.6f;
        Energy = 1620f;

        if (ImpactStatic.BulletInfo == null) return;
        
        // KE = 1/2 * m * v^2, but EFT bullet weight is in g instead of kg so we need to divide by 1000 as well
        // NB: We floor the bullet weight for KE calculations as BSG specified that buckshot pellets weigh 0.1g for example. IRL it's 3.5g
        var mass = Mathf.Max(ImpactStatic.BulletInfo.BulletMassGram, 3.5f) / 1000;
        var speed = ImpactStatic.BulletInfo.Speed;
        Impulse = mass * speed;
        Energy = Impulse * speed / 2;
        
        SizeScale = Mathf.Clamp(Mathf.Sqrt(Energy / 1500f), 0.5f, 1.25f);
        // Chance scaling has linear scaling below 1, quadratic above. This ensures visible difference for large calibers without suppressing
        // things too much for smaller ones.
        ChanceScale = SizeScale < 1f ? SizeScale : Mathf.Pow(SizeScale, 2f);
    }
}