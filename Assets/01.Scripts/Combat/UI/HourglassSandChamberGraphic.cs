using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws top or bottom hourglass chamber sand fill as a trapezoid-like shape.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class HourglassSandChamberGraphic : Graphic
{
    public enum ChamberKind
    {
        Top,
        Bottom
    }

    [SerializeField] private ChamberKind _chamberKind = ChamberKind.Top;
    [Range(0f, 1f)] [SerializeField] private float _fillAmount = 0f;

    public void SetFill(float fill)
    {
        _fillAmount = Mathf.Clamp01(fill);
        SetVerticesDirty();
    }

    public void SetTint(Color color)
    {
        this.color = color;
        SetVerticesDirty();
    }

    public void SetChamberKind(ChamberKind kind)
    {
        _chamberKind = kind;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_fillAmount <= 0f)
        {
            return;
        }

        Rect rect = GetPixelAdjustedRect();
        float w = rect.width * 0.5f;
        float h = rect.height * 0.5f;

        float topY = h * 0.78f;
        float neckTopY = h * 0.14f;
        float neckBottomY = -h * 0.14f;
        float bottomY = -h * 0.78f;

        float topW = w * 0.52f;
        float bottomW = w * 0.52f;
        float neckW = w * 0.10f;

        if (_chamberKind == ChamberKind.Top)
        {
            float y = Mathf.Lerp(neckTopY, topY, _fillAmount);
            float t = Mathf.InverseLerp(neckTopY, topY, y);
            float halfW = Mathf.Lerp(neckW, topW, t);
            AddQuad(vh,
                new Vector2(-topW, topY),
                new Vector2(topW, topY),
                new Vector2(halfW, y),
                new Vector2(-halfW, y),
                color);
            return;
        }

        float yBottomFill = Mathf.Lerp(bottomY, neckBottomY, _fillAmount);
        float tb = Mathf.InverseLerp(bottomY, neckBottomY, yBottomFill);
        float halfWB = Mathf.Lerp(bottomW, neckW, tb);

        AddQuad(vh,
            new Vector2(-halfWB, yBottomFill),
            new Vector2(halfWB, yBottomFill),
            new Vector2(bottomW, bottomY),
            new Vector2(-bottomW, bottomY),
            color);
    }

    private static void AddQuad(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color c)
    {
        int start = vh.currentVertCount;
        AddVert(vh, v0, c);
        AddVert(vh, v1, c);
        AddVert(vh, v2, c);
        AddVert(vh, v3, c);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddVert(VertexHelper vh, Vector2 pos, Color c)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = pos;
        v.color = c;
        vh.AddVert(v);
    }
}
