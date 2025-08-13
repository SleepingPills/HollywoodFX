using System.Collections;
using System.Collections.Generic;
using EFT;
using UnityEngine;

namespace HollywoodFX.Helpers;

/*
 * Credit to SAIN / Solarint
 */
public static class DebugGizmos
{
    static DebugGizmos()
    {
        GameWorld.OnDispose += Dispose;
    }

    private static void Dispose()
    {
        ClearGizmos();
    }

    private static void ClearGizmos()
    {
        if (DrawnGizmos.Count <= 0) return;
        
        foreach (var t in DrawnGizmos)
        {
            if (t != null)
                Object.Destroy(t);
        }

        DrawnGizmos.Clear();
    }
    
    private static readonly List<GameObject> DrawnGizmos = new();

    public static GameObject Sphere(Vector3 position, float size, Color color, bool temporary = false, float expiretime = 1f)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.GetComponent<Renderer>().material.color = color;
        sphere.GetComponent<Collider>().enabled = false;
        sphere.transform.position = new Vector3(position.x, position.y, position.z);

        sphere.transform.localScale = new Vector3(size, size, size);

        AddGizmo(sphere, expiretime);

        return sphere;
    }

    public static GameObject Box(Vector3 position, float length, float height, Color color, float expiretime = -1f)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.GetComponent<Renderer>().material.color = color;
        box.GetComponent<Collider>().enabled = false;
        box.transform.position = position;
        box.transform.localScale = new Vector3(length * 2, height * 2, length * 2);
        AddGizmo(box, expiretime);

        return box;
    }

    private static void AddGizmo(GameObject obj, float expireTime)
    {
        if (expireTime > 0)
        {
            TempCoroutine.DestroyAfterDelay(obj, expireTime);
        }
        else
        {
            DrawnGizmos.Add(obj);
        }
    }

    public static GameObject Sphere(Vector3 position, float size, float expiretime = 1f)
    {
        return Sphere(position, size, RandomColor, expiretime > 0, expiretime);
    }

    public static GameObject Sphere(Vector3 position, float expiretime = 1f)
    {
        return Sphere(position, 0.25f, RandomColor, expiretime > 0, expiretime);
    }

    public static GameObject Line(Vector3 startPoint, Vector3 endPoint, Color color, float lineWidth = 0.05f, float expiretime = 1f, bool taperLine = false)
    {
        var lineObject = new GameObject();
        var lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Modify the color and width of the line
        lineRenderer.material.color = color;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = taperLine ? lineWidth / 4f : lineWidth;

        // Modify the start and end points of the line
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        AddGizmo(lineObject, expiretime);

        return lineObject;
    }

    public static void UpdatePositionLine(Vector3 a, Vector3 b, GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }

        var lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer?.SetPosition(0, a);
        lineRenderer?.SetPosition(1, b);
    }

    public static GameObject Line(Vector3 startPoint, Vector3 endPoint, float lineWidth = 0.05f, float expiretime = 1f, bool taperLine = false)
    {
        return Line(startPoint, endPoint, RandomColor, lineWidth, expiretime, taperLine);
    }

    public static GameObject Ray(Vector3 startPoint, Vector3 direction, Color color, float length = 0.35f, float lineWidth = 0.05f,
        bool temporary = false, float expiretime = 1f, bool taperLine = false)
    {
        var endPoint = startPoint + direction.normalized * length;
        return Line(startPoint, endPoint, color, lineWidth, expiretime, taperLine);
    }

    public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float raisePoints, params Vector3[] points)
    {
        return DrawLinesBetweenPoints(lineSize, -1, raisePoints, points);
    }

    public static List<GameObject> DrawLinesBetweenPoints(params Vector3[] points)
    {
        return DrawLinesBetweenPoints(0.1f, -1, 0f, points);
    }

    private const float SphereMulti = 1.5f;
    private const float MaxSphere = 10f;
    private const float MinSphere = 0.15f;
    private const float MinMag = 0.01f;

    private static void DrawLinesToPoint(List<GameObject> list, Vector3 origin, Color color, float lineSize, float expireTime, float raisePoints,
        params Vector3[] points)
    {
        foreach (var t in points)
        {
            var pointB = t;
            pointB.y += raisePoints;

            if (origin == t) continue;
            
            var direction = origin - pointB;
            var magnitude = direction.magnitude;
            
            if (!(magnitude > MinMag)) continue;
            
            var ray = Ray(pointB, direction, color, magnitude, lineSize, expireTime > 0, expireTime);
            list.Add(ray);
        }
    }

    public static List<GameObject> DrawLinesToPoint(Vector3 origin, Color color, float lineSize, float expireTime, float raisePoints,
        params Vector3[] points)
    {
        var list = new List<GameObject>();
        DrawLinesToPoint(list, origin, color, lineSize, expireTime, raisePoints, points);
        return list;
    }

    public static void DrawSpheresAtPoints(List<GameObject> list, Color color, float size, float expireTime, float raisePoints,
        params Vector3[] points)
    {
        foreach (var t in points)
        {
            var pointA = t;
            pointA.y += raisePoints;
            var sphere = Sphere(pointA, size, color, expireTime > 0, expireTime);
            list.Add(sphere);
        }
    }

    public static List<GameObject> DrawSpheresAtPoints(Color color, float size, float expireTime, float raisePoints, params Vector3[] points)
    {
        var list = new List<GameObject>();
        DrawSpheresAtPoints(list, color, size, expireTime, raisePoints, points);
        return list;
    }

    public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float expireTime, float raisePoints, params Vector3[] points)
    {
        var list = new List<GameObject>();
        foreach (var t in points)
        {
            var pointA = t;
            pointA.y += raisePoints;

            var color = RandomColor;

            var sphereSize = Mathf.Clamp(lineSize * SphereMulti, MinSphere, MaxSphere);
            var sphere = Sphere(pointA, sphereSize, color, expireTime > 0, expireTime);
            list.Add(sphere);

            DrawLinesToPoint(list, pointA, color, lineSize, expireTime, raisePoints, points);
        }

        return list;
    }

    public static List<GameObject> DrawLinesBetweenPoints(float lineSize, float expireTime, float raisePoints, Color color, params Vector3[] points)
    {
        var list = new List<GameObject>();
        
        foreach (var t in points)
        {
            var pointA = t;
            pointA.y += raisePoints;

            var sphereSize = Mathf.Clamp(lineSize * SphereMulti, MinSphere, MaxSphere);
            var sphere = Sphere(pointA, sphereSize, color, expireTime > 0, expireTime);
            list.Add(sphere);

            DrawLinesToPoint(list, pointA, color, lineSize, expireTime, raisePoints, points);
        }

        return list;
    }

    private static float RandomFloat => Random.Range(0.2f, 1f);
    private static Color RandomColor => new Color(RandomFloat, RandomFloat, RandomFloat);

    internal class TempCoroutine : MonoBehaviour
    {
        internal class TempCoroutineRunner : MonoBehaviour;

        public static void DestroyAfterDelay(GameObject obj, float delay)
        {
            if (obj == null) return;

            var runner = new GameObject("TempCoroutineRunner").AddComponent<TempCoroutineRunner>();
            runner?.StartCoroutine(RunDestroyAfterDelay(obj, delay));
        }

        private static IEnumerator RunDestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj == null) yield break;

            var runner = obj.GetComponentInParent<TempCoroutineRunner>();

            if (runner != null)
            {
                Destroy(runner.gameObject);
            }

            Destroy(obj);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}