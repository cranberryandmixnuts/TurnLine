using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class AlphaHitButton : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float threshold = 0.2f;

    [SerializeField]
    private bool disableRaycastWhenNoSprite = true;

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
        img.alphaHitTestMinimumThreshold = threshold;
        if (disableRaycastWhenNoSprite && img.sprite == null) img.raycastTarget = false;
    }

    public void SetThreshold(float value)
    {
        threshold = Mathf.Clamp01(value);
        img.alphaHitTestMinimumThreshold = threshold;
    }
}
