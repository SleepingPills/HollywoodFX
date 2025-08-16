using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using Unity.Jobs;
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

public class Grid
{
    public readonly List<Vector3Int> Entries;
    public readonly Cell[,,] Cells;
    
    private readonly Vector3Int _sizeVector;
    private readonly int _radius;
    private readonly float _rounding;

    public Grid(float radius, float rounding)
    {
        // Add a bit of buffer around the desired size
        _radius = Mathf.CeilToInt(radius / rounding);
        var size = 2 * _radius + 1;
        var cellCount = size * size * size;
        
        Entries = new(cellCount);
        Cells = new Cell[size, size, size];

        _rounding = rounding;
        _sizeVector = new Vector3Int(size, size, size);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Vector3 origin, Vector3 position)
    {
        var coords = position - origin;
        // Need to offset the grid coordinates so that we get rid of -ve vector components and everything is in the +ve quadrant of the grid.
        var coordsInt = new Vector3Int(
            Mathf.RoundToInt(coords.x / _rounding) + _radius,
            Mathf.RoundToInt(coords.y / _rounding) + _radius,
            Mathf.RoundToInt(coords.z / _rounding) + _radius
        );

        // Ensure we don't go out of bounds
        coordsInt.Clamp(Vector3Int.zero, _sizeVector);

        var cell = Cells[coordsInt.x, coordsInt.y, coordsInt.z];

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

        Cells[coordsInt.x, coordsInt.y, coordsInt.z] = cell;
    }

    public void Clear()
    {
        // Clear out all the occupied cells
        for (var i = 0; i < Entries.Count; i++)
        {
            var coords = Entries[i];
            Cells[coords.x, coords.y, coords.z] = default;
        }

        // Clear out all the entries
        Entries.Clear();
    }
    
    public List<Vector3Int> Sample(int count)
    {
        if (count > Entries.Count)
            count = Entries.Count;
        
        if (count <= 0)
            return Entries;
        
        // Partial Fisher-Yates: only shuffle the first 'count' positions
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(i, Entries.Count);
            (Entries[i], Entries[randomIndex]) = (Entries[randomIndex], Entries[i]);
        }
        
        // First 'count' elements are now the random sample
        return Entries;
    }
}

public class Confinement
{
    public readonly Grid Up;
    public readonly Grid Ring;

    public RadialRaycastBatch raycastBatch => _raycastBatch;

    private readonly float _radius;
    private readonly RadialRaycastBatch _raycastBatch;
    
    public Confinement(LayerMask layerMask, float radius, float spacing)
    {
        _radius = radius;
        var rayCount = CalculateRayCountForHemisphere(radius, spacing);
        _raycastBatch = new RadialRaycastBatch(rayCount, layerMask, radius);
        Up = new Grid(radius, 3);
        Ring = new Grid(radius, 2);
    }

    public void Schedule(Vector3 origin, Vector3 normal)
    {
        _raycastBatch.ScheduleRaycasts(origin + 0.05f * normal, normal);
    }

    public void Complete()
    {
        _raycastBatch.Complete();

        var origin = _raycastBatch.Origin;
        var threshLongRange = _radius * 0.75f;
        
        for (var i = 0; i < _raycastBatch.RayCount; i++)
        {
            var command = _raycastBatch.Commands[i];
            var result = _raycastBatch.Results[i];

            var coords = result.collider == null ? origin + command.distance * command.direction : result.point;

            var distance = Vector3.Distance(origin, coords);
            var angle = Vector3.Angle(Vector3.up, coords - origin);
            
            if (distance >= threshLongRange && angle <= 75)
            {
                Up.Add(origin, coords);
            }

            if (angle > 80)
            {
                Ring.Add(origin, coords);
            }
        }
    }

    public void Clear()
    {
        Up.Clear();
        Ring.Clear();
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