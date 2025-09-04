using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MoveArrowManager : MonoBehaviour
{
    [SerializeField] private RectTransform layer;
    [SerializeField] private UIMoveArrow arrowPrefab;

    private readonly Dictionary<int, UIMoveArrow> arrows = new Dictionary<int, UIMoveArrow>();

    public void SetArrow(RegionNode from, RegionNode to)
    {
        int key = from.Id;
        UIMoveArrow a;
        if (!arrows.TryGetValue(key, out a)) a = Instantiate(arrowPrefab, layer);
        arrows[key] = a;
        a.Render(from.Rect, to.Rect, layer, null);
        a.SetAlpha(1f);
    }

    public void RemoveArrow(int regionId)
    {
        UIMoveArrow a;
        if (!arrows.TryGetValue(regionId, out a)) return;
        Destroy(a.gameObject);
        arrows.Remove(regionId);
    }

    public void ClearAll()
    {
        foreach (var kv in arrows) Destroy(kv.Value.gameObject);
        arrows.Clear();
    }

    public void SetArrowRect(int key, RectTransform fromRect, RectTransform toRect)
    {
        UIMoveArrow a;
        if (!arrows.TryGetValue(key, out a)) a = Instantiate(arrowPrefab, layer);
        arrows[key] = a;
        a.Render(fromRect, toRect, layer, null);
        a.SetAlpha(0f);
    }

    public void FadeIn(int key, float duration)
    {
        UIMoveArrow a;
        if (!arrows.TryGetValue(key, out a)) return;
        a.DoFade(1f, duration);
    }

    public void FadeOutAndRemove(int key, float duration)
    {
        UIMoveArrow a;
        if (!arrows.TryGetValue(key, out a)) return;
        a.DoFade(0f, duration).OnComplete(() =>
        {
            if (arrows.ContainsKey(key)) arrows.Remove(key);
            if (a != null) Destroy(a.gameObject);
        });
    }
}