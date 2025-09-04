using TMPro;
using UnityEngine;

public class UITargetModeIndicator : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text label;

    public void SetActive(bool on, string message)
    {
        root.SetActive(on);
        label.text = message;
    }

    public void ShowMessage(string message)
    {
        root.SetActive(true);
        label.text = message;
    }
}