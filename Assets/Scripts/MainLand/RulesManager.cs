using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RulesManager : MonoBehaviour
{
       // ---------- Finish Bays ----------
    [System.Serializable]
    public class FinishBay
    {
        public PlayerColor color;
        public List<Transform> slots = new List<Transform>(); // ترتیب اسلات‌ها
    }

    [Header("Finish Bays (assign in Inspector)")]
    public List<FinishBay> finishBays = new List<FinishBay>();

    // ---------- Internal State ----------
    private readonly HashSet<Token> finishedTokens = new HashSet<Token>();
    private readonly Dictionary<Token, int> homeSlotOfToken = new Dictionary<Token, int>();

    private readonly Dictionary<PlayerColor, int> finishCounters = new Dictionary<PlayerColor, int>();


    // ======================================
    // Public API (GameManager / others call these)
    // ======================================

    /// تمام مهره‌ها را به نزدیک‌ترین اسلات خانه‌شان مپ می‌کند (برای شروع بازی/لود صحنه)
    public void EnsureHomeSlotAssignedForAll(List<PlayerController> players)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (p == null || p.Tokens == null) continue;
            foreach (var t in p.Tokens) EnsureHomeSlotAssigned(t);
        }
    }

    /// وقتی مهره به آخر مسیر خودش رسید (آخر FullPath)
    public void HandleIfFinished(Token t)
    {
        if (t == null || t.owner == null || !t.isOnBoard) return;

        var pc = t.owner;
        var bm = pc.boardManager;
        if (bm == null) return;

        var path = bm.GetFullPath(pc.color);
        if (path == null || path.Count == 0) return;

        int lastIndex = path.Count - 1;
    Debug.Log($"[Rules] Token {t.name} idx={t.currentTileIndex}, last={lastIndex}, color={pc.color}");


        if (t.currentTileIndex != lastIndex) return;
    Debug.Log($"[Rules] FINISH triggered for {t.name} ({pc.color})");



           // خروج از برد و انتقال به FinishBay
    t.isOnBoard = false;
    t.isMoving = false;
    t.currentTileIndex = -999;

    var bay = GetBay(pc.color);
    if (bay == null || bay.slots == null || bay.slots.Count == 0)
    {
        Debug.LogWarning($"[Rules] FinishBay for {pc.color} is not set.");
        finishedTokens.Add(t);
        return;
    }

    // ✨ لاجیک جدید: به ازای هر رنگ، به ترتیب توی اسلات‌ها می‌چینیم
    if (!finishCounters.TryGetValue(pc.color, out var count))
        count = 0;

    int slotIndex = Mathf.Clamp(count, 0, bay.slots.Count - 1);
    finishCounters[pc.color] = count + 1;

    var target = bay.slots[slotIndex];
    if (target != null)
        t.transform.position = target.position;

    finishedTokens.Add(t);

    }

    /// به صورت دستی برگرداندن مهره به خانه (اگر جای دیگری لازم داشته باشی)
    public void SendTokenHome(Token t)
    {
        if (t == null || t.owner == null) return;

        finishedTokens.Remove(t);
        t.isOnBoard = false;
        t.isMoving = false;
        t.currentTileIndex = -1;

        int slot = FindHomeSlotFor(t);
        homeSlotOfToken[t] = slot;

        var pc = t.owner;
        if (pc != null && pc.spawnPoints != null && pc.spawnPoints.Count > 0)
        {
            int idx = Mathf.Clamp(slot, 0, pc.spawnPoints.Count - 1);
            var sp = pc.spawnPoints[idx];
            if (sp != null) t.transform.position = sp.position;
        }

        Debug.Log($"[SendHome] {t.name} -> {t.owner.color} spawn slot {slot}");
    }

    /// آیا این مهره جزو تمام‌شده‌هاست؟
    public bool IsFinished(Token t) => t != null && finishedTokens.Contains(t);

    // ======================================
    // Finish Bay helpers
    // ======================================

    private FinishBay GetBay(PlayerColor color)
    {
        for (int i = 0; i < finishBays.Count; i++)
            if (finishBays[i] != null && finishBays[i].color == color)
                return finishBays[i];
        return null;
    }

