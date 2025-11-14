using System.Collections.Generic;
using UnityEngine;

namespace HollywoodFX.Gore;

public struct PlayerDamage(BulletKinetics bullet, float frameTime, Collider hitCollider)
{
    public readonly float Impulse = bullet.Impulse;
    public readonly float PenetrationPower = bullet.Info.PenetrationPower;
    public readonly float SizeScale = bullet.SizeScale;
    public readonly float FrameTime = frameTime;
    
    public Vector3 Direction = bullet.Info.Direction;
    public Vector3 HitPoint = bullet.Info.HitPoint;
    public Vector3 HitNormal = bullet.Info.HitNormal;

    public readonly bool Penetrated = bullet.Penetrated;
    
    public readonly Collider HitCollider = hitCollider;
    
}

public class PlayerDamageRegistry : Dictionary<int, PlayerDamage>
{
    public void RegisterDamage(BulletKinetics bullet, Collider collider, Transform root)
    {
        this[root.GetInstanceID()] = new PlayerDamage(bullet, Time.fixedTime, collider);
    }
}