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

    [SerializeField] private Color playerBaseColor = new Color(0.20f, 0.55f, 1.00f, 1f);
    [SerializeField] private Color enemyBaseColor = new Color(1.00f, 0.30f, 0.30f, 1f);
    [SerializeField] private float minTintStrength = 0.55f;
    [SerializeField] private float maxTintStrength = 1.00f;

    private readonly List<RegionNode> regions = new List<RegionNode>();

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
            Debug.LogWarning("Multiple GameController instances detected. Destroying duplicate.");
            return;
        }
        Instance = this;
    }

    public void RegisterRegion(RegionNode node)
    {
        if (!regions.Contains(node)) regions.Add(node);
        if (!pendingOrders.ContainsKey(node.Id)) pendingOrders[node.Id] = new Order { Kind = OrderKind.Wait, TargetRegionId = 0, Amount = 0 };
        node.ApplyOwnerTint();
        RefreshAllOwnerTints();
    }

    public void NotifyTroopChanged()
    {
        RefreshAllOwnerTints();
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
        RegionSelected?.Invoke(node);
    }

    public void IssueOrder(OrderKind kind)
    {
        if (kind == OrderKind.Move)
        {
            StartMove();
            return;
        }
        pendingOrders[SelectedRegion.Id] = new Order { Kind = kind, TargetRegionId = 0, Amount = 0 };
        arrowManager.RemoveArrow(SelectedRegion.Id);
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
        pendingOrders[MoveSource.Id] = new Order { Kind = OrderKind.Move, TargetRegionId = target.Id, Amount = amount };
        arrowManager.SetArrow(MoveSource, target);
        CancelTargetPicking();
    }

    public Color GetTintFor(RegionNode node)
    {
        if (node.Owner == RegionNode.OwnerKind.Neutral) return node.NeutralBaseColor;

        int maxPlayer = 0;
        int maxEnemy = 0;

        for (int i = 0; i < regions.Count; i++)
        {
            var r = regions[i];
            if (r.Owner == RegionNode.OwnerKind.Player) maxPlayer = Mathf.Max(maxPlayer, r.TroopCount);
            else if (r.Owner == RegionNode.OwnerKind.Enemy) maxEnemy = Mathf.Max(maxEnemy, r.TroopCount);
        }

        float t;
        Color teamBase;

        if (node.Owner == RegionNode.OwnerKind.Player)
        {
            teamBase = playerBaseColor;
            t = maxPlayer > 0 ? Mathf.Clamp01((float)node.TroopCount / maxPlayer) : 0f;
        }
        else
        {
            teamBase = enemyBaseColor;
            t = maxEnemy > 0 ? Mathf.Clamp01((float)node.TroopCount / maxEnemy) : 0f;
        }

        float strength = Mathf.Lerp(minTintStrength, maxTintStrength, t);
        Color result = Color.Lerp(node.NeutralBaseColor, teamBase, strength);
        result.a = 1f;
        return result;
    }

    private void RefreshAllOwnerTints()
    {
        for (int i = 0; i < regions.Count; i++) regions[i].ApplyOwnerTint();
    }
}
