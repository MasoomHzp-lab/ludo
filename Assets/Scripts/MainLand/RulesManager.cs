using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RulesManager : MonoBehaviour
{
    [Header("Safe Tiles (assign in Inspector)")]
    public List<Transform> safeTileMarkers = new List<Transform>(); // ستاره‌ها/خانه‌های امن
    public float safeEpsilon = 0.02f;       // تلورانس تشخیص موقعیت امن
    public float sameTileEpsilon = 0.001f;  // تلورانس تشخیص هم‌خانه‌ای (~1mm)

    // ---------- Finish Bays ----------
    [System.Serializable]
    public class FinishBay
    {
        public PlayerColor color;
        public List<Transform> slots = new List<Transform>(); // ترتیب دلخواه: (نارنجی، زرد، بنفش، صورتی)
    }

    [Header("Finish Bays (assign in Inspector)")]
    public List<FinishBay> finishBays = new List<FinishBay>();

    // مهره‌هایی که بازی‌شان تمام شده (در خانهٔ نهایی قرار گرفته‌اند)
    private readonly HashSet<Token> finishedTokens = new HashSet<Token>();

    // اسلات خانۀ اختصاصی هر مهره (Home/Spawn) — بدون تغییر Token.cs
    private readonly Dictionary<Token, int> homeSlotOfToken = new Dictionary<Token, int>();

    // ---------- API عمومی که GameManager صدا می‌زند ----------

    public void EnsureHomeSlotAssignedForAll(List<PlayerController> players)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (p == null || p.Tokens == null) continue;
            foreach (var t in p.Tokens)
                EnsureHomeSlotAssigned(t);
        }
    }

    public void ResolveCaptures(GameManager gm, Token mover)
    {
        if (gm == null || mover == null || !mover.isOnBoard) return;
        if (IsSafeTile(mover)) return;

        foreach (var p in gm.players)
        {
            if (p == null || p.Tokens == null) continue;
            foreach (var other in p.Tokens)
            {
                if (other == null || other == mover) continue;
                if (!other.isOnBoard) continue;
                if (finishedTokens.Contains(other)) continue; // تو Finish هست → برخورد نشه

                if (AreOnSameTile(mover, other))
                {
                    if (other.owner == mover.owner) continue;
                    if (IsSafeTile(other)) continue;

                    SendTokenHome(other);
                    Debug.Log($"[Capture] {mover.owner.playerName} captured {other.owner.playerName}'s token.");
                }
            }
        }
    }

    /// وقتی مهره به آخر مسیر خودش رسید، بفرستش داخل خانهٔ نهایی در اولین اسلات خالی
    public void HandleIfFinished(Token t)
    {
        if (t == null || t.owner == null || !t.isOnBoard) return;

        var pc = t.owner;
        var bm = pc.boardManager;
        if (bm == null) return;

        var path = bm.GetFullPath(pc.color);
        if (path == null || path.Count == 0) return;

        int lastIndex = path.Count - 1;
        if (t.currentTileIndex != lastIndex) return; // هنوز به آخر مسیر نرسیده

        // جدا از برد
        t.isOnBoard = false;
        t.isMoving = false;
        t.currentTileIndex = -999; // نشانگر اختیاری: Finished

        // پیدا کردن Bay مربوط به همین رنگ
        var bay = GetBay(pc.color);
        if (bay == null || bay.slots == null || bay.slots.Count == 0)
        {
            Debug.LogWarning($"[Rules] FinishBay for {pc.color} is not set.");
            finishedTokens.Add(t);
            return;
        }

        int slot = FindFirstFreeFinishSlot(pc, bay);
        var target = bay.slots[Mathf.Clamp(slot, 0, bay.slots.Count - 1)];
        if (target != null) t.transform.position = target.position;

        finishedTokens.Add(t);
    }

    public bool IsSafeTile(Token t)
    {
        if (t == null || t.owner == null) return false;

        // 1) خانه‌ی شروع هر رنگ امن است
        if (t.isOnBoard && t.currentTileIndex == 0) return true;

        // 2) مارکرهای امن (اختیاری از اینسپکتور)
        if (safeTileMarkers != null && safeTileMarkers.Count > 0)
        {
            Vector3 pos = t.transform.position;
            float sq = safeEpsilon * safeEpsilon;
            foreach (var m in safeTileMarkers)
            {
                if (m == null) continue;
                if ((pos - m.position).sqrMagnitude <= sq)
                    return true;
            }
        }

        // 3) اگر BoardManager API امن دارد، اینجا استفاده کن (اختیاری)
        try
        {
            var bm = (t.owner != null) ? t.owner.boardManager : null;
            if (bm != null)
            {
                // if (bm.IsSafeTile(t.owner.color, t.currentTileIndex)) return true;
                // if (bm.IsInHomePath(t.owner.color, t.currentTileIndex)) return true;
            }
        }
        catch { }

        return false;
    }

    public bool AreOnSameTile(Token a, Token b)
    {
        if (a == null || b == null) return false;
        if (!a.isOnBoard || !b.isOnBoard) return false;

        // گرد کردن برای حذف نویز
        Vector3 pa = Snap(a.transform.position);
        Vector3 pb = Snap(b.transform.position);

        float sq = sameTileEpsilon * sameTileEpsilon;
        return (pa - pb).sqrMagnitude <= sq;
    }

    // ---------- داخلی: Finish Bays ----------

    private FinishBay GetBay(PlayerColor color)
    {
        for (int i = 0; i < finishBays.Count; i++)
            if (finishBays[i] != null && finishBays[i].color == color)
                return finishBays[i];
        return null;
    }

    private int FindFirstFreeFinishSlot(PlayerController pc, FinishBay bay)
    {
        int n = bay.slots.Count;
        bool[] occupied = new bool[n];

        // هر مهرهٔ تمام‌شدهٔ همین بازیکن → نزدیک‌ترین اسلات را اشغال کرده فرض کن
        foreach (var tok in pc.Tokens)
        {
            if (tok == null) continue;
            if (!finishedTokens.Contains(tok)) continue;

            int idx = NearestIndex(bay.slots, tok.transform.position);
            if (idx >= 0 && idx < n) occupied[idx] = true;
        }

        for (int i = 0; i < n; i++)
            if (!occupied[i]) return i;

        return 0; // نادر: اگر همه پر بود
    }

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

    // ---------- داخلی: Home/Spawn ----------

    private void SendTokenHome(Token t)
    {
        if (t == null || t.owner == null) return;

        finishedTokens.Remove(t);   // اگر قبلاً در Finish بود، حذف شود
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
    }

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
                if (nearest >= 0) occupied[nearest] = true;
            }
        }

        if (homeSlotOfToken.TryGetValue(t, out int mySlot) && mySlot >= 0 && mySlot < count && !occupied[mySlot])
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

    private static Vector3 Snap(Vector3 v)
    {
        v.x = Mathf.Round(v.x * 1000f) / 1000f;
        v.y = Mathf.Round(v.y * 1000f) / 1000f;
        v.z = Mathf.Round(v.z * 1000f) / 1000f;
        return v;
    }
}
