using HollywoodFX.Particles;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class Explosion(EffectBundle[] effectsUp, EffectBundle[] effectsAngled, float scale = 1f)
{
    public void Emit(Vector3 position, Vector3 normal)
    {
        for (var i = 0; i < effectsUp.Length; i++)
            effectsUp[i].Emit(position, Vector3.up, scale);

        for (var i = 0; i < effectsAngled.Length; i++)
            effectsAngled[i].Emit(position, normal, scale);
    }
}