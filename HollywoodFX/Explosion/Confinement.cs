using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

internal struct Cell
{
    public Vector3 Position;
    public Bounds Bounds;
    public int Count;
    public bool Occupied;
}

internal class Grid
{
    private readonly int _radius;
    private readonly Vector3Int _sizeVector;
    public readonly List<Vector3Int> Entries;
    public readonly Cell[,,] Cells;
    private readonly int _rounding;

    public Grid(int radius, int rounding)
    {
        // Add a bit of buffer around the desired size
        _rounding = rounding;
        _radius = Mathf.CeilToInt(radius) + 1;
        var size = 2 * radius + 1;
        _sizeVector = new Vector3Int(size, size, size);

        Entries = new(size * size * size);
        Cells = new Cell[size, size, size];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Vector3 origin, Vector3 position)
    {
        var coords = position - origin;
        // Need to offset the grid coordinates so that we get rid of -ve vector components and everything is in the +ve quadrant of the grid.
        var coordsInt = new Vector3Int(
            Mathf.RoundToInt(coords.x / _rounding) * _rounding + _radius,
            Mathf.RoundToInt(coords.y / _rounding) * _rounding + _radius,
            Mathf.RoundToInt(coords.z / _rounding) * _rounding + _radius
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
    
    public void SampleInPlace(int count)
    {
        if (count > Entries.Count) count = Entries.Count;
        if (count <= 0) return;
        
        // Partial Fisher-Yates: only shuffle the first 'count' positions
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(i, Entries.Count);
            (Entries[i], Entries[randomIndex]) = (Entries[randomIndex], Entries[i]);
        }
        
        // First 'count' elements are now the random sample
    }
}

public class Confinement
{
    private readonly RadialRaycastBatch _raycastBatch;

    private readonly Grid _gridFine;
    private readonly Grid _gridCoarse;

    public Confinement(LayerMask layerMask, float radius, float spacing)
    {
        var rayCount = CalculateRayCountForHemisphere(radius, spacing);
        _raycastBatch = new RadialRaycastBatch(rayCount, layerMask, radius);
        var gridSize = 2 * Mathf.CeilToInt(radius) + 1;
        _gridFine = new Grid(gridSize, 2);
        _gridCoarse = new Grid(gridSize, 4);
    }

    public void Calculate(Effects eftEffects, Vector3 origin, Vector3 normal)
    {
        eftEffects.StartCoroutine(CalculateRaycastBatch(origin, normal));
    }

    private IEnumerator CalculateRaycastBatch(Vector3 origin, Vector3 normal)
    {
        var jobHandle = _raycastBatch.ScheduleRaycasts(origin, normal);
        yield return null;
        jobHandle.Complete();

        for (var i = 0; i < _raycastBatch.RayCount; i++)
        {
            var command = _raycastBatch.Commands[i];
            var result = _raycastBatch.Results[i];

            if (result.collider != null)
            {
                _gridFine.Add(origin, result.point);
            }
            else
            {
                _gridCoarse.Add(origin, origin + command.distance * command.direction);
            }
        }

        foreach (var coords in _gridFine.Entries)
        {
            var cell = _gridFine.Cells[coords.x, coords.y, coords.z];
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: Color.red);
        }

        foreach (var coords in _gridCoarse.Entries)
        {
            var cell = _gridCoarse.Cells[coords.x, coords.y, coords.z];
            ConsoleScreen.Log($"Cell pos: {cell.Position} aabb pos: {cell.Bounds.center} aabb size: {cell.Bounds.size.magnitude} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: Color.green);
        }

        ConsoleScreen.Log($"Fine cells: {_raycastBatch.RayCount} rays into {_gridFine.Entries.Count} cells");
        ConsoleScreen.Log($"Coarse cells: {_raycastBatch.RayCount} rays into {_gridCoarse.Entries.Count} cells");

        _gridFine.Clear();
        _gridCoarse.Clear();
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