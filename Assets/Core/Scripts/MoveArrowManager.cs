using System.Collections.Generic;
using UnityEngine;

public class MoveArrowManager : MonoBehaviour
{
    [SerializeField] private RectTransform layer;
    [SerializeField] private UIMoveArrow arrowPrefab;

    private readonly Dictionary<int, UIMoveArrow> arrows = new Dictionary<int, UIMoveArrow>();
    private Canvas _canvas;
    private Camera _uiCam;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
    }

    public void SetArrow(RegionNode from, RegionNode to)
    {
        if (layer == null || arrowPrefab == null || from == null || to == null) return;
        if (!arrows.TryGetValue(from.Id, out var arrow))
        {
            arrow = Instantiate(arrowPrefab, layer);
            arrows[from.Id] = arrow;
        }
        arrow.Render(from.Rect, to.Rect, layer, _uiCam);
    }

    public void RemoveArrow(int sourceRegionId)
    {
        if (!arrows.TryGetValue(sourceRegionId, out var arrow)) return;
        Destroy(arrow.gameObject);
        arrows.Remove(sourceRegionId);
    }

    public void ClearAll()
    {
        foreach (var kv in arrows) if (kv.Value != null) Destroy(kv.Value.gameObject);
        arrows.Clear();
    }
}