using System;
using System.Collections.Generic;
using UnityEngine;

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

    public event Action<RegionNode> RegionSelected;

    [SerializeField] private UITargetModeIndicator targetIndicator;
    [SerializeField] private MoveArrowManager arrowManager;

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

    private readonly Dictionary<int, Order> pendingOrders = new Dictionary<int, Order>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
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
            if (MoveSource == null) return;
            if (node == MoveSource)
            {
                if (targetIndicator != null) targetIndicator.ShowMessage("자기 자신은 대상이 될 수 없습니다");
                CancelTargetPicking();
                return;
            }
            ConfirmMove(node);
            return;
        }
        SelectRegion(node);
    }

    public void SelectRegion(RegionNode node)
    {
        SelectedRegion = node;
        RegionSelected?.Invoke(node);
    }

    public void IssueOrder(OrderKind kind)
    {
        if (SelectedRegion == null) return;
        if (kind == OrderKind.Move)
        {
            StartMove();
            return;
        }
        pendingOrders[SelectedRegion.Id] = new Order { Kind = kind, TargetRegionId = 0, Amount = 0 };
        if (arrowManager != null) arrowManager.RemoveArrow(SelectedRegion.Id);
    }

    public bool TryGetOrder(int regionId, out Order order)
    {
        return pendingOrders.TryGetValue(regionId, out order);
    }

    public void StartMove()
    {
        if (SelectedRegion == null) return;
        MoveSource = SelectedRegion;
        IsPickingTarget = true;
        int preset = Mathf.Clamp(MoveAmount, 0, MoveSource.TroopCount);
        MoveAmount = preset;
        if (targetIndicator != null) targetIndicator.SetActive(true, "이동할 대상 지역을 선택하세요");
    }

    public void CancelTargetPicking()
    {
        IsPickingTarget = false;
        MoveSource = null;
        if (targetIndicator != null) targetIndicator.SetActive(false, "");
    }

    private void ConfirmMove(RegionNode target)
    {
        int amount = Mathf.Clamp(MoveAmount, 0, MoveSource.TroopCount);
        if (amount <= 0)
        {
            if (targetIndicator != null) targetIndicator.ShowMessage("0명은 이동할 수 없습니다");
            CancelTargetPicking();
            return;
        }
        pendingOrders[MoveSource.Id] = new Order { Kind = OrderKind.Move, TargetRegionId = target.Id, Amount = amount };
        if (arrowManager != null) arrowManager.SetArrow(MoveSource, target);
        CancelTargetPicking();
    }
}