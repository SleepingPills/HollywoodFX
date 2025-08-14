using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Explosion;

internal struct Cell
{
    public Vector3 Position;
    public Bounds Bounds;
    public int Count;
    public bool Occupied;
}

/*
 * TODO:
 * Ring will contain dust puffs.
 * Weight the amount of puffs and speed based on the length of the cell vector. Cell vectors > than 90% of the radius will get a speed boost. We want to
 * emit about 1 puff per 1.5 meters of size.
 * We'll emit puffs for each ring cell, and we'll pick randomly from the 3 available puffs (or just rotate through them).
 */

internal class Grid
{
    public readonly List<Vector3Int> Entries;
    public readonly Cell[,,] Cells;

    private readonly Vector3Int[] _buffer;
    
    private readonly Vector3Int _sizeVector;
    private readonly int _radius;
    private readonly int _rounding;

    public Grid(int radius, int rounding)
    {
        // Add a bit of buffer around the desired size
        var size = 2 * radius + 1;
        var cellCount = size * size * size;
        
        Entries = new(cellCount);
        Cells = new Cell[size, size, size];

        _buffer = new Vector3Int[cellCount];
        _rounding = rounding;
        _radius = Mathf.CeilToInt(radius) + 1;
        _sizeVector = new Vector3Int(size, size, size);
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
    
    // TODO simplify this down, get rid of the filter, copy and spans, we'll just work directly on the list and take out the selected items.
    public ReadOnlySpan<Vector3Int> Sample(int count, int minCount=0)
    {
        Span<Vector3Int> sample;

        if (minCount == 0)
        {
            Entries.CopyTo(_buffer);
            sample = _buffer.AsSpan(0, Entries.Count);
        }
        else
        {
            var counter = 0;
            
            for (var i = 0; i < Entries.Count; i++)
            {
                var coords = Entries[i];
                var cell = Cells[coords.x, coords.y, coords.z];
                
                if (cell.Count < minCount)
                    continue;
                
                _buffer[counter] = coords;
                counter++;
            }
            
            sample = _buffer.AsSpan(0, counter);
        }
        
        if (count > sample.Length)
            count = sample.Length;
        
        if (count <= 0)
            return ReadOnlySpan<Vector3Int>.Empty;
        
        // Partial Fisher-Yates: only shuffle the first 'count' positions
        for (var i = 0; i < count; i++)
        {
            var randomIndex = Random.Range(i, sample.Length);
            (sample[i], sample[randomIndex]) = (sample[randomIndex], sample[i]);
        }
        
        // First 'count' elements are now the random sample
        return sample[..count];
    }
}

public class Confinement
{
    private readonly float _radius;
    private readonly RadialRaycastBatch _raycastBatch;

    private readonly Grid _gridLongRange;
    private readonly Grid _gridRing;

    public Confinement(LayerMask layerMask, float radius, float spacing)
    {
        _radius = radius;
        var rayCount = CalculateRayCountForHemisphere(radius, spacing);
        _raycastBatch = new RadialRaycastBatch(rayCount, layerMask, radius);
        var gridSize = 2 * Mathf.CeilToInt(radius) + 1;
        _gridLongRange = new Grid(gridSize, 2);
        _gridRing = new Grid(gridSize, 2);
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

        var threshLongRange = _radius * 0.9f;
        
        for (var i = 0; i < _raycastBatch.RayCount; i++)
        {
            var command = _raycastBatch.Commands[i];
            var result = _raycastBatch.Results[i];

            var coords = result.collider == null ? origin + command.distance * command.direction : result.point;

            var distance = Vector3.Distance(origin, coords);

            if (distance >= threshLongRange)
            {
                _gridLongRange.Add(origin, coords);
            }

            if (Vector3.Angle(Vector3.up, coords - origin) > 60)
            {
                _gridRing.Add(origin, coords);
            }
        }

        // foreach (var coords in _gridLongRange.Entries)
        // {
        //     var cell = _gridLongRange.Cells[coords.x, coords.y, coords.z];
        //     
        //     if (cell.Count < 2)
        //         continue;
        //     
        //     var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
        //     
        //     ConsoleScreen.Log($"Long Range Cell pos: {cell.Position} aabb pos: {cell.Bounds.center} aabb size: {cell.Bounds.size.magnitude} count: {cell.Count}");
        //     DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(countScale, 0, 0));
        // }

        foreach (var coords in _gridRing.Entries)
        {
            var cell = _gridRing.Cells[coords.x, coords.y, coords.z];
            
            if (cell.Count < 2)
                continue;
            
            var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
            
            ConsoleScreen.Log($"Ring Grid Cell pos: {cell.Position} angle: {Vector3.Angle(Vector3.up, cell.Position - origin)} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(0, countScale, 0));
        }

        ConsoleScreen.Log($"Long Range cells: {_raycastBatch.RayCount} rays into {_gridLongRange.Entries.Count} cells");
        ConsoleScreen.Log($"Ring Grid cells: {_raycastBatch.RayCount} rays into {_gridRing.Entries.Count} cells");

        _gridLongRange.Clear();
        _gridRing.Clear();
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