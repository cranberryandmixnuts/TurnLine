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
    [SerializeField] private TMP_Text upgradeCostLabel;
    [SerializeField] private Color insufficientColor = new(1f, 0.25f, 0.25f, 1f);

    private RegionNode current;
    private Color upgradeCostDefaultColor;

    private void Start()
    {
        upgradeCostDefaultColor = upgradeCostLabel.color;

        root.SetActive(false);

        controller.RegionSelected += OnRegionSelected;

        moveButton.onClick.AddListener(OnMove);
        waitButton.onClick.AddListener(OnWait);
        defenseButton.onClick.AddListener(OnDefenseEnter);
        defenseExitButton.onClick.AddListener(OnDefenseExit);
        upgradeButton.onClick.AddListener(OnUpgrade);
        closeButton.onClick.AddListener(OnClose);

        moveSlider.wholeNumbers = true;
        moveSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        controller.RegionSelected -= OnRegionSelected;

        moveButton.onClick.RemoveListener(OnMove);
        waitButton.onClick.RemoveListener(OnWait);
        defenseButton.onClick.RemoveListener(OnDefenseEnter);
        defenseExitButton.onClick.RemoveListener(OnDefenseExit);
        upgradeButton.onClick.RemoveListener(OnUpgrade);
        closeButton.onClick.RemoveListener(OnClose);

        moveSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnRegionSelected(RegionNode node)
    {
        current = node;
        UpdateSliderBounds();
        UpdateUpgradeCost();
        RefreshLabels();
        if (controller.IsPickingTarget) root.SetActive(false);
        else root.SetActive(true);
    }

    private void UpdateSliderBounds()
    {
        moveSlider.minValue = 0;
        moveSlider.maxValue = Mathf.Max(0, current.TroopCount);
        int preset = Mathf.Clamp(controller.MoveAmount, 0, current.TroopCount);
        moveSlider.SetValueWithoutNotify(preset);
        OnSliderChanged(preset);
    }

    private void UpdateUpgradeCost()
    {
        bool isMax = current.Level >= controller.MaxLevel;
        if (isMax)
        {
            upgradeCostLabel.text = "업그레이드 비용: MAX";
            upgradeCostLabel.color = upgradeCostDefaultColor;
            upgradeButton.interactable = false;
            return;
        }

        int cost = controller.GetUpgradeCost(current.Level);
        upgradeCostLabel.text = $"업그레이드 비용: {cost}";
        bool affordable = current.TroopCount >= cost;
        upgradeCostLabel.color = affordable ? upgradeCostDefaultColor : insufficientColor;
        upgradeButton.interactable = affordable;
    }

    private void OnMove()
    {
        controller.StartMove();
        ClosePanel();
    }

    private void OnWait()
    {
        controller.IssueOrder(GameController.OrderKind.Wait);
        ClosePanel();
    }

    private void OnDefenseEnter()
    {
        controller.IssueOrder(GameController.OrderKind.DefenseEnter);
        ClosePanel();
    }

    private void OnDefenseExit()
    {
        controller.IssueOrder(GameController.OrderKind.DefenseExit);
        ClosePanel();
    }

    private void OnUpgrade()
    {
        controller.IssueOrder(GameController.OrderKind.Upgrade);
        ClosePanel();
    }

    private void OnClose()
    {
        controller.CancelTargetPicking();
        root.SetActive(false);
    }

    private void OnSliderChanged(float v)
    {
        controller.MoveAmount = Mathf.RoundToInt(v);
        amountLabel.text = controller.MoveAmount.ToString();
        RefreshLabels();
        UpdateUpgradeCost();
    }

    private void RefreshLabels()
    {
        string lvStr = current.Level >= controller.MaxLevel ? "MAX" : current.Level.ToString();
        int prod = controller.GetProductionPerTurn(current.Level);
        string status = current.IsDefending ? "방어중" : $"턴당 +{prod}";
        string picking = controller.IsPickingTarget ? " [타깃 선택 모드]" : "";
        string orderText = "";
        if (controller.TryGetOrder(current.Id, out var order))
        {
            if (order.Kind == GameController.OrderKind.Move) orderText = $"명령예약: 이동 → {order.TargetRegionId} ({order.Amount})";
            else if (order.Kind == GameController.OrderKind.Wait) orderText = "명령예약: 대기";
            else if (order.Kind == GameController.OrderKind.DefenseEnter) orderText = "명령예약: 방어태세";
            else if (order.Kind == GameController.OrderKind.DefenseExit) orderText = "명령예약: 방어해제";
            else if (order.Kind == GameController.OrderKind.Upgrade) orderText = "명령예약: 업그레이드";
        }
        infoLabel.text = $"지역 {current.Id} | Lv{lvStr} | 병력 {current.TroopCount} | {status}{picking}\n{orderText}";
    }

    private void ClosePanel()
    {
        root.SetActive(false);
    }
}