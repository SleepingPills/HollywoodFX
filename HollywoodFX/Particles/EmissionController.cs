using UnityEngine;

namespace HollywoodFX.Particles;

internal class EmissionController() : MonoBehaviour
{
    public int limit = 100;
    private int _counter;

    public void Awake()
    {
        _counter = 0;
    }

    public void Update()
    {
        _counter = 0;
    }

    public void Emit(ParticleSystem particleSystem, Vector3 position, Vector3 normal, float scale = 1f)
    {
        var rotation = Quaternion.LookRotation(normal);
        Emit(particleSystem, position, rotation, scale);
    }
    
    public void Emit(ParticleSystem particleSystem, Vector3 position, Quaternion rotation, float scale = 1f)
    {
        if (_counter >= limit)
            return;

        particleSystem.transform.position = position;
        particleSystem.transform.localScale = new Vector3(scale, scale, scale);
        particleSystem.transform.rotation = rotation;
        particleSystem.Play(true);
        
        _counter++;
    }
}