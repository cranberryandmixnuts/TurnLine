using System.Collections.Generic;
using UnityEngine;

public sealed class AIAgent
{
    private sealed class AIParams
    {
        public int HubCount;
        public float NeedMultiplier;
        public float MinAttackScore;
        public bool AllowDefense;
        public bool AllowUpgrade;
        public bool AllowConsolidation;
        public bool AllowReinforce;
        public float ConsolidationRadius;
    }

    private readonly GameController gc;

    public AIAgent(GameController controller)
    {
        gc = controller;
    }

    public void PlanTurn(int aiLevel)
    {
        if (aiLevel < 1) aiLevel = 1;
        if (aiLevel > 4) aiLevel = 4;

        AIParams P = MakeParams(aiLevel);

        List<RegionNode> enemies = gc.GetRegionsOwned(RegionNode.OwnerKind.Enemy);
        if (enemies.Count == 0) return;

        List<RegionNode> neutrals = gc.GetRegionsOwned(RegionNode.OwnerKind.Neutral);
        List<RegionNode> players = gc.GetRegionsOwned(RegionNode.OwnerKind.Player);

        HashSet<int> usedAsDonor = new HashSet<int>();
        Dictionary<int, int> plannedSend = new Dictionary<int, int>();

        for (int i = 0; i < enemies.Count; i = i + 1)
        {
            gc.SetOrder(enemies[i].Id, GameController.OrderKind.Wait, 0, 0);
        }

        if (P.AllowDefense)
        {
            for (int i = 0; i < enemies.Count; i = i + 1)
            {
                RegionNode n = enemies[i];
                int incoming = gc.CountIncomingFor(n.Id, RegionNode.OwnerKind.Player);
                if (incoming > 0) gc.SetOrder(n.Id, GameController.OrderKind.DefenseEnter, 0, 0);
            }
        }

        List<RegionNode> hubs = PickHubs(enemies, P.HubCount);

        if (P.AllowUpgrade)
        {
            for (int i = 0; i < hubs.Count; i = i + 1)
            {
                RegionNode h = hubs[i];
                if (h.Level >= gc.MaxLevel) continue;
                int cost = gc.GetUpgradeCost(h.Level);
                if (h.TroopCount >= cost)
                {
                    gc.SetOrder(h.Id, GameController.OrderKind.Upgrade, 0, 0);
                }
            }
        }

        if (P.AllowConsolidation)
        {
            for (int i = 0; i < hubs.Count; i = i + 1)
            {
                RegionNode h = hubs[i];
                if (h.Level >= gc.MaxLevel) continue;
                int cost = gc.GetUpgradeCost(h.Level);
                int deficit = Mathf.Max(0, cost - h.TroopCount);
                if (deficit <= 0) continue;

                List<RegionNode> donors = CollectDonors(enemies, h, usedAsDonor, P.ConsolidationRadius);
                for (int d = 0; d < donors.Count && deficit > 0; d = d + 1)
                {
                    RegionNode from = donors[d];
                    int surplus = Surplus(from);
                    if (surplus <= 0) continue;
                    int send = Mathf.Min(surplus, deficit);
                    usedAsDonor.Add(from.Id);
                    plannedSend[from.Id] = send;
                    gc.SetOrder(from.Id, GameController.OrderKind.Move, h.Id, send);
                    gc.DrawAIArrow(from, h);
                    deficit = deficit - send;
                }
            }
        }

        if (P.AllowReinforce)
        {
            for (int i = 0; i < enemies.Count; i = i + 1)
            {
                RegionNode n = enemies[i];
                int incoming = gc.CountIncomingFor(n.Id, RegionNode.OwnerKind.Player);
                if (incoming <= 0) continue;

                int defenders = n.TroopCount;
                float r = gc.DefenseFactor(n.Level, n.IsDefending, false);
                int needed = Mathf.CeilToInt(incoming / Mathf.Max(0.01f, 1f - r));
                int gap = Mathf.Max(0, needed - defenders);

                if (gap <= 0) continue;

                List<RegionNode> donors = CollectDonors(enemies, n, usedAsDonor, P.ConsolidationRadius);
                for (int d = 0; d < donors.Count && gap > 0; d = d + 1)
                {
                    RegionNode from = donors[d];
                    int surplus = Surplus(from);
                    if (surplus <= 0) continue;
                    int send = Mathf.Min(surplus, gap);
                    usedAsDonor.Add(from.Id);
                    plannedSend[from.Id] = send;
                    gc.SetOrder(from.Id, GameController.OrderKind.Move, n.Id, send);
                    gc.DrawAIArrow(from, n);
                    gap = gap - send;
                }
            }
        }

        for (int i = 0; i < enemies.Count; i = i + 1)
        {
            RegionNode from = enemies[i];
            if (usedAsDonor.Contains(from.Id)) continue;

            int surplusNow = Surplus(from);
            if (surplusNow <= 0)
            {
                MaybeExitDefense(from, P);
                continue;
            }

            Target best = PickBestTarget(from, neutrals, players, P.NeedMultiplier);
            if (best.Kind == TargetKind.None)
            {
                MaybeExitDefense(from, P);
                continue;
            }

            float score = AttackScore(from, best.Region, best.Required);
            if (score < P.MinAttackScore)
            {
                MaybeExitDefense(from, P);
                continue;
            }

            int send = Mathf.Clamp(best.Required, 1, surplusNow);
            gc.SetOrder(from.Id, GameController.OrderKind.Move, best.Region.Id, send);
            gc.DrawAIArrow(from, best.Region);
            if (from.IsDefending && P.AllowConsolidation) gc.SetOrder(from.Id, GameController.OrderKind.DefenseExit, 0, 0);
        }
    }

