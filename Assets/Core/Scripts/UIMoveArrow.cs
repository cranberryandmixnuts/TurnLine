using UnityEngine;
using UnityEngine.UI;

public class UIMoveArrow : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform shaft;
    [SerializeField] private Image shaftImage;
    [SerializeField] private RectTransform head;
    [SerializeField] private TriangleGraphic headGraphic;
    [SerializeField] private float shaftThickness = 8f;
    [SerializeField] private float headLength = 44f;
    [SerializeField] private float headHeight = 36f;
    [SerializeField] private float startPadding = 18f;
    [SerializeField] private float endPadding = 26f;

    public void Render(RectTransform from, RectTransform to, RectTransform container, Camera uiCam)
    {
        if (root == null) root = transform as RectTransform;
        if (from == null || to == null || container == null) return;

        Vector2 aScreen = RectTransformUtility.WorldToScreenPoint(uiCam, from.position);
        Vector2 bScreen = RectTransformUtility.WorldToScreenPoint(uiCam, to.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, aScreen, uiCam, out var aLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, bScreen, uiCam, out var bLocal);

        Vector2 ab = bLocal - aLocal;
        float len = ab.magnitude;
        if (len < 0.001f) return;

        Vector2 dir = ab / len;
        Vector2 start = aLocal + dir * startPadding;
        Vector2 end = bLocal - dir * endPadding;
        Vector2 seg = end - start;
        float segLen = seg.magnitude;
        if (segLen < 0.001f) return;

        float angle = Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg;

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 0.5f);
        root.anchoredPosition = start;
        root.localRotation = Quaternion.Euler(0f, 0f, angle);

        float shaftLen = Mathf.Max(0f, segLen - headLength);

        if (shaft != null)
        {
            shaft.anchorMin = new Vector2(0f, 0.5f);
            shaft.anchorMax = new Vector2(0f, 0.5f);
            shaft.pivot = new Vector2(0f, 0.5f);
            shaft.anchoredPosition = Vector2.zero;
            shaft.sizeDelta = new Vector2(shaftLen, shaftThickness);
        }

        if (shaftImage != null) shaftImage.raycastTarget = false;

        if (head != null)
        {
            head.anchorMin = new Vector2(0f, 0.5f);
            head.anchorMax = new Vector2(0f, 0.5f);
            head.pivot = new Vector2(0f, 0.5f);
            head.anchoredPosition = new Vector2(shaftLen, 0f);
            head.sizeDelta = new Vector2(headLength, headHeight <= 0f ? shaftThickness * 3f : headHeight);
        }

        if (headGraphic != null) headGraphic.SetDirtyNow();
    }
}