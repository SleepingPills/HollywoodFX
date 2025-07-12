using System;
using System.Runtime.CompilerServices;
using HollywoodFX.Helpers;
using UnityEngine;

namespace HollywoodFX;

[Flags]
public enum CamDir
{
    None = 0,
    Front = 1 << 0,
    Angled = 1 << 1,
    Left = 1 << 6,
    Right = 1 << 7,
    All = ~0
}

[Flags]
public enum WorldDir
{
    None = 0,
    Horizontal = 1 << 1,
    Vertical = 1 << 2,
    Up = 1 << 3,
    Down = 1 << 4,
    All = ~0
}

public static class OrientationEnumExtensions
{
    public static bool IsSet(this CamDir self, CamDir flag)
    {
        return (self & flag) == flag;
    }
    
    public static bool IsSet(this WorldDir self, WorldDir flag)
    {
        return (self & flag) == flag;
    }
}

public static class Orientation
{
    private const float FrontAngle = 107.5f;
    private const float FrontAngleInv = 180f - FrontAngle;
    
    private const float AngledAngle = 160.0f;
    private const float AngledAngleInv = 180f - AngledAngle;

    private const float AdjustmentAngle = 30f;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetNormOffset(Vector3 normal, CamDir camDir)
    {
        if (!camDir.IsSet(CamDir.Front) || !camDir.IsSet(CamDir.Angled)) return normal;
        
        var camera = CameraClass.Instance.Camera;
        var backward = -camera.transform.forward;
        var angle = Vector3.Angle(backward, normal);
        var adjustment = Mathf.Min(FrontAngleInv - angle, AdjustmentAngle);
            
        return adjustment <= 1e-3f ? normal : VectorMath.IncreaseAngleBetweenVectors(backward, normal, adjustment);

    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CamDir GetCamDir(Vector3 normal)
    {
        var camera = CameraClass.Instance.Camera;
        var camAngle = Vector3.Angle(camera.transform.forward, normal);
        var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, normal, Vector3.up);

        var camDir = CamDir.None;
        if (camAngle > FrontAngle)
        {
            camDir |= CamDir.Front;
        }

        if (camAngle < AngledAngle)
        {
            camDir |= CamDir.Angled;
        }

        switch (camAngleSigned)
        {
            case > 0:
                camDir |= CamDir.Right;
                break;
            case < 0:
                camDir |= CamDir.Left;
                break;
        }

        return camDir;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WorldDir GetWorldDir(Vector3 normal)
    {
        var worldAngle = Vector3.Angle(Vector3.down, normal);

        var worldDir = WorldDir.None;
        if (worldAngle > 45 & worldAngle < 135)
        {
            worldDir |= WorldDir.Horizontal;
        }
        else
        {
            worldDir |= WorldDir.Vertical;

            if (worldAngle >= 135)
            {
                worldDir |= WorldDir.Up;
            }
            else
            {
                worldDir |= WorldDir.Down;
            }
        }

        return worldDir;
    }
}