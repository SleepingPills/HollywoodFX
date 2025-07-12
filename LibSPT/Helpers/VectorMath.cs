using UnityEngine;

namespace HollywoodFX.Helpers;

public static class VectorMath
{
    public static Vector3 IncreaseAngleBetweenVectors(Vector3 v1, Vector3 v2, float additionalAngleDegrees)
    {
        // Find the axis of rotation (perpendicular to both vectors)
        var rotationAxis = Vector3.Cross(v1, v2).normalized;
    
        // If vectors are parallel, choose an arbitrary perpendicular axis
        if (rotationAxis.magnitude < 0.001f)
        {
            rotationAxis = Vector3.Cross(v1, Vector3.up).normalized;
            if (rotationAxis.magnitude < 0.001f)
                rotationAxis = Vector3.Cross(v1, Vector3.right).normalized;
        }
    
        // Rotate v2 away from v1
        var rotation = Quaternion.AngleAxis(additionalAngleDegrees, rotationAxis);
        return rotation * v2;
    }
}