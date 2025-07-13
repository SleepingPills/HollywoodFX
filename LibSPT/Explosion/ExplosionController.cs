using HollywoodFX.Particles;
using Systems.Effects;
using UnityEngine;

namespace HollywoodFX.Explosion;

public class ExplosionController
{
    private readonly EffectBundle _grenadeExp;

    public ExplosionController(Effects eftEffects)
    {
        Plugin.Log.LogInfo("Loading Explosion Prefabs");

        var expMainPrefab = AssetRegistry.AssetBundle.LoadAsset<GameObject>("HFX Boom");
        var mainEffects = EffectBundle.LoadPrefab(eftEffects, expMainPrefab, true);

        _grenadeExp = mainEffects["Test"];
    }

    public void Emit(Vector3 position)
    {
        _grenadeExp.Emit(position, Vector3.up, 1f);
    }
}