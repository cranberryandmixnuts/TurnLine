using System;
using UnityEngine;
using UnityEngine.UI;

public class UITouchBlocker : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button skipButton;

    private Action onSkip;

    private void Awake()
    {
        skipButton.onClick.AddListener(OnSkip);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        skipButton.onClick.RemoveListener(OnSkip);
    }

    public void Show(Action onSkipCallback)
    {
        onSkip = onSkipCallback;
        root.SetActive(true);
        skipButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        skipButton.gameObject.SetActive(false);
    }

    private void OnSkip()
    {
        onSkip();
    }
}