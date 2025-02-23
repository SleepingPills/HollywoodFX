using Comfort.Common;
using UnityEngine;

namespace HollywoodFX.Particles;

internal class SystemBundle(string name, ParticleSystem[] particleSystems)
{
    public void EmitRandom(Vector3 position, Vector3 normal, float scale)
    {
        var pick = particleSystems[Random.Range(0, particleSystems.Length)];
        Singleton<EmissionController>.Instance.Emit(pick, position, normal, scale);
    }
}