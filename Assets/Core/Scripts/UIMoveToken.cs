using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMoveToken : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image triangle;
    [SerializeField] private TMP_Text amountLabel;
    [SerializeField] private float holdThreshold = 0.35f;

    private RectTransform startRect;
    private RectTransform endRect;
    private MoveArrowManager arrowManager;
    private int arrowKey;
    private float fadeDuration;
    private bool holding;
    private float holdTime;
    private Tween moveTween;

    public RectTransform Rect
    {
        get { return transform as RectTransform; }
    }

    public void Initialize(RegionNode.OwnerKind owner, int amount, RectTransform start, RectTransform end, MoveArrowManager mgr, int endRegionId, float arrowFade)
    {
        startRect = start;
        endRect = end;
        arrowManager = mgr;
        fadeDuration = arrowFade;
        amountLabel.text = amount.ToString();

        Color c = owner == RegionNode.OwnerKind.Player ? GameController.Instance.PlayerColor : GameController.Instance.EnemyColor;
        c.a = 130f / 255f;
        triangle.color = c;

        Vector2 a = WorldToCanvas(start.position);
        Vector2 b = WorldToCanvas(end.position);
        Vector2 d = (b - a).normalized;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90;
        triangle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

        Rect.anchoredPosition = a;
        Rect.localRotation = Quaternion.identity;
        arrowKey = 100000 + endRegionId;
    }

    public Tween AnimateToMid(float duration)
    {
        Vector2 a = WorldToCanvas(startRect.position);
        Vector2 b = WorldToCanvas(endRect.position);
        Vector2 mid = Vector2.Lerp(a, b, 0.5f);
        moveTween?.Kill();
        moveTween = Rect.DOAnchorPos(mid, duration).SetEase(Ease.InOutSine);
        return moveTween;
    }

    public Tween AnimateToTarget(float duration)
    {
        Vector2 b = WorldToCanvas(endRect.position);
        moveTween?.Kill();
        moveTween = Rect.DOAnchorPos(b, duration).SetEase(Ease.InOutSine).OnComplete(() => Destroy(gameObject));
        return moveTween;
    }

    public void InstantToMid()
    {
        Vector2 a = WorldToCanvas(startRect.position);
        Vector2 b = WorldToCanvas(endRect.position);
        Rect.anchoredPosition = Vector2.Lerp(a, b, 0.5f);
    }

    public void InstantToTarget()
    {
        moveTween?.Kill();
        Vector2 b = WorldToCanvas(endRect.position);
        Rect.anchoredPosition = b;
        Destroy(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        holding = true;
        holdTime = 0f;
        StartCoroutine(HoldLoop());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        holding = false;
        arrowManager.FadeOutAndRemove(arrowKey, fadeDuration);
    }

    private IEnumerator HoldLoop()
    {
        while (holding)
        {
            holdTime = holdTime + Time.deltaTime;
            if (holdTime >= holdThreshold)
                arrowManager.SetArrowRect(arrowKey, Rect, endRect);
            if (holdTime >= holdThreshold)
            {
                arrowManager.FadeIn(arrowKey, fadeDuration);
                break;
            }
            yield return null;
        }
    }

    private Vector2 WorldToCanvas(Vector3 world)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, sp, cam, out var lp);
        return lp;
    }
}