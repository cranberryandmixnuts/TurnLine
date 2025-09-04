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
    [SerializeField] private Color insufficientColor = new Color(1f, 0.25f, 0.25f, 1f);

    private RegionNode current;
    private Color upgradeCostDefaultColor;
    private bool sliderBound;

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

        if (sliderBound) moveSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnRegionSelected(RegionNode node)
    {
        current = node;
        UpdateSliderBounds();
        if (!sliderBound)
        {
            moveSlider.onValueChanged.AddListener(OnSliderChanged);
            sliderBound = true;
        }
        ResetSliderToOneUI();
        controller.MoveAmount = 1;
        UpdateUpgradeCost();
        UpdateButtonsState();
        RefreshLabels();
        if (controller.IsPickingTarget) root.SetActive(false);
        else root.SetActive(true);
    }

    private void UpdateSliderBounds()
    {
        moveSlider.minValue = 1;
        int max = Mathf.Max(1, current.TroopCount);
        moveSlider.maxValue = max;
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

        bool canUpgrade = affordable && !current.IsDefending;
        upgradeButton.interactable = canUpgrade;
    }

    private void UpdateButtonsState()
    {
        bool hasTroops = current.TroopCount > 0;
        bool defending = current.IsDefending;

        moveButton.interactable = hasTroops && !defending;

        defenseButton.gameObject.SetActive(!defending);
        defenseExitButton.gameObject.SetActive(defending);
    }

    private void OnMove()
    {
        controller.MoveAmount = Mathf.RoundToInt(moveSlider.value);
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
        ResetSliderToOneUI();
        root.SetActive(false);
    }

    private void OnSliderChanged(float v)
    {
        int iv = Mathf.RoundToInt(v);
        controller.MoveAmount = iv;
        amountLabel.text = iv.ToString();
        RefreshLabels();
        UpdateUpgradeCost();
        UpdateButtonsState();
    }

    private void RefreshLabels()
    {
        string status = current.IsDefending ? "방어중" : $"턴당 +{controller.GetProductionPerTurn(current.Level)}";
        string picking = controller.IsPickingTarget ? " [타깃 선택 모드]" : "";
        string orderText = "";
        GameController.Order order;
        if (controller.TryGetOrder(current.Id, out order))
        {
            if (order.Kind == GameController.OrderKind.Move) orderText = $"명령예약: 이동 → {order.TargetRegionId} ({order.Amount})";
            else if (order.Kind == GameController.OrderKind.Wait) orderText = "명령예약: 대기";
            else if (order.Kind == GameController.OrderKind.DefenseEnter) orderText = "명령예약: 방어태세";
            else if (order.Kind == GameController.OrderKind.DefenseExit) orderText = "명령예약: 방어해제";
            else if (order.Kind == GameController.OrderKind.Upgrade) orderText = "명령예약: 업그레이드";
        }
        infoLabel.text = $"지역 {current.Id} | {status}{picking}\n{orderText}";
    }

    private void ClosePanel()
    {
        ResetSliderToOneUI();
        root.SetActive(false);
    }

    private void ResetSliderToOneUI()
    {
        moveSlider.SetValueWithoutNotify(1);
        amountLabel.text = "1";
    }
}