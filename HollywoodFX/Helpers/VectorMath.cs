using System.Runtime.CompilerServices;
using UnityEngine;

namespace HollywoodFX.Helpers;

public static class VectorMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 AddRandomRotation(Vector3 unitVector, float maxAngleDegrees)
    {
        // Convert to radians and generate random offset in tangent space
        var maxAngleRad = maxAngleDegrees * Mathf.Deg2Rad;
        var randomOffset = Random.insideUnitCircle * maxAngleRad;
            
        // Find perpendicular vectors - optimized branch
        var tangent = Mathf.Abs(unitVector.y) < 0.9f 
            ? new Vector3(-unitVector.z, 0f, unitVector.x)  // Cross with up when safe
            : new Vector3(0f, unitVector.z, -unitVector.y); // Cross with right when parallel to up
            
        tangent.Normalize();
            
        // Bitangent = original × tangent (no need to normalize since both inputs are unit)
        var bitangent = new Vector3(
            unitVector.y * tangent.z - unitVector.z * tangent.y,
            unitVector.z * tangent.x - unitVector.x * tangent.z,
            unitVector.x * tangent.y - unitVector.y * tangent.x
        );
            
        // Apply offset and normalize
        return (unitVector + tangent * randomOffset.x + bitangent * randomOffset.y).normalized;
    }
}