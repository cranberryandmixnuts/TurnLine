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

    private sealed class MoveTransit
    {
        public int SourceId;
        public int TargetId;
        public int Amount;
        public RegionNode.OwnerKind Owner;
    }

    public event Action<RegionNode> RegionSelected = _ => { };

    [Header("UI")]
    [SerializeField] private UITargetModeIndicator targetIndicator;
    [SerializeField] private MoveArrowManager arrowManager;

    [Header("Ownership Colors")]
    [SerializeField] private Color playerColor = new Color(0.20f, 0.60f, 1.00f, 1f);
    [SerializeField] private Color playerLightColor = new Color(0.72f, 0.86f, 1.00f, 1f);
    [SerializeField] private Color enemyColor = new Color(1.00f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color enemyLightColor = new Color(1.00f, 0.80f, 0.80f, 1f);
    [SerializeField] private Color neutralColor = new Color(0.74f, 0.74f, 0.74f, 1f);
    [SerializeField] private int troopsForMaxIntensity = 50;

    [Header("Level Settings")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int[] upgradeCostByLevel = new int[10] { 5, 8, 12, 16, 20, 25, 30, 36, 44, 0 };
    [SerializeField] private int[] productionPerLevel = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Header("Defense Reduction")]
    [SerializeField] private float defendBase = 0.20f;
    [SerializeField] private float defendStep = 0.06f;
    [SerializeField] private float defendCap = 0.60f;

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

    public int TurnIndex
    {
        get; private set;
    }

    private readonly Dictionary<int, Order> pendingOrders = new Dictionary<int, Order>();
    private readonly Dictionary<int, RegionNode> regionsById = new Dictionary<int, RegionNode>();
    private readonly List<RegionNode> regions = new List<RegionNode>();
    private readonly List<MoveTransit> inTransit = new List<MoveTransit>();
    private readonly List<MoveTransit> bufferNext = new List<MoveTransit>();

    private void Awake()
    {
        Instance = this;

        RegionNode.MaxLevelCache = maxLevel;
        RegionNode.PlayerColorCache = playerColor;
        RegionNode.PlayerLightColorCache = playerLightColor;
        RegionNode.EnemyColorCache = enemyColor;
        RegionNode.EnemyLightColorCache = enemyLightColor;
        RegionNode.NeutralColorCache = neutralColor;
        RegionNode.TroopsForMaxIntensityCache = troopsForMaxIntensity;
    }

    private void Start()
    {
        InitializeRegions();
    }

    public void RegisterRegion(RegionNode node)
    {
        if (!pendingOrders.ContainsKey(node.Id)) pendingOrders[node.Id] = new Order { Kind = OrderKind.Wait, TargetRegionId = 0, Amount = 0 };
        if (!regionsById.ContainsKey(node.Id)) regionsById.Add(node.Id, node);
        if (!regions.Contains(node)) regions.Add(node);
        if (node.Level < 1) node.Level = startLevel;
    }

    private void InitializeRegions()
    {
        RegionNode[] nodes = FindObjectsByType<RegionNode>(FindObjectsSortMode.None);
        for (int i = 0; i < nodes.Length; i = i + 1) RegisterRegion(nodes[i]);
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
        int preset = Mathf.Clamp(MoveAmount, 1, MoveSource.TroopCount);
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

    public void EndTurn()
    {
        ResolveArrivals();
        ApplyUpgradesAndStances();
        CommitMoves();
        arrowManager.ClearAll();
        ClearOrdersToWait();
        ProduceNextTurn();
        TurnIndex = TurnIndex + 1;
        PromoteTransitBuffer();
        RefreshAllLabels();
    }

    private void ResolveArrivals()
    {
        Dictionary<int, List<MoveTransit>> byTarget = new Dictionary<int, List<MoveTransit>>();
        for (int i = 0; i < inTransit.Count; i = i + 1)
        {
            MoveTransit m = inTransit[i];
            if (!byTarget.ContainsKey(m.TargetId)) byTarget[m.TargetId] = new List<MoveTransit>();
            byTarget[m.TargetId].Add(m);
        }

        foreach (var kv in byTarget)
        {
            int targetId = kv.Key;
            List<MoveTransit> list = kv.Value;
            RegionNode target = regionsById[targetId];

            int atkPlayer = 0;
            int atkEnemy = 0;
            for (int i = 0; i < list.Count; i = i + 1)
            {
                if (list[i].Owner == RegionNode.OwnerKind.Player) atkPlayer = atkPlayer + list[i].Amount;
                else if (list[i].Owner == RegionNode.OwnerKind.Enemy) atkEnemy = atkEnemy + list[i].Amount;
            }

            if (target.Owner == RegionNode.OwnerKind.Neutral)
            {
                int net = Mathf.Abs(atkPlayer - atkEnemy);
                RegionNode.OwnerKind netOwner = atkPlayer >= atkEnemy ? RegionNode.OwnerKind.Player : RegionNode.OwnerKind.Enemy;
                int d = target.TroopCount;
                if (net > d)
                {
                    target.Owner = netOwner;
                    target.IsDefending = false;
                    target.TroopCount = net - d;
                }
                else
                {
                    target.TroopCount = d - net;
                }
                continue;
            }

            RegionNode.OwnerKind defenderOwner = target.Owner;
            int attackers = 0;
            RegionNode.OwnerKind attackerOwner = RegionNode.OwnerKind.Neutral;
            if (defenderOwner == RegionNode.OwnerKind.Player)
            {
                attackers = atkEnemy;
                attackerOwner = RegionNode.OwnerKind.Enemy;
            }
            else
            {
                attackers = atkPlayer;
                attackerOwner = RegionNode.OwnerKind.Player;
            }

            bool exitThisTurn = false;
            Order defOrder;
            if (pendingOrders.TryGetValue(targetId, out defOrder))
                if (defOrder.Kind == OrderKind.DefenseExit) exitThisTurn = true;

            float r = Mathf.Min(defendCap, defendBase + defendStep * target.Level);
            int effective = target.IsDefending && !exitThisTurn ? Mathf.FloorToInt(attackers * (1f - r)) : attackers;

            int dNow = target.TroopCount;
            if (effective > dNow)
            {
                target.Owner = attackerOwner;
                target.IsDefending = false;
                target.TroopCount = effective - dNow;
            }
            else
            {
                target.TroopCount = dNow - effective;
            }
        }

        inTransit.Clear();
    }

    private void ApplyUpgradesAndStances()
    {
        foreach (var kv in pendingOrders)
        {
            int id = kv.Key;
            Order o = kv.Value;
            RegionNode node = regionsById[id];

            if (o.Kind == OrderKind.Upgrade)
            {
                if (node.Level < maxLevel)
                {
                    int cost = GetUpgradeCost(node.Level);
                    if (node.TroopCount >= cost)
                    {
                        node.TroopCount = node.TroopCount - cost;
                        node.Level = node.Level + 1;
                    }
                }
            }
            else if (o.Kind == OrderKind.DefenseEnter)
            {
                node.IsDefending = true;
            }
            else if (o.Kind == OrderKind.DefenseExit)
            {
                node.IsDefending = false;
            }
        }
    }

    private void CommitMoves()
    {
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            int id = regions[i].Id;
            Order o;
            if (!pendingOrders.TryGetValue(id, out o)) continue;
            if (o.Kind != OrderKind.Move) continue;

            RegionNode src = regionsById[id];
            RegionNode.OwnerKind owner = src.Owner;
            src.TroopCount = src.TroopCount - o.Amount;

            MoveTransit m = new MoveTransit
            {
                SourceId = id,
                TargetId = o.TargetRegionId,
                Amount = o.Amount,
                Owner = owner
            };
            bufferNext.Add(m);
        }
    }

    private void ClearOrdersToWait()
    {
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            int id = regions[i].Id;
            pendingOrders[id] = new Order { Kind = OrderKind.Wait, TargetRegionId = 0, Amount = 0 };
        }
    }

    private void ProduceNextTurn()
    {
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            RegionNode n = regions[i];
            if (n.Owner == RegionNode.OwnerKind.Neutral) continue;
            int g = GetProductionPerTurn(n.Level);
            n.TroopCount = n.TroopCount + g;
        }
    }

    private void PromoteTransitBuffer()
    {
        for (int i = 0; i < bufferNext.Count; i = i + 1)
        {
            inTransit.Add(bufferNext[i]);
        }
        bufferNext.Clear();
    }

    private void RefreshAllLabels()
    {
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            regions[i].RefreshLabel();
        }
    }

    public int MaxLevel
    {
        get { return maxLevel; }
    }

    public int StartLevel
    {
        get { return startLevel; }
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