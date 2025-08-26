using UnityEngine;

namespace HollywoodFX.Explosion;

public interface IBlast
{
    public void Emit(Vector3 position, Vector3 normal);
}