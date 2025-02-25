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
            var rotation = Quaternion.LookRotation(emission.Normal);
            var system = emission.System;
            system.transform.position = emission.Position;
            system.transform.localScale = new Vector3(emission.Scale, emission.Scale, emission.Scale);
            system.transform.rotation = rotation;
            system.Play(true);
        }

        _counter = 0;
    }

    public void Emit(ParticleSystem particleSystem, Vector3 position, Vector3 normal, float scale = 1f)
    {
        if (_counter >= _emissions.Length)
            return;

        _emissions[_counter] = new Emission(particleSystem, position, normal, scale);
        _counter++;
    }

    private struct Emission(ParticleSystem system, Vector3 position, Vector3 normal, float scale)
    {
        public readonly ParticleSystem System = system;
        public readonly Vector3 Position = position;
        public readonly Vector3 Normal = normal;
        public readonly float Scale = scale;
    }
}