    private enum TargetKind
    {
        None,
        Neutral,
        Player
    }

    private sealed class Target
    {
        public TargetKind Kind;
        public RegionNode Region;
        public int Required;
    }

    private AIParams MakeParams(int level)
    {
        AIParams p = new AIParams();
        if (level == 1)
        {
            p.HubCount = 0;
            p.NeedMultiplier = 1.0f;
            p.MinAttackScore = 0.75f;
            p.AllowDefense = false;
            p.AllowUpgrade = false;
            p.AllowConsolidation = false;
            p.AllowReinforce = false;
            p.ConsolidationRadius = 0f;
            return p;
        }
        if (level == 2)
        {
            p.HubCount = 1;
            p.NeedMultiplier = 1.0f;
            p.MinAttackScore = 0.5f;
            p.AllowDefense = true;
            p.AllowUpgrade = true;
            p.AllowConsolidation = false;
            p.AllowReinforce = false;
            p.ConsolidationRadius = 0f;
            return p;
        }
        if (level == 3)
        {
            p.HubCount = 1;
            p.NeedMultiplier = 0.9f;
            p.MinAttackScore = 0.35f;
            p.AllowDefense = true;
            p.AllowUpgrade = true;
            p.AllowConsolidation = true;
            p.AllowReinforce = false;
            p.ConsolidationRadius = 99999f;
            return p;
        }
        p.HubCount = 2;
        p.NeedMultiplier = 0.8f;
        p.MinAttackScore = 0.2f;
        p.AllowDefense = true;
        p.AllowUpgrade = true;
        p.AllowConsolidation = true;
        p.AllowReinforce = true;
        p.ConsolidationRadius = 99999f;
        return p;
    }

    private List<RegionNode> PickHubs(List<RegionNode> enemies, int count)
    {
        List<RegionNode> sorted = new List<RegionNode>(enemies);
        sorted.Sort((a, b) =>
        {
            int lv = b.Level.CompareTo(a.Level);
            if (lv != 0) return lv;
            return b.TroopCount.CompareTo(a.TroopCount);
        });
        if (count < 1) count = 0;
        if (count > sorted.Count) count = sorted.Count;
        return sorted.GetRange(0, count);
    }

    private List<RegionNode> CollectDonors(List<RegionNode> enemies, RegionNode target, HashSet<int> used, float radius)
    {
        List<RegionNode> list = new List<RegionNode>();
        for (int i = 0; i < enemies.Count; i = i + 1)
        {
            RegionNode n = enemies[i];
            if (n.Id == target.Id) continue;
            if (used.Contains(n.Id)) continue;
            if (Surplus(n) <= 0) continue;
            if (radius > 0f)
            {
                float d = Distance(n, target);
                if (d > radius) continue;
            }
            list.Add(n);
        }
        list.Sort((a, b) => Distance(a, target).CompareTo(Distance(b, target)));
        return list;
    }

    private void MaybeExitDefense(RegionNode n, AIParams P)
    {
        if (!P.AllowConsolidation) return;
        int incoming = gc.CountIncomingFor(n.Id, RegionNode.OwnerKind.Player);
        if (incoming == 0 && n.IsDefending) gc.SetOrder(n.Id, GameController.OrderKind.DefenseExit, 0, 0);
    }

    private Target PickBestTarget(RegionNode from, List<RegionNode> neutrals, List<RegionNode> players, float needMul)
    {
        Target best = new Target { Kind = TargetKind.None, Region = null, Required = 0 };

        for (int i = 0; i < neutrals.Count; i = i + 1)
        {
            RegionNode t = neutrals[i];
            int need = t.TroopCount + 1;
            if (need <= 0) need = 1;
            if (AttackScore(from, t, need) <= AttackScore(from, best.Region, best.Required)) { }
            else
            {
                best.Kind = TargetKind.Neutral;
                best.Region = t;
                best.Required = need;
            }
        }

        for (int i = 0; i < players.Count; i = i + 1)
        {
            RegionNode t = players[i];
            float r = gc.DefenseFactor(t.Level, t.IsDefending, false);
            int need = Mathf.CeilToInt(t.TroopCount / Mathf.Max(0.01f, 1f - r)) + 1;
            need = Mathf.CeilToInt(need * needMul);
            if (need <= 0) need = 1;
            float sNew = AttackScore(from, t, need) + 0.5f;
            float sOld = AttackScore(from, best.Region, best.Required);
            if (sNew <= sOld) { }
            else
            {
                best.Kind = TargetKind.Player;
                best.Region = t;
                best.Required = need;
            }
        }

        return best;
    }

    private float AttackScore(RegionNode from, RegionNode to, int need)
    {
        if (to == null) return float.NegativeInfinity;
        float d = Distance(from, to);
        return (from.TroopCount - need) / Mathf.Max(1f, d);
    }

    private int Surplus(RegionNode n)
    {
        return Mathf.Max(0, n.TroopCount - 1);
    }

    private float Distance(RegionNode a, RegionNode b)
    {
        Vector3 pa = a.Rect.position;
        Vector3 pb = b.Rect.position;
        return Vector2.Distance(new Vector2(pa.x, pa.y), new Vector2(pb.x, pb.y));
    }
}