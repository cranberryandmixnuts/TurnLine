using UnityEngine;
using UnityEngine.UI;

public class UIEndTurnButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        GameController.Instance.EndTurn();
    }
}