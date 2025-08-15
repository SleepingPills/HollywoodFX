using System;
using System.Collections.Generic;
using Systems.Effects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollywoodFX.Explosion;

public class BlastPoolScheduler : MonoBehaviour
{
    public readonly List<BlastPool> Pools = [];
    
    public void Update()
    {
        for (var i = 0; i < Pools.Count; i++)
        {
            Pools[i].Update();
        }
    }
}

public class BlastPool
{
    private  readonly float _lifetime;

    private readonly List<Blast> _pool;
    private readonly Queue<Emission> _active;

    public BlastPool(Effects eftEffects, GameObject prefab, Func<Effects, GameObject, Blast> builder, int copyCount, float lifetime)
    {
        _lifetime = lifetime;
        
        _pool = [];
        _active = new Queue<Emission>();
        
        for (var i = 0; i < copyCount; i++)
        {
            Plugin.Log.LogInfo($"Instantiating Explosion Effects Prefab {prefab.name} installment {i + 1}");
            var rootInstance = Object.Instantiate(prefab);
            var explosion = builder(eftEffects, rootInstance);
            _pool.Add(explosion);
        }
    }

    public void Update()
    {
        while (_active.Count > 0)
        {
            var emission = _active.Peek();
    
            if (Time.time - emission.Timestamp > _lifetime)
            {
                _active.Dequeue();
                _pool.Add(emission.Effect);
            }
            else
            {
                // The next item is still active, we bail out.
                break;
            }
        }
    }

    public void Emit(Vector3 position, Vector3 normal)
    {
        Blast effect;
    
        if (_pool.Count > 0)
        {
            var last = _pool.Count - 1;
            // Pick the last item
            effect = _pool[last];
            // Pop the last item
            _pool.RemoveAt(last);
        }
        else
        {
            // Steal an active emission
            var emission = _active.Dequeue();
            effect = emission.Effect;
        }

        effect.Emit(position, normal);
        
        _active.Enqueue(new Emission(effect, Time.time));
    }

    private struct Emission(Blast effect, float timestamp)
    {
        public readonly Blast Effect = effect;
        public readonly float Timestamp = timestamp;
    }
}