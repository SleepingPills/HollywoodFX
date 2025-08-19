using System.Collections;
using System.Runtime.CompilerServices;
using EFT.UI;
using HollywoodFX.Helpers;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

/*
 * TODO:
 * Ring will contain dust puffs.
 * Weight the amount of puffs and speed based on the length of the cell vector. Cell vectors > than 90% of the radius will get a speed boost. We want to
 * emit about 1 puff per 1.5 meters of size.
 * We'll emit puffs for each ring cell, and we'll pick randomly from the 3 available puffs (or just rotate through them).
 * 
 */

public class ConfinedBlast(Effects eftEffects, float radius, float granularity)
{
    private readonly Confinement _confinement = new(GClass3449.HitMask, radius, granularity);

    public void Emit(Vector3 position, Vector3 normal)
    {
        eftEffects.StartCoroutine(Detonate(position, normal));
    }
    
    private IEnumerator Detonate(Vector3 origin, Vector3 normal)
    {
        _confinement.Schedule(origin, Vector3.up);
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
            
            if (cell.Count < 4)
                continue;
            
            var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
            
            ConsoleScreen.Log($"Ring Grid Cell pos: {cell.Position} angle: {Vector3.Angle(Vector3.up, cell.Position - origin)} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(0, countScale, 0));
        }
        
        // TODO: pass in the shortfall in emitting long range effects (trails, sparks) and emit some through this if possible.
        ConfinedEffects(origin);
        
        ConsoleScreen.Log($"Long Range cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Up.Entries.Count} cells");
        ConsoleScreen.Log($"Ring Grid cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Ring.Entries.Count} cells");
        ConsoleScreen.Log($"Confined Grid cells: {_confinement.raycastBatch.RayCount} rays into {_confinement.Confined.Entries.Count} cells");
        
        _confinement.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConfinedEffects(Vector3 origin)
    {
        // If we get 25 or fewer entries with count >= 4, sample 15 of them and blast through some dust.
        var countBefore = _confinement.Confined.Entries.Count;
        _confinement.Confined.FilterByCount(3);
        ConsoleScreen.Log($"Trimmed cells from {countBefore} -> {_confinement.Confined.Entries.Count}");
        
        var entriesCount = _confinement.Confined.Entries.Count;
        if (entriesCount > 25) return;
        
        var pickCount = _confinement.Confined.Sample(Random.Range(15, 25));

        for (var i = 0; i < pickCount; i++)
        {
            var coords = _confinement.Confined.Entries[i];
            var cell = _confinement.Confined.Cells[coords.x, coords.y, coords.z];
                
            var countScale = Mathf.InverseLerp(1f, 10f, cell.Count);
            
            ConsoleScreen.Log($"Confined Range Cell pos: {cell.Position} aabb pos: {cell.Bounds.center} aabb size: {cell.Bounds.size.magnitude} count: {cell.Count}");
            DebugGizmos.Line(origin, cell.Position, expiretime: 30f, color: new Color(0, 0, countScale));
        }
    }
}