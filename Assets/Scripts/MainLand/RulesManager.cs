using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RulesManager : MonoBehaviour
{

     [Header("Win")]
    public WinManager winManager;

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
    if (t == null || t.owner == null) return;

    var pc = t.owner;
    var bm = pc.boardManager;
    if (bm == null) return;

    var path = bm.GetFullPath(pc.color);
    if (path == null || path.Count == 0) return;

    int lastIndex = path.Count - 1;

    // طول مسیر و وضعیت فعلی مهره
    Debug.Log($"[Rules] FullPath length for {pc.color} = {path.Count}");
    Debug.Log($"[Rules] Token {t.name} idx={t.currentTileIndex}, last={lastIndex}, color={pc.color}");

    // ✅ از این اندیس به بعد، خونه‌های نهایی این رنگ حساب می‌شن
    int tokensPerPlayer = 4;
    int finishStartIndex = Mathf.Max(0, lastIndex - (tokensPerPlayer - 1)); 
    // مثال: 48 خانه → lastIndex = 47 → finishStartIndex = 44

    // اگر هنوز وارد محدوده‌ی خونه‌های آخر نشده، کاری نکن
    if (t.currentTileIndex < finishStartIndex)
        return;

    // اگر قبلاً به عنوان مهره‌ی تمام‌شده ثبت شده، دوباره کاری نکن
    if (finishedTokens.Contains(t))
        return;

    Debug.Log($"[Rules] FINISH zone triggered for {t.name} ({pc.color}) at idx={t.currentTileIndex}");

    // 🔹 پیدا کردن FinishBay مربوط به این رنگ
    var bay = GetBay(pc.color);

    if (bay != null && bay.slots != null && bay.slots.Count > 0)
    {
        // چند تا مهره از این پلیر قبلاً فینیش شدن؟
        int sameColorFinished = finishedTokens
            .Count(tok => tok != null && tok.owner == pc);

        // هر مهره روی اسلات بعدی می‌شینه: 0,1,2,3
        int slotIndex = Mathf.Clamp(sameColorFinished, 0, bay.slots.Count - 1);
        var slot = bay.slots[slotIndex];

        if (slot != null)
        {
            t.transform.position = slot.position;   // انتقال به خونه‌ی نهایی
        }
    }

    // این مهره دیگه روی برد مانع حساب نشه
    t.isMoving = false;
    t.isOnBoard = false;

    // ثبت به عنوان مهره‌ی فینیش‌شده
    finishedTokens.Add(t);
    Debug.Log($"[Rules] Registered FINISH for {t.name} ({pc.color}). Total finished for this color = " +
              finishedTokens.Count(tok => tok != null && tok.owner == pc));

    // خبر دادن به WinManager
    if (winManager != null)
    {
        Debug.Log("[Rules] Calling WinManager.RegisterFinishedToken for " + t.name);
        winManager.RegisterFinishedToken(t);
    }
    else
    {
        Debug.LogWarning("[Rules] winManager is NULL in inspector!");
    }
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
