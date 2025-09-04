using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

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
        public int Id;
        public int SourceId;
        public int TargetId;
        public int Amount;
        public RegionNode.OwnerKind Owner;
    }

    public event Action<RegionNode> RegionSelected = _ => { };

    [Header("UI")]
    [SerializeField] private UITargetModeIndicator targetIndicator;
    [SerializeField] private MoveArrowManager arrowManager;
    [SerializeField] private UITouchBlocker touchBlocker;
    [SerializeField] private UIMoveToken tokenPrefab;
    [SerializeField] private RectTransform tokenLayer;

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

    [Header("Animation Durations")]
    [SerializeField] private float upgradePause = 0.3f;
    [SerializeField] private float defensePause = 0.2f;
    [SerializeField] private float moveHalfDuration = 0.8f;
    [SerializeField] private float moveArriveDuration = 0.6f;
    [SerializeField] private float productionTickInterval = 0.1f;
    [SerializeField] private float productionBouncePower = 12f;
    [SerializeField] private float productionBounceDuration = 0.1f;
    [SerializeField] private float arrowFadeDuration = 0.3f;
    [SerializeField] private float phaseGap = 0.1f;

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

    public int MaxLevel
    {
        get { return maxLevel; }
    }

    public int StartLevel
    {
        get { return startLevel; }
    }

    public Color PlayerColor
    {
        get { return playerColor; }
    }

    public Color EnemyColor
    {
        get { return enemyColor; }
    }

    private readonly Dictionary<int, Order> pendingOrders = new Dictionary<int, Order>();
    private readonly Dictionary<int, RegionNode> regionsById = new Dictionary<int, RegionNode>();
    private readonly List<RegionNode> regions = new List<RegionNode>();
    private readonly List<MoveTransit> inTransit = new List<MoveTransit>();
    private readonly List<MoveTransit> bufferNext = new List<MoveTransit>();
    private readonly Dictionary<int, UIMoveToken> tokenByTransitId = new Dictionary<int, UIMoveToken>();
    private int nextTransitId;
    private bool skipRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Another instance of GameController exists, destroying this.");
            Destroy(this);
            return;
        }
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
            if (node == MoveSource) targetIndicator.ShowMessage("자기 자신은 대상이 될 수 없습니다");
            else ConfirmMove(node);
            CancelTargetPicking();
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
        int amount = Mathf.Clamp(MoveAmount, 1, MoveSource.TroopCount);
        pendingOrders[MoveSource.Id] = new Order
        {
            Kind = OrderKind.Move,
            TargetRegionId = target.Id,
            Amount = amount
        };
        arrowManager.SetArrow(MoveSource, target);
    }

    public void EndTurn()
    {
        StartCoroutine(ResolveTurnAnimated());
    }

    private IEnumerator ResolveTurnAnimated()
    {
        touchBlocker.Show(OnSkipPressed);
        skipRequested = false;

        yield return RunUpgradePhase();
        if (skipRequested) yield break;
        if (!skipRequested) yield return DOVirtual.DelayedCall(phaseGap, () => { }).WaitForCompletion();

        yield return RunDefensePhase();
        if (skipRequested) yield break;
        if (!skipRequested) yield return DOVirtual.DelayedCall(phaseGap, () => { }).WaitForCompletion();

        yield return RunMovePhase();
        if (skipRequested) yield break;
        if (!skipRequested) yield return DOVirtual.DelayedCall(phaseGap, () => { }).WaitForCompletion();

        yield return RunProductionPhase();
        if (skipRequested) yield break;

        ClearOrdersToWait();
        TurnIndex = TurnIndex + 1;
        RefreshAllLabels();
        touchBlocker.Hide();
    }

    private IEnumerator RunUpgradePhase()
    {
        bool any = false;
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            int id = regions[i].Id;
            Order o;
            if (!pendingOrders.TryGetValue(id, out o)) continue;
            if (o.Kind != OrderKind.Upgrade) continue;

            RegionNode n = regionsById[id];
            if (n.Level >= maxLevel) continue;

            int cost = GetUpgradeCost(n.Level);
            if (n.TroopCount < cost) continue;

            n.TroopCount = n.TroopCount - cost;
            n.Level = n.Level + 1;
            any = true;
        }

        if (!skipRequested && any) yield return DOVirtual.DelayedCall(upgradePause, () => { }).WaitForCompletion();
    }

    private IEnumerator RunDefensePhase()
    {
        bool any = false;

        for (int i = 0; i < regions.Count; i = i + 1)
        {
            int id = regions[i].Id;
            Order o;
            if (!pendingOrders.TryGetValue(id, out o)) continue;

            if (o.Kind == OrderKind.DefenseEnter)
            {
                regionsById[id].IsDefending = true;
                regionsById[id].SetDefenseVisual(true);
                any = true;
            }
            else if (o.Kind == OrderKind.DefenseExit)
            {
                regionsById[id].IsDefending = false;
                regionsById[id].SetDefenseVisual(false);
                any = true;
            }
        }

        if (!skipRequested && any) yield return DOVirtual.DelayedCall(defensePause, () => { }).WaitForCompletion();
    }

    private IEnumerator RunMovePhase()
    {
        Dictionary<int, List<MoveTransit>> arrivals = new Dictionary<int, List<MoveTransit>>();
        for (int i = 0; i < inTransit.Count; i = i + 1)
        {
            MoveTransit m = inTransit[i];
            if (!arrivals.ContainsKey(m.TargetId)) arrivals[m.TargetId] = new List<MoveTransit>();
            arrivals[m.TargetId].Add(m);
        }

        arrowManager.ClearAll();

        if (skipRequested)
        {
            for (int i = 0; i < inTransit.Count; i = i + 1)
            {
                int tid = inTransit[i].Id;
                UIMoveToken tkn;
                if (tokenByTransitId.TryGetValue(tid, out tkn)) tkn.InstantToTarget();
            }

            ResolveArrivalsNow(arrivals);

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
                    Id = ++nextTransitId,
                    SourceId = id,
                    TargetId = o.TargetRegionId,
                    Amount = o.Amount,
                    Owner = owner
                };
                bufferNext.Add(m);

                UIMoveToken token = Instantiate(tokenPrefab, tokenLayer);
                RegionNode dst = regionsById[o.TargetRegionId];
                token.Initialize(owner, o.Amount, src.Rect, dst.Rect, arrowManager, o.TargetRegionId, arrowFadeDuration);
                token.InstantToMid();
                tokenByTransitId[m.Id] = token;
            }
        }
        else
        {
            Sequence sArrive = DOTween.Sequence();
            for (int i = 0; i < inTransit.Count; i = i + 1)
            {
                int tid = inTransit[i].Id;
                UIMoveToken tkn;
                if (tokenByTransitId.TryGetValue(tid, out tkn)) sArrive.Join(tkn.AnimateToTarget(moveArriveDuration));
            }

            Sequence sMid = DOTween.Sequence();
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
                    Id = ++nextTransitId,
                    SourceId = id,
                    TargetId = o.TargetRegionId,
                    Amount = o.Amount,
                    Owner = owner
                };
                bufferNext.Add(m);

                UIMoveToken token = Instantiate(tokenPrefab, tokenLayer);
                RegionNode dst = regionsById[o.TargetRegionId];
                token.Initialize(owner, o.Amount, src.Rect, dst.Rect, arrowManager, o.TargetRegionId, arrowFadeDuration);
                sMid.Join(token.AnimateToMid(moveHalfDuration));
                tokenByTransitId[m.Id] = token;
            }

            if (sArrive.active || sMid.active) yield return DOTween.Sequence().Join(sArrive).Join(sMid).WaitForCompletion();

            ResolveArrivalsNow(arrivals);
        }

        for (int i = 0; i < inTransit.Count; i = i + 1) tokenByTransitId.Remove(inTransit[i].Id);
        inTransit.Clear();
        for (int i = 0; i < bufferNext.Count; i = i + 1) inTransit.Add(bufferNext[i]);
        bufferNext.Clear();
    }

    private void ResolveArrivalsNow(Dictionary<int, List<MoveTransit>> byTarget)
    {
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
                    target.SetDefenseVisual(false);
                    target.TroopCount = net - d;
                }
                else target.TroopCount = d - net;

                continue;
            }

            RegionNode.OwnerKind defenderOwner = target.Owner;
            int attackers = defenderOwner == RegionNode.OwnerKind.Player ? atkEnemy : atkPlayer;
            RegionNode.OwnerKind attackerOwner = defenderOwner == RegionNode.OwnerKind.Player ? RegionNode.OwnerKind.Enemy : RegionNode.OwnerKind.Player;

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
                target.SetDefenseVisual(false);
                target.TroopCount = effective - dNow;
            }
            else target.TroopCount = dNow - effective;
        }
    }

    private IEnumerator RunProductionPhase()
    {
        Sequence agg = DOTween.Sequence();
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            RegionNode n = regions[i];
            if (n.Owner == RegionNode.OwnerKind.Neutral) continue;
            int g = GetProductionPerTurn(n.Level);
            if (g <= 0) continue;
            agg.Join(n.PlayProductionBounceTween(g, productionTickInterval, productionBouncePower, productionBounceDuration));
        }
        if (!skipRequested && agg.active) yield return agg.WaitForCompletion();
    }

    private void ClearOrdersToWait()
    {
        for (int i = 0; i < regions.Count; i = i + 1)
        {
            int id = regions[i].Id;
            pendingOrders[id] = new Order { Kind = OrderKind.Wait, TargetRegionId = 0, Amount = 0 };
        }
    }

    private void RefreshAllLabels()
    {
        for (int i = 0; i < regions.Count; i = i + 1) regions[i].RefreshLabel();
    }

    private void OnSkipPressed()
    {
        skipRequested = true;
        StopAllCoroutines();

        Dictionary<int, List<MoveTransit>> arrivals = new Dictionary<int, List<MoveTransit>>();
        for (int i = 0; i < inTransit.Count; i = i + 1)
        {
            MoveTransit m = inTransit[i];
            if (!arrivals.ContainsKey(m.TargetId)) arrivals[m.TargetId] = new List<MoveTransit>();
            arrivals[m.TargetId].Add(m);
        }

        foreach (var kv in tokenByTransitId) kv.Value.InstantToTarget();

        ResolveArrivalsNow(arrivals);
        inTransit.Clear();
        tokenByTransitId.Clear();

        for (int i = 0; i < regions.Count; i = i + 1)
        {
            RegionNode n = regions[i];
            if (n.Owner == RegionNode.OwnerKind.Neutral) continue;
            int g = GetProductionPerTurn(n.Level);
            n.TroopCount = n.TroopCount + g;
        }

        ClearOrdersToWait();
        TurnIndex = TurnIndex + 1;
        RefreshAllLabels();
        arrowManager.ClearAll();
        touchBlocker.Hide();
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