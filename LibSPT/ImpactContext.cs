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
    public BallisticCollider Collider;
    public Vector3 Position;
    public Vector3 Normal;
    public float Volume;
    public bool IsKnife;
    public bool IsHitPointVisible;
    public EPointOfView Pov;

    public float DistanceToImpact;

    public CamDir CamOrientation;
    public WorldDir WorldOrientation;
    public float KineticEnergy;

    public Vector3 RandNormal;

    public void EmitEffect(Effects.Effect effect)
    {
        Singleton<Effects>.Instance.AddEffectEmit(
            effect, Position, Normal, Collider, false, Volume,
            IsKnife, true, false, Pov
        );
    }

    public void Update(MaterialType material,
        BallisticCollider collider,
        Vector3 position,
        Vector3 normal,
        float volume,
        bool isKnife,
        bool isHitPointVisible,
        EPointOfView pov)
    {
        Material = material;
        Collider = collider;
        Position = position;
        Normal = normal;
        Volume = volume;
        IsKnife = isKnife;
        IsHitPointVisible = isHitPointVisible;
        Pov = pov;

        DistanceToImpact = Vector3.Distance(CameraClass.Instance.Camera.transform.position, Position);

        // Render things closer than 3 meters but further than 1 of the camera even if the impact location is not directly in the viewport
        if (DistanceToImpact is <= 3f and >= 1f)
        {
            IsHitPointVisible = true;
        }

        UpdateKineticEnergy();
        UpdateImpactOrientation(normal);
    }

    private void UpdateImpactOrientation(Vector3 normal)
    {
        var camera = CameraClass.Instance.Camera;
        var worldAngle = Vector3.Angle(Vector3.down, normal);
        var camAngle = Vector3.Angle(camera.transform.forward, normal);
        var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, normal, Vector3.up);

        CamOrientation = CamDir.None;
        WorldOrientation = WorldDir.None;

        if (camAngle > 107.5)
        {
            CamOrientation |= CamDir.Front;
        }

        if (camAngle < 150)
        {
            CamOrientation |= CamDir.Angled;
        }

        switch (camAngleSigned)
        {
            case > 0:
                CamOrientation |= CamDir.Right;
                break;
            case < 0:
                CamOrientation |= CamDir.Left;
                break;
        }

        if (worldAngle > 45 & worldAngle < 135)
        {
            WorldOrientation |= WorldDir.Horizontal;
        }
        else
        {
            WorldOrientation |= WorldDir.Vertical;

            if (worldAngle >= 135)
            {
                WorldOrientation |= WorldDir.Up;
            }
            else
            {
                WorldOrientation |= WorldDir.Down;
            }
        }
    }

    private void UpdateKineticEnergy()
    {
        KineticEnergy = 500f;

        if (ImpactStatic.BulletInfo != null)
        {
            // KE = 1/2 * m * v^2, but EFT bullet weight is in g instead of kg so we need to divide by 1000 as well
            // NB: We floor the bullet weight for KE calculations as BSG specified that buckshot pellets weigh 0.1g for example. IRL it's 3.5g
            KineticEnergy = Mathf.Max(ImpactStatic.BulletInfo.BulletMassGram, 3.5f) * Mathf.Pow(ImpactStatic.BulletInfo.Speed, 2) / 2000;
        }

        // Add a small amount of randomization to simulate hitting rough surfaces and reduce the jarring uniformity
        RandNormal = (0.75f * Normal + 0.25f * Random.onUnitSphere).normalized;
    }
}