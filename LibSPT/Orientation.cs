using System;
using System.Runtime.CompilerServices;
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

public static class Orientation
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CamDir GetCamDir(Vector3 normal)
    {
        var camera = CameraClass.Instance.Camera;
        var camAngle = Vector3.Angle(camera.transform.forward, normal);
        var camAngleSigned = Vector3.SignedAngle(camera.transform.forward, normal, Vector3.up);

        var camDir = CamDir.None;
        if (camAngle > 107.5)
        {
            camDir |= CamDir.Front;
        }

        if (camAngle < 160)
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