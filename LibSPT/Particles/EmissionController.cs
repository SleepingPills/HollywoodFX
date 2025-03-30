using UnityEngine;

namespace HollywoodFX.Particles;

internal class EmissionController : MonoBehaviour
{
    private Emission[] _emissions;
    private int _counter;

    public void Awake()
    {
        _emissions = new Emission[Plugin.MiscMaxConcurrentParticleSys.Value];
        _counter = 0;
    }

    public void Update()
    {
        for (var i = 0; i < _counter; i++)
        {
            var emission = _emissions[i];
            var system = emission.System;
            system.transform.position = emission.Position;
            system.transform.localScale = new Vector3(emission.Scale, emission.Scale, emission.Scale);
            system.transform.rotation = emission.Rotation;
            system.Play(true);
        }

        _counter = 0;
    }

    public void Emit(ParticleSystem particleSystem, Vector3 position, Vector3 normal, float scale = 1f)
    {
        if (_counter >= _emissions.Length)
            return;

        var rotation = Quaternion.LookRotation(normal);
        _emissions[_counter] = new Emission(particleSystem, position, rotation, scale);
        _counter++;
    }
    
    public void Emit(ParticleSystem particleSystem, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (_counter >= _emissions.Length)
            return;

        _emissions[_counter] = new Emission(particleSystem, position, rotation, scale);
        _counter++;
    }

    private struct Emission(ParticleSystem system, Vector3 position, Quaternion rotation, float scale)
    {
        public readonly ParticleSystem System = system;
        public readonly Vector3 Position = position;
        public readonly Quaternion Rotation = rotation;
        public readonly float Scale = scale;
    }
}