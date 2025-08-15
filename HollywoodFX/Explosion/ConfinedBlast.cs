using System.Collections;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class ConfinedBlast(Effects eftEffects, float radius, float granularity)
{
    private readonly Confinement _confinement = new(GClass3449.HitMask, radius, granularity);

    public void Emit(Vector3 position, Vector3 normal)
    {
        eftEffects.StartCoroutine(Detonate(position, normal));
    }
    
    private IEnumerator Detonate(Vector3 origin, Vector3 normal)
    {
        _confinement.Schedule(origin, normal);
        yield return null;
        _confinement.Complete();
        
        foreach (var coords in _confinement.Up.Entries)
        {
            var cell = _confinement.Up.Cells[coords.x, coords.y, coords.z];
            var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
            
            ConsoleScreen.Log($"Long Range Cell pos: {cell.Position} aabb pos: {cell.Bounds.center} aabb size: {cell.Bounds.size.magnitude} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(countScale, 0, 0));
        }

        foreach (var coords in _confinement.Ring.Entries)
        {
            var cell = _confinement.Ring.Cells[coords.x, coords.y, coords.z];
            var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
            
            ConsoleScreen.Log($"Ring Grid Cell pos: {cell.Position} angle: {Vector3.Angle(Vector3.up, cell.Position - origin)} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(0, countScale, 0));
        }

        ConsoleScreen.Log($"Long Range cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Up.Entries.Count} cells");
        ConsoleScreen.Log($"Ring Grid cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Ring.Entries.Count} cells");
        
        _confinement.Clear();
    }
}