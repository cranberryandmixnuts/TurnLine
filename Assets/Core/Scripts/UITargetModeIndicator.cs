using TMPro;
using UnityEngine;

public class UITargetModeIndicator : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text label;

    public void SetActive(bool on, string message)
    {
        if (root == null) root = gameObject;
        root.SetActive(on);
        if (label != null) label.text = message;
    }

    public void ShowMessage(string message)
    {
        if (root == null) root = gameObject;
        if (!root.activeSelf) root.SetActive(true);
        if (label != null) label.text = message;
    }
}