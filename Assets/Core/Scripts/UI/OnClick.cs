using UnityEngine;

public class OnClick : MonoBehaviour
{
    [SerializeField] private string objectName;

    public void Click()
    {
        Debug.Log($"Clicked on {objectName}");
    }
}