//     private int FindFirstFreeFinishSlot(PlayerController pc, FinishBay bay)
//     {
//         int n = bay.slots.Count;
//         if (n <= 0) return 0;

//         bool[] occupied = new bool[n];

//         foreach (var tok in pc.Tokens)
//         {
//             if (tok == null) continue;
//             if (!finishedTokens.Contains(tok)) continue;

//             int idx = NearestIndex(bay.slots, tok.transform.position);
//             if (idx >= 0 && idx < n) occupied[idx = Mathf.Clamp(idx, 0, n - 1)] = true;
//         }

//         for (int i = 0; i < n; i++)
//             if (!occupied[i]){
//              Debug.Log($"[Rules] Free finish slot for {pc.color}: {i}");
//              return i;
// }
//         return 0;
//     }

    private int NearestIndex(List<Transform> points, Vector3 pos)
    {
        if (points == null || points.Count == 0) return -1;
        float best = float.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;
            float d = (p.position - pos).sqrMagnitude;
            if (d < best) { best = d; bestIdx = i; }
        }
        return bestIdx;
    }

    // ======================================
    // Home / Spawn helpers
    // ======================================

    private void EnsureHomeSlotAssigned(Token t)
    {
        if (t == null || t.owner == null) return;
        if (homeSlotOfToken.ContainsKey(t)) return;

        var pc = t.owner;
        if (pc.spawnPoints == null || pc.spawnPoints.Count == 0) return;

        int idx = NearestSpawnIndex(pc, t.transform.position);
        if (idx < 0) idx = 0;
        homeSlotOfToken[t] = idx;
    }

    private int FindHomeSlotFor(Token t)
    {
        var pc = t.owner;
        if (pc == null || pc.spawnPoints == null || pc.spawnPoints.Count == 0) return 0;
        int count = pc.spawnPoints.Count;

        bool[] occupied = new bool[count];
        foreach (var tok in pc.Tokens)
        {
            if (tok == null || tok == t) continue;
            if (tok.isOnBoard) continue;

            if (homeSlotOfToken.TryGetValue(tok, out int hs) && hs >= 0 && hs < count)
            {
                occupied[hs] = true;
            }
            else
            {
                int nearest = NearestSpawnIndex(pc, tok.transform.position);
                if (nearest >= 0 && nearest < count) occupied[nearest] = true;
            }
        }

        if (homeSlotOfToken.TryGetValue(t, out int mySlot) &&
            mySlot >= 0 && mySlot < count && !occupied[mySlot])
            return mySlot;

        int bestIdx = -1;
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (pc.spawnPoints[i] == null || occupied[i]) continue;
            float d = (pc.spawnPoints[i].position - t.transform.position).sqrMagnitude;
            if (d < best) { best = d; bestIdx = i; }
        }
        return (bestIdx >= 0) ? bestIdx : 0;
    }

    private int NearestSpawnIndex(PlayerController pc, Vector3 pos)
    {
        if (pc == null || pc.spawnPoints == null || pc.spawnPoints.Count == 0) return -1;
        float best = float.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < pc.spawnPoints.Count; i++)
        {
            var sp = pc.spawnPoints[i];
            if (sp == null) continue;
            float d = (pos - sp.position).sqrMagnitude;
            if (d < best) { best = d; bestIdx = i; }
        }
        return bestIdx;
    }

    public void ResetFinishState()
{
    finishedTokens.Clear();
    finishCounters.Clear();
    // اگه جایی لازم شد خونه‌ها رو هم ریست کنی:
    // homeSlotOfToken.Clear();
}

public bool IsTokenFinished(Token t)
{
    return t != null && finishedTokens.Contains(t);
}











}
