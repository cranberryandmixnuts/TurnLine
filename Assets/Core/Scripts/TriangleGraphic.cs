using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class TriangleGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float w = r.width;
        float h = r.height;

        Vector2 tip = new Vector2(w, 0f);
        Vector2 baseTop = new Vector2(0f, h * 0.5f);
        Vector2 baseBottom = new Vector2(0f, -h * 0.5f);

        UIVertex a = UIVertex.simpleVert; a.color = color; a.position = tip;
        UIVertex b = UIVertex.simpleVert; b.color = color; b.position = baseTop;
        UIVertex c = UIVertex.simpleVert; c.color = color; c.position = baseBottom;

        vh.AddUIVertexTriangleStream(new List<UIVertex> { a, b, c });
    }

    public void SetDirtyNow()
    {
        SetVerticesDirty();
        SetLayoutDirty();
    }
}