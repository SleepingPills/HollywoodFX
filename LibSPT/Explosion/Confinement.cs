using System.Collections;
using System.Collections.Generic;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

internal struct Cell
{
    public Vector3 Position;
    public float Radius;
    public int Count;
}

public class Confinement
{
    private readonly RadialRaycastBatch _raycastBatch;
    private readonly Dictionary<Vector3Int, Cell> _cells1;
    private readonly Dictionary<Vector3Int, Cell> _cells2;

    public Confinement(LayerMask layerMask, float radius, float spacing)
    {
        var rayCount = CalculateRayCountForHemisphere(radius, spacing);
        _raycastBatch = new RadialRaycastBatch(rayCount, layerMask, radius);
        _cells1 = new Dictionary<Vector3Int, Cell>(rayCount);
        _cells2 = new Dictionary<Vector3Int, Cell>(rayCount);
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

            // TODO: Put the rays which didn't hit anything into a coarser grid
            var coords = result.collider != null ? result.point : command.from + command.distance * command.direction;
            
            if (result.collider != null)
                continue;
            
            var coordsRounded = new Vector3Int(
                Mathf.RoundToInt(coords.x / 2f) * 2,
                Mathf.RoundToInt(coords.y / 2f) * 2,
                Mathf.RoundToInt(coords.z / 2f) * 2
            );
            var distance = Vector3.Distance(coordsRounded, coords);

            if (!_cells1.TryGetValue(coordsRounded, out var cell))
            {
                cell = new Cell { Position = coords, Radius = distance, Count = 1 };
            }
            else
            {
                cell.Position = Vector3.Lerp(cell.Position, coords, 1f / (cell.Count + 1));

                if (distance > cell.Radius)
                    cell.Radius = distance;

                cell.Count++;
            }

            _cells1[coordsRounded] = cell;
        }

        foreach (var kv in _cells1)
        {
            DebugGizmos.Line(origin, kv.Value.Position, expiretime: 30f);
        }

        ConsoleScreen.Log($"Collapse: {_raycastBatch.RayCount} rays into {_cells1.Count} cells");

        _cells1.Clear();
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