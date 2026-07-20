using HollywoodFX.Particles;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class Blast(EffectBundle[] effectsUp, EffectBundle[] effectsAngled, float scale = 1f) : IBlast
{
    public void Emit(Vector3 position, Vector3 normal)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < effectsUp.Length; i++)
        {
            effectsUp[i].Emit(position, Vector3.up, scale);
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < effectsAngled.Length; i++)
        {
            effectsAngled[i].Emit(position, normal, scale);
        }
    }
}