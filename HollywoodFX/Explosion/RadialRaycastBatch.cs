using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class RadialRaycastBatch : IDisposable
{
    public Vector3 Origin;
    public NativeArray<RaycastCommand> Commands;
    public NativeArray<RaycastHit> Results;
    public readonly int RayCount;

    private JobHandle _jobHandle;
    private bool _isDisposed;

    private readonly float _radius;
    private readonly LayerMask _layerMask;
    private readonly int _minCommandsPerJob;
    
    public RadialRaycastBatch(int rayCount, LayerMask layerMask, float radius = 5f, int minCommandsPerJob=128)
    {
        _radius = radius;
        _layerMask = layerMask;
        _minCommandsPerJob = minCommandsPerJob;

        RayCount = Mathf.Max(3, rayCount);
        Commands = new NativeArray<RaycastCommand>(RayCount, Allocator.Persistent);
        Results = new NativeArray<RaycastHit>(RayCount, Allocator.Persistent);
    }
    
    public void ScheduleHemisphere(Vector3 origin, Vector3 normal)
    {
        Origin = origin;
        
        GenerateHemisphereRays(normal);

        _jobHandle = RaycastCommand.ScheduleBatch(Commands, Results, _minCommandsPerJob);
    }

    public void ScheduleSphere(Vector3 origin)
    {
        Origin = origin;
        
        GenerateSphereRays();

        _jobHandle = RaycastCommand.ScheduleBatch(Commands, Results, _minCommandsPerJob);
    }
    
    public void Complete()
    {
        _jobHandle.Complete();
    }

    private void GenerateSphereRays()
    {
        var query = new QueryParameters(_layerMask);
        
        for (var i = 0; i < RayCount; i++)
        {
            var direction = GenerateUniformSphereDirection(i, RayCount);
            Commands[i] = new RaycastCommand(Origin, direction, query, _radius);
        }
    }
    
    private void GenerateHemisphereRays(Vector3 normal)
    {
        var query = new QueryParameters(_layerMask);
        
        for (var i = 0; i < RayCount; i++)
        {
            var direction = GenerateUniformHemisphereDirection(i, RayCount, normal);
            Commands[i] = new RaycastCommand(Origin, direction, query, _radius);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GenerateUniformSphereDirection(int index, int totalCount)
    {
        // Use Fibonacci sphere algorithm for uniform distribution
        var theta = 2f * Mathf.PI * index / (1f + Mathf.Sqrt(5f)); // Golden angle
        var y = 1f - 2f * index / (totalCount - 1f); // Linear spacing in y
        var radius = Mathf.Sqrt(1f - y * y);

        var x = radius * Mathf.Cos(theta);
        var z = radius * Mathf.Sin(theta);

        return new Vector3(x, y, z).normalized;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GenerateUniformHemisphereDirection(int index, int totalCount, Vector3 upDirection)
    {
        // Generate sphere direction and project to hemisphere
        var sphereDir = GenerateUniformSphereDirection(index, totalCount * 2);
            
        // If the direction is pointing down relative to the up direction, flip it
        if (Vector3.Dot(sphereDir, upDirection) < 0)
            sphereDir = -sphereDir;
            
        return sphereDir.normalized;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (Commands.IsCreated)
            Commands.Dispose();

        if (Results.IsCreated)
            Results.Dispose();

        _isDisposed = true;
    }

    ~RadialRaycastBatch()
    {
        if (_isDisposed) return;
        
        Debug.LogWarning("RadialRaycastBatch was not properly disposed. Call Dispose() to avoid memory leaks.");
        Dispose();
    }
}
