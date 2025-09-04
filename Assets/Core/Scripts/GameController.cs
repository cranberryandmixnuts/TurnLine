using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameController : MonoBehaviour
{
    public static GameController Instance
    {
        get; private set;
    }

    public enum OrderKind
    {
        None,
        Wait,
        DefenseEnter,
        DefenseExit,
        Upgrade,
        Move
    }

    public sealed class Order
    {
        public OrderKind Kind;
        public int TargetRegionId;
        public int Amount;
    }

    public event Action<RegionNode> RegionSelected = _ => { };

    [Header("UI")]
    [SerializeField] private UITargetModeIndicator targetIndicator;
    [SerializeField] private MoveArrowManager arrowManager;

    [Header("Ownership Colors")]
    [SerializeField] private Color playerColor = new(0.20f, 0.60f, 1.00f, 1f);
    [SerializeField] private Color playerLightColor = new(0.72f, 0.86f, 1.00f, 1f);
    [SerializeField] private Color enemyColor = new(1.00f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color enemyLightColor = new(1.00f, 0.80f, 0.80f, 1f);
    [SerializeField] private Color neutralColor = new(0.74f, 0.74f, 0.74f, 1f);
    [SerializeField] private int troopsForMaxIntensity = 50;

    [Header("Level Settings")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int[] upgradeCostByLevel = new int[10] { 5, 8, 12, 16, 20, 25, 30, 36, 44, 0 };
    [SerializeField] private int[] productionPerLevel = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    public RegionNode SelectedRegion
    {
        get; private set;
    }

    public bool IsPickingTarget
    {
        get; private set;
    }

    public RegionNode MoveSource
    {
        get; private set;
    }

    public int MoveAmount
    {
        get; set;
    }

    private readonly Dictionary<int, Order> pendingOrders = new();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterRegion(RegionNode node)
    {
        if (!pendingOrders.ContainsKey(node.Id)) pendingOrders[node.Id] = new Order { Kind = OrderKind.Wait, TargetRegionId = 0, Amount = 0 };
    }

    public void OnRegionClicked(RegionNode node)
    {
        if (IsPickingTarget)
        {
            if (node == MoveSource)
            {
                targetIndicator.ShowMessage("자기 자신은 대상이 될 수 없습니다");
                CancelTargetPicking();
                return;
            }
            ConfirmMove(node);
            return;
        }

        if (node.Owner != RegionNode.OwnerKind.Player) return;
        SelectRegion(node);
    }

    public void SelectRegion(RegionNode node)
    {
        SelectedRegion = node;
        RegionSelected(node);
    }

    public void IssueOrder(OrderKind kind)
    {
        pendingOrders[SelectedRegion.Id] = new Order { Kind = kind, TargetRegionId = 0, Amount = 0 };
        if (kind != OrderKind.Move) arrowManager.RemoveArrow(SelectedRegion.Id);
        else StartMove();
    }

    public bool TryGetOrder(int regionId, out Order order)
    {
        return pendingOrders.TryGetValue(regionId, out order);
    }

    public void StartMove()
    {
        MoveSource = SelectedRegion;
        IsPickingTarget = true;
        int preset = Mathf.Clamp(MoveAmount, 0, MoveSource.TroopCount);
        MoveAmount = preset;
        targetIndicator.SetActive(true, "이동할 대상 지역을 선택하세요");
    }

    public void CancelTargetPicking()
    {
        IsPickingTarget = false;
        MoveSource = null;
        targetIndicator.SetActive(false, "");
    }

    private void ConfirmMove(RegionNode target)
    {
        int amount = Mathf.Clamp(MoveAmount, 0, MoveSource.TroopCount);
        if (amount <= 0)
        {
            targetIndicator.ShowMessage("0명은 이동할 수 없습니다");
            CancelTargetPicking();
            return;
        }

        pendingOrders[MoveSource.Id] = new Order
        {
            Kind = OrderKind.Move,
            TargetRegionId = target.Id,
            Amount = amount
        };

        arrowManager.SetArrow(MoveSource, target);
        CancelTargetPicking();
    }

    public int MaxLevel
    {
        get { return maxLevel; }
    }

    public int StartLevel
    {
        get { return startLevel; }
    }

    public int TroopsForMaxIntensity
    {
        get { return troopsForMaxIntensity; }
    }

    public Color PlayerColor
    {
        get { return playerColor; }
    }

    public Color PlayerLightColor
    {
        get { return playerLightColor; }
    }

    public Color EnemyColor
    {
        get { return enemyColor; }
    }

    public Color EnemyLightColor
    {
        get { return enemyLightColor; }
    }

    public Color NeutralColor
    {
        get { return neutralColor; }
    }

    public int GetUpgradeCost(int level)
    {
        return upgradeCostByLevel[level - 1];
    }

    public int GetProductionPerTurn(int level)
    {
        return productionPerLevel[level - 1];
    }
}