using UnityEngine;

namespace HollywoodFX;

public static class CameraExtensions
{
	private static readonly LayerMask LayerMaskNoP = 0b0000_00100_0000_0001_1000_0000_0000;

	public static bool IsPointVisible(this Camera camera, Vector3 point)
	{
		/*
		 * Better logic:
		 * 1. Make a raycast to the destination having the player mask disabled
		 * 2. Calculate the distance to the destination and the distance to the raycast hit
		 * 3. If the raycast hit nothing, or the distance to it's hit is larger than the distance to destination, then the destination must be visible.
		 */
		var origin = camera.transform.position;

		origin += (point - origin).normalized * 0.01f; // Offset origin to avoid self-collision

		// If we don't hit anything solid on the way to the destination, we assume it's visible
		return !Physics.Linecast(origin, point, out _, LayerMaskNoP);
	}
}