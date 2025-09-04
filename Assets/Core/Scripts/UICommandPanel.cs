using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICommandPanel : MonoBehaviour
{
    [SerializeField] private GameController controller;
    [SerializeField] private GameObject root;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button waitButton;
    [SerializeField] private Button defenseButton;
    [SerializeField] private Button defenseExitButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider moveSlider;
    [SerializeField] private TMP_Text infoLabel;
    [SerializeField] private TMP_Text amountLabel;

    private RegionNode current;

    private void Start()
    {
        if (controller == null) controller = GameController.Instance;
        if (root == null) root = gameObject;
        root.SetActive(false);

        if (controller != null) controller.RegionSelected += OnRegionSelected;

        if (moveButton != null) moveButton.onClick.AddListener(OnMove);
        if (waitButton != null) waitButton.onClick.AddListener(OnWait);
        if (defenseButton != null) defenseButton.onClick.AddListener(OnDefenseEnter);
        if (defenseExitButton != null) defenseExitButton.onClick.AddListener(OnDefenseExit);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgrade);
        if (closeButton != null) closeButton.onClick.AddListener(OnClose);

        if (moveSlider != null)
        {
            moveSlider.wholeNumbers = true;
            moveSlider.onValueChanged.AddListener(OnSliderChanged);
        }
        RefreshLabels();
    }

    private void OnDestroy()
    {
        if (controller != null) controller.RegionSelected -= OnRegionSelected;

        if (moveButton != null) moveButton.onClick.RemoveListener(OnMove);
        if (waitButton != null) waitButton.onClick.RemoveListener(OnWait);
        if (defenseButton != null) defenseButton.onClick.RemoveListener(OnDefenseEnter);
        if (defenseExitButton != null) defenseExitButton.onClick.RemoveListener(OnDefenseExit);
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgrade);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);

        if (moveSlider != null) moveSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnRegionSelected(RegionNode node)
    {
        current = node;
        UpdateDefenseButtons();
        UpdateSliderBounds();
        RefreshLabels();
        if (controller != null && controller.IsPickingTarget) root.SetActive(false);
        else root.SetActive(true);
    }

    private void UpdateDefenseButtons()
    {
        if (defenseButton != null) defenseButton.gameObject.SetActive(current != null && !current.IsDefending);
        if (defenseExitButton != null) defenseExitButton.gameObject.SetActive(current != null && current.IsDefending);
    }

    private void UpdateSliderBounds()
    {
        if (moveSlider == null || current == null) return;
        moveSlider.minValue = 0;
        moveSlider.maxValue = Mathf.Max(0, current.TroopCount);
        int preset = Mathf.Clamp(controller.MoveAmount, 0, current.TroopCount);
        moveSlider.SetValueWithoutNotify(preset);
        OnSliderChanged(preset);
    }

    private void OnMove()
    {
        if (controller == null) return;
        controller.StartMove();
        ClosePanel();
    }

    private void OnWait()
    {
        if (controller == null) return;
        controller.IssueOrder(GameController.OrderKind.Wait);
        ClosePanel();
    }

    private void OnDefenseEnter()
    {
        if (controller == null) return;
        controller.IssueOrder(GameController.OrderKind.DefenseEnter);
        ClosePanel();
    }

    private void OnDefenseExit()
    {
        if (controller == null) return;
        controller.IssueOrder(GameController.OrderKind.DefenseExit);
        ClosePanel();
    }

    private void OnUpgrade()
    {
        if (controller == null) return;
        controller.IssueOrder(GameController.OrderKind.Upgrade);
        ClosePanel();
    }

    private void OnClose()
    {
        if (controller == null) return;
        controller.CancelTargetPicking();
        root.SetActive(false);
    }

    private void OnSliderChanged(float v)
    {
        if (controller == null) return;
        controller.MoveAmount = Mathf.RoundToInt(v);
        if (amountLabel != null) amountLabel.text = controller.MoveAmount.ToString();
    }

    private void RefreshLabels()
    {
        if (infoLabel == null) return;
        if (current == null)
        {
            infoLabel.text = "";
            return;
        }

        string state = current.IsDefending ? "방어중" : "일반";
        string picking = controller != null && controller.IsPickingTarget ? " [타깃 선택 모드]" : "";
        string orderText = "";
        if (controller != null && controller.TryGetOrder(current.Id, out var order))
        {
            if (order.Kind == GameController.OrderKind.Move) orderText = $"명령: 이동 → {order.TargetRegionId} ({order.Amount})";
            else if (order.Kind == GameController.OrderKind.Wait) orderText = "명령: 대기";
            else if (order.Kind == GameController.OrderKind.DefenseEnter) orderText = "명령: 방어태세";
            else if (order.Kind == GameController.OrderKind.DefenseExit) orderText = "명령: 방어해제";
            else if (order.Kind == GameController.OrderKind.Upgrade) orderText = "명령: 업그레이드";
        }
        infoLabel.text = $"지역 {current.Id} | 병력 {current.TroopCount} | {state}{picking}\n{orderText}";
    }

    private void ClosePanel()
    {
        root.SetActive(false);
    }
}