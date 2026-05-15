using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws an hourglass-like frame and glass body for the central combat UI.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class HourglassShapeGraphic : Graphic
{
    [SerializeField] private Color _glassColor = new Color(0.12f, 0.16f, 0.2f, 0.6f);
    [SerializeField] private Color _outlineColor = new Color(0.85f, 0.9f, 1f, 0.85f);
    [SerializeField] private float _outlineThickness = 4f;
    [SerializeField] private float _glowThickness = 10f;

    private Color _actorGlowColor = new Color(0.24f, 0.58f, 1f, 0.25f);

    public void SetActorTone(Color actorColor)
    {
        _actorGlowColor = new Color(actorColor.r, actorColor.g, actorColor.b, 0.32f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float w = rect.width * 0.5f;
        float h = rect.height * 0.5f;

        float outerW = w * 0.63f;
        float outerH = h * 0.9f;
        float neckW = w * 0.17f;
        float neckCoreW = w * 0.08f;
        float neckY = h * 0.22f;

        Vector2[] p = new Vector2[]
        {
            new Vector2(-outerW, outerH),
            new Vector2( outerW, outerH),
            new Vector2( neckW, neckY),
            new Vector2( neckCoreW, 0f),
            new Vector2( neckW, -neckY),
            new Vector2( outerW, -outerH),
            new Vector2(-outerW, -outerH),
            new Vector2(-neckW, -neckY),
            new Vector2(-neckCoreW, 0f),
            new Vector2(-neckW, neckY)
        };

        AddFilledPolygon(vh, p, _glassColor);
        AddOutline(vh, p, _actorGlowColor, _glowThickness);
        AddOutline(vh, p, _outlineColor, _outlineThickness);
    }

    private static void AddFilledPolygon(VertexHelper vh, Vector2[] points, Color color)
    {
        int start = vh.currentVertCount;

        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i++)
        {
            center += points[i];
        }

        center /= points.Length;

        AddVert(vh, center, color);
        for (int i = 0; i < points.Length; i++)
        {
            AddVert(vh, points[i], color);
        }

        for (int i = 0; i < points.Length; i++)
        {
            int a = start;
            int b = start + 1 + i;
            int c = start + 1 + ((i + 1) % points.Length);
            vh.AddTriangle(a, b, c);
        }
    }

    private static void AddOutline(VertexHelper vh, Vector2[] points, Color color, float thickness)
    {
        if (thickness <= 0f)
        {
            return;
        }

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            AddThickLine(vh, a, b, thickness, color);
        }
    }

    private static void AddThickLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

        Vector2 v0 = a - normal;
        Vector2 v1 = a + normal;
        Vector2 v2 = b + normal;
        Vector2 v3 = b - normal;

        int start = vh.currentVertCount;
        AddVert(vh, v0, color);
        AddVert(vh, v1, color);
        AddVert(vh, v2, color);
        AddVert(vh, v3, color);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddVert(VertexHelper vh, Vector2 pos, Color color)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = pos;
        v.color = color;
        vh.AddVert(v);
    }
}
