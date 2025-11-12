using UnityEngine;

namespace HollywoodFX.Helpers;

public class DebugUI
{
    public static Rect Label(Vector2 position, string text, bool centered = true)
    {
        var content = new GUIContent(text);
        var size = GUI.skin.label.CalcSize(content);
        var upperLeft = centered ? position - size / 2f : position;
        var rect = new Rect(upperLeft, size);
        GUI.Label(rect, content);
        return rect;
    }
    
    public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, float thickness)
    {
        var vector = lineEnd - lineStart;
        var pivot = /* 180/PI */ Mathf.Rad2Deg * Mathf.Atan(vector.y / vector.x);
        if (vector.x < 0f)
            pivot += 180f;

        thickness = Mathf.Max(thickness, 1f);
        var yOffset = (int)Mathf.Ceil(thickness / 2);

        GUIUtility.RotateAroundPivot(pivot, lineStart);
        GUI.DrawTexture(new Rect(lineStart.x, lineStart.y - yOffset, vector.magnitude, thickness), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-pivot, lineStart);
    }
}