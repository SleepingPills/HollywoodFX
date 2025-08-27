using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Explosion;

public struct Cell
{
    public Vector3 Position;
    public Bounds Bounds;
    public int Count;
    public bool Occupied;
}

public struct Sample
{
    public Vector3 Direction;
    public float Magnitude;
}

public class Grid
{
    public readonly List<Vector3Int> Entries;
    public Vector3 Origin;

    private readonly Cell[,,] _cells;
    private readonly List<Sample> _samples;
    private readonly Vector3Int _sizeVector;
    private readonly int _radius;
    private readonly float _rounding;

    public Grid(float radius, float rounding)
    {
        // Add a bit of buffer around the desired size
        _radius = Mathf.CeilToInt(radius / rounding);
        var size = 2 * _radius + 1;
        var cellCount = size * size * size;

        _samples = new(cellCount);
        Entries = new(cellCount);
        _cells = new Cell[size, size, size];

        _rounding = rounding;
        _sizeVector = new Vector3Int(size, size, size);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Vector3 position)
    {
        var coords = position - Origin;
        // Need to offset the grid coordinates so that we get rid of -ve vector components and everything is in the +ve quadrant of the grid.
        var coordsInt = new Vector3Int(
            Mathf.RoundToInt(coords.x / _rounding) + _radius,
            Mathf.RoundToInt(coords.y / _rounding) + _radius,
            Mathf.RoundToInt(coords.z / _rounding) + _radius
        );

        // Ensure we don't go out of bounds
        coordsInt.Clamp(Vector3Int.zero, _sizeVector);

        ref var cell = ref _cells[coordsInt.x, coordsInt.y, coordsInt.z];

        if (cell.Occupied)
        {
            cell.Position = Vector3.Lerp(cell.Position, position, 1f / (cell.Count + 1));
            cell.Bounds.Encapsulate(position);
            cell.Count++;
        }
        else
        {
            cell.Position = position;
            cell.Bounds = new Bounds(position, Vector3.zero);
            cell.Count = 1;
            cell.Occupied = true;
            Entries.Add(coordsInt);
        }
    }

    public void Clear()
    {
        // Clear out all the occupied cells
        for (var i = 0; i < Entries.Count; i++)
        {
            var coords = Entries[i];
            _cells[coords.x, coords.y, coords.z] = default;
        }

        // Clear out all the entries
        Entries.Clear();
        _samples.Clear();
    }
    
    public List<Sample> Pick(int count=0)
    {
        _samples.Clear();

        if (count >= Entries.Count || count <= 0)
        {
            count = Entries.Count;
        }
        else
        {
            // Partial Fisher-Yates: only shuffle the first 'count' positions
            for (var i = 0; i < count; i++)
            {
                var randomIndex = Random.Range(i, Entries.Count);
                (Entries[i], Entries[randomIndex]) = (Entries[randomIndex], Entries[i]);
            }            
        }
        
        for (var i = 0; i < count; i++)
        {
            var coords = Entries[i];
            ref var cell = ref _cells[coords.x, coords.y, coords.z];

            var ray = cell.Position - Origin;
            _samples.Add(new Sample
            {
                Direction = ray.normalized,
                Magnitude = ray.magnitude,
            });
        }

        // First 'count' elements are now the random sample
        return _samples;
    }
}

public class Confinement(LayerMask layerMask, float radius, float spacing)
{
    public readonly Grid Up = new(radius, 3);
    public readonly Grid Ring = new(radius, 2);
    public readonly Grid Confined = new(radius, 1.5f);

    public Vector3 Normal = Vector3.up;
    public float Proximity = 1f;
    
    private readonly RadialRaycastBatch _proximityBatch = new(CalculateRayCountForSphere(2f, 0.7f), layerMask, radius);
    private readonly RadialRaycastBatch _raycastBatch = new(CalculateRayCountForHemisphere(radius, spacing), layerMask, radius);

    public void ScheduleProximity(Vector3 origin)
    {
        _proximityBatch.ScheduleSphere(origin);
    }

    public void CompleteProximity()
    {
        _proximityBatch.Complete();
        
        var origin = _proximityBatch.Origin;
        var candidate = Vector3.zero;

        for (var i = 0; i < _proximityBatch.RayCount; i++)
        {
            var command = _proximityBatch.Commands[i];
            var result = _proximityBatch.Results[i];

            var offset = result.collider == null ? command.distance * command.direction : result.point - origin;
            
            candidate += offset;
        }
        
        candidate /= _proximityBatch.RayCount;
        
        Proximity = candidate.magnitude;
        
        // Force the normal upright unless it's at a very horizontal angle and there's nothing nearby blocking it
        if (Vector3.Angle(Vector3.up, candidate) <= 60f || Proximity <= 0.5f)
        {
            Normal = Vector3.up;
        }
        else
        {
            Normal = candidate;
            Normal.Normalize();
        }
        
        // ConsoleScreen.Log($"Norm calc ray count: {_proximityBatch.RayCount} offset mag: {Proximity}");
        // DebugGizmos.Line(origin, origin + Normal, expiretime: 30f, color: new Color(0f, 1f, 0f));
    }
    
    public void ScheduleMain(Vector3 origin)
    {
        _raycastBatch.ScheduleHemisphere(origin + 0.05f * Normal, Normal);
    }

    public void CompleteMain()
    {
        _raycastBatch.Complete();

        var origin = _raycastBatch.Origin;
        var threshold = radius * 0.75f;
        
        Confined.Origin = Ring.Origin = Up.Origin = origin;

        for (var i = 0; i < _raycastBatch.RayCount; i++)
        {
            var command = _raycastBatch.Commands[i];
            var result = _raycastBatch.Results[i];

            var coords = result.collider == null ? origin + command.distance * command.direction : result.point;

            var distance = Vector3.Distance(origin, coords);
            var angle = Vector3.Angle(Normal, coords - origin);

            if (distance >= threshold)
            {
                if (angle <= 60)
                {
                    Up.Add(coords);
                }
                if (angle is >= 60 and <= 80)
                {
                    Confined.Add(coords);
                }
            }
            
            if (angle > 80)
            {
                Ring.Add(coords);
            }
        }
    }

    public void Clear()
    {
        Up.Clear();
        Ring.Clear();
        Confined.Clear();
    }

    private static int CalculateRayCountForHemisphere(float radius, float spacing)
    {
        return CalculateRayCountForSphere(radius, spacing) / 2;
    }

    private static int CalculateRayCountForSphere(float radius, float spacing)
    {
        var sphereArea = 4f * Mathf.PI * radius * radius;
        var areaPerRay = spacing * spacing;
        return Mathf.CeilToInt(sphereArea / areaPerRay);
    }
}