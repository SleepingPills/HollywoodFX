using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using Systems.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodFX.Gore;

internal class DetachOnDisable : MonoBehaviour
{
    private void OnDisable()
    {
        transform.SetParent(Singleton<Effects>.Instance.transform);
        enabled = true;
        ConsoleScreen.Log($"Detached {gameObject.name} from {transform.parent}");
    }
}

internal class RigidbodyEffects : MonoBehaviour
{
    private float _lifetime = 2f;

    private List<ParticleSystem> _pool;
    private Queue<Emission> _active;
    private Effects _eftEffects;

    public void Setup(Effects eftEffects, GameObject prefab, int copyCount, float lifetime)
    {
        _lifetime = lifetime;
        _eftEffects = eftEffects;
        _pool = [];
        _active = new Queue<Emission>();

        for (var i = 0; i < copyCount; i++)
        {
            Plugin.Log.LogInfo($"Instantiating Effects Prefab {prefab.name} installment {i + 1}");

            var rootInstance = Instantiate(prefab);
            foreach (var child in rootInstance.transform.GetChildren())
            {
                if (!child.gameObject.TryGetComponent<ParticleSystem>(out var particleSystem)) continue;

                child.parent = eftEffects.transform;
                Singleton<LitMaterialRegistry>.Instance.Register(particleSystem, false);
                _pool.Add(particleSystem);
                Plugin.Log.LogInfo($"Adding Effect {child.name}");
            }
        }

        foreach (var effect in _pool)
        {
            effect.gameObject.AddComponent<DetachOnDisable>();

            var particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            
            if (particleSystems == null)
                continue;
            
            foreach (var particleSystem in particleSystems)
            {
                if (!particleSystem.collision.enabled)
                    continue;
                
                particleSystem.gameObject.AddComponent<BloodSquirtCollisionHandler>();                
            }
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
                emission.Effect.transform.SetParent(_eftEffects.transform);
                _pool.Add(emission.Effect);
            }
            else
            {
                // The next item is still active, we bail out.
                break;
            }
        }
    }

    public void Emit(Rigidbody rigidbody, Vector3 position, Vector3 normal, float scale = 1f)
    {
        ParticleSystem effect;

        if (_pool.Count > 0)
        {
            // Pick a random effect from the pool
            var pick = _pool.Count == 1 ? 0 : Random.Range(0, _pool.Count);
            effect = _pool[pick];
            var last = _pool.Count - 1;
            // Swap the last item to the one we just removed
            _pool[pick] = _pool[last];
            // Pop the last item in the list
            _pool.RemoveAt(last);
        }
        else
        {
            // Steal an active emission
            var emission = _active.Dequeue();
            effect = emission.Effect;
        }

        effect.transform.position = position;
        effect.transform.localScale = new Vector3(scale, scale, scale);
        effect.transform.rotation = Quaternion.LookRotation(normal);
        effect.transform.SetParent(rigidbody.transform);
        effect.Play(true);

        _active.Enqueue(new Emission(effect, Time.time));
    }

    private struct Emission(ParticleSystem effect, float timestamp)
    {
        public readonly ParticleSystem Effect = effect;
        public readonly float Timestamp = timestamp;
    }
}

public class BloodSquirtCollisionHandler : MonoBehaviour
{
    private Effects _effects;
    private ParticleSystem _particleSystem;
    private List<ParticleCollisionEvent> _collisionEvents;

    public void Start()
    {
        _effects = Singleton<Effects>.Instance;
        _particleSystem = GetComponent<ParticleSystem>();
        _collisionEvents = [];
        Plugin.Log.LogInfo($"Starting collision handler for {_particleSystem.name}");
    }

    public void OnParticleCollision(GameObject other)
    {
        if (other == null)
            return;

        var numEvents = _particleSystem.GetCollisionEvents(other, _collisionEvents);

        for (var i = 0; i < numEvents; i++)
        {
            var hitPos = _collisionEvents[i].intersection;
            var hitNormal = _collisionEvents[i].normal;

            _effects.EmitBleeding(hitPos, hitNormal);
        }
    }
}
