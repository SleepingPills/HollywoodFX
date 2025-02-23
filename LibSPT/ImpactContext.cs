using System;
using System.Diagnostics.CodeAnalysis;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX;

[Flags]
internal enum CamDir
{
    None = 0,
    Front = 1 << 0,
    Angled = 1 << 1,
    Left = 1 << 6,
    Right = 1 << 7,
    All = ~0
}

[Flags]
internal enum WorldDir
{
    None = 0,
    Horizontal = 1 << 1,
    Vertical = 1 << 2,
    Up = 1 << 3,
    Down = 1 << 4,
    All = ~0
}

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
internal class ImpactContext
{
    public MaterialType Material;
    public Vector3 Position;
    public Vector3 Normal;
    public bool IsHitPointVisible;

    public float DistanceToImpact;

    public CamDir CamDir;
    public WorldDir WorldDir;
    public float KineticEnergy;

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

        UpdateKineticEnergy();
        UpdateImpactOrientation(normal);
    }

    private void UpdateImpactOrientation(Vector3 normal)
    {
        var camera = CameraClass.Instance.Camera;
        var worldAngle = Vector3.Angle(Vector3.down, normal);
        var camAngle = Vector3.Angle(camera.transform.forward, normal);
        var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, normal, Vector3.up);

        CamDir = CamDir.None;
        WorldDir = WorldDir.None;

        if (camAngle > 107.5)
        {
            CamDir |= CamDir.Front;
        }

        if (camAngle < 150)
        {
            CamDir |= CamDir.Angled;
        }

        switch (camAngleSigned)
        {
            case > 0:
                CamDir |= CamDir.Right;
                break;
            case < 0:
                CamDir |= CamDir.Left;
                break;
        }

        if (worldAngle > 45 & worldAngle < 135)
        {
            WorldDir |= WorldDir.Horizontal;
        }
        else
        {
            WorldDir |= WorldDir.Vertical;

            if (worldAngle >= 135)
            {
                WorldDir |= WorldDir.Up;
            }
            else
            {
                WorldDir |= WorldDir.Down;
            }
        }
    }

    private void UpdateKineticEnergy()
    {
        KineticEnergy = 1500f;

        if (ImpactStatic.BulletInfo != null)
        {
            // KE = 1/2 * m * v^2, but EFT bullet weight is in g instead of kg so we need to divide by 1000 as well
            // NB: We floor the bullet weight for KE calculations as BSG specified that buckshot pellets weigh 0.1g for example. IRL it's 3.5g
            KineticEnergy = Mathf.Max(ImpactStatic.BulletInfo.BulletMassGram, 3.5f) * Mathf.Pow(ImpactStatic.BulletInfo.Speed, 2) / 2000;
        }
    }
}