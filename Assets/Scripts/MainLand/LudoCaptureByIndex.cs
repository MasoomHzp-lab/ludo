using System.Collections.Generic;
using UnityEngine;

public class LudoCaptureByIndex : MonoBehaviour
{
       [Header("References")]
    public List<PlayerController> players = new List<PlayerController>();
    public BoardManager boardManager;

    [Header("Rules")]
    public bool allowCaptureOnHomePath = false;   // معمولا false
    public bool allowCaptureOnStartTiles = false; // false یعنی خانه‌های شروع امن‌اند
    [Tooltip("RingId های امن (ستاره‌ها) روی لوپ مشترک.")]
    public List<int> safeLoopIndices = new List<int>();
    public bool allowStackSameColor = true;       // هم‌رنگ‌ها می‌تونن هم‌خانه شوند

    [Header("Same-tile precision")]
    public bool enforceCenterSnap = true;         // برای اسنپ دقیق پیشنهاد می‌شود true باشد
    public float ringCenterSnap = 0.08f;          // ~8cm

    [Header("Auto-discovery")]
    public bool autoFindPlayersIfEmpty = true;
    public float autoFindInterval = 1.0f;

    // snapshots
    private readonly Dictionary<Token, bool> lastMoving = new Dictionary<Token, bool>();
    private readonly Dictionary<Token, int>  lastIndex  = new Dictionary<Token, int>();
    private float _findTimer;

    private void OnEnable()
    {
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();
        PrimePlayersAndTokens();
    }

    private void Update()
    {
        // کشف خودکار پلیرها اگر لیست خالی است
        if (autoFindPlayersIfEmpty && (players == null || players.Count == 0))
        {
            _findTimer -= Time.deltaTime;
            if (_findTimer <= 0f)
            {
                _findTimer = autoFindInterval;
                var found = FindObjectsOfType<PlayerController>();
                if (found != null && found.Length > 0)
                {
                    players = new List<PlayerController>(found);
                    PrimePlayersAndTokens();
                    Debug.Log("[Capture] Auto-discovered players.");
                }
            }
        }

        if (players == null || players.Count == 0) return;
        if (boardManager == null || boardManager.commonPath == null || boardManager.commonPath.Count == 0) return;

        foreach (var p in players)
        {
            if (p == null) continue;
            var tokens = p.GetTokens();
            if (tokens == null) continue;

            foreach (var t in tokens)
            {
                if (t == null) continue;

                if (!lastMoving.ContainsKey(t)) lastMoving[t] = t.isMoving;
                if (!lastIndex.ContainsKey(t))  lastIndex[t]  = t.currentTileIndex;

                bool wasMoving = lastMoving[t];
                int  wasIndex  = lastIndex[t];

                bool nowMoving = t.isMoving;
                int  nowIndex  = t.currentTileIndex;

                bool landedNow = (wasMoving && !nowMoving) || ((nowIndex != wasIndex) && !nowMoving);

                if (landedNow)
                    OnTokenLanded(t);

                lastMoving[t] = nowMoving;
                lastIndex[t]  = nowIndex;
            }
        }
    }

    private void OnTokenLanded(Token landed)
    {
        if (landed == null || !landed.isOnBoard || boardManager == null) return;

        int commonCount = (boardManager.commonPath != null) ? boardManager.commonPath.Count : 0;
        if (commonCount <= 0) return;

        bool moverOnHomePath = (landed.currentTileIndex >= commonCount);

        // در مسیر خانۀ پایان معمولا کپچر نداریم
        if (moverOnHomePath && !allowCaptureOnHomePath) return;

        // RingId مهاجم
        if (!TryGetRingId(landed, out int moverRing)) return;

        // ــ اسنپ مهاجم به مرکز خانه (برای اینکه دقیقاً "روی هم" دیده شوند)
        SnapToRingCenter(landed, moverRing);

        foreach (var p in players)
        {
            if (p == null) continue;
            var tokens = p.GetTokens();
            if (tokens == null) continue;

            foreach (var other in tokens)
            {
                if (other == null || other == landed) continue;
                if (!other.isOnBoard) continue;

                bool otherOnHomePath = (other.currentTileIndex >= commonCount);
                if ((moverOnHomePath || otherOnHomePath) && !allowCaptureOnHomePath)
                    continue;

                bool sameOwner = (other.owner != null && landed.owner != null && other.owner == landed.owner);
                bool sameColor = other.color == landed.color ||
                                 (other.owner != null && landed.owner != null &&
                                  other.owner.color == landed.owner.color);
                if ((sameOwner || sameColor) && allowStackSameColor)
                    continue;

                if (!TryGetRingId(other, out int otherRing))
                    continue;

                if (otherRing != moverRing)
                    continue;

                if (enforceCenterSnap && !BothNearRingCenter(moverRing, landed, other))
                    continue;

                if (IsSafeLoopIndex(otherRing, commonCount))
                    continue;

                if (!allowCaptureOnStartTiles && IsStartRing(otherRing, commonCount))
                    continue;

                // --- کُشتن: قربانی برگردد خانه (اولین اسلات خالی)
                SendHomeRobust(other);

                Debug.Log($"[Capture] {landed.owner?.playerName} captured {other.owner?.playerName}'s token.");
            }
        }
    }

    // ===== Helpers =====

    private bool TryGetRingId(Token t, out int ringId)
    {
        ringId = -1;
        if (t == null || t.owner == null) return false;
        var bm = t.owner.boardManager != null ? t.owner.boardManager : boardManager;
        if (bm == null) return false;
        return bm.TryGetRingId(t.owner.color, t.currentTileIndex, out ringId);
    }

    private void SnapToRingCenter(Token t, int ringId)
    {
        if (!enforceCenterSnap) return;
        var list = boardManager?.commonPath;
        if (list == null || ringId < 0 || ringId >= list.Count) return;
        var center = list[ringId];
        if (center == null) return;
        // اگر فیزیک داری، برخوردها را لحظه‌ای غیرفعال کن تا روی هم بنشینند
        var col = t.GetComponent<Collider>();
        if (col) col.enabled = false;
        t.transform.position = center.position;
        if (col) col.enabled = true;
    }

    private bool BothNearRingCenter(int ringId, Token a, Token b)
    {
        var list = boardManager?.commonPath;
        if (list == null || ringId < 0 || ringId >= list.Count) return true;
        var center = list[ringId];
        if (center == null) return true;

        float thresholdSq = ringCenterSnap * ringCenterSnap;
        float da = (a.transform.position - center.position).sqrMagnitude;
        float db = (b.transform.position - center.position).sqrMagnitude;

        if (da > thresholdSq) { Debug.Log($"[SameTile] A far from center ({Mathf.Sqrt(da):0.000})"); return false; }
        if (db > thresholdSq) { Debug.Log($"[SameTile] B far from center ({Mathf.Sqrt(db):0.000})"); return false; }
        return true;
    }

    private bool IsSafeLoopIndex(int idx, int commonCount)
    {
        if (commonCount <= 0) return false;
        int m = Mod(idx, commonCount);
        return safeLoopIndices != null && safeLoopIndices.Contains(m);
    }

    private bool IsStartRing(int ringId, int commonCount)
    {
        if (boardManager == null || commonCount <= 0) return false;
        int r = Mod(boardManager.redStartIndex, commonCount);
        int b = Mod(boardManager.blueStartIndex, commonCount);
        int g = Mod(boardManager.greenStartIndex, commonCount);
        int y = Mod(boardManager.yellowStartIndex, commonCount);
        int m = Mod(ringId, commonCount);
        return m == r || m == b || m == g || m == y;
    }

    private static int Mod(int a, int m)
    {
        if (m <= 0) return a;
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    // --- Robust home send (اولین اسلات خالی + ریست state)
    private void SendHomeRobust(Token token)
    {
        var owner = token.owner;
        if (owner == null || owner.spawnPoints == null || owner.spawnPoints.Count == 0)
        {
            Debug.LogWarning("[LudoCaptureByIndex] Owner has no spawn points; cannot send home.");
            return;
        }

        // اگر انیمیشن/کوروتینی در حال حرکت دارد، اینجا قطعش کن (در صورت وجود API)
        // token.StopAllCoroutines(); // اگر مجاز است

        token.isMoving = false;
        token.isOnBoard = false;
        token.currentTileIndex = -1;

        // اگر فیلدهای دیگری داری که مربوط به مسیر/هوم‌پث‌اند، اینجا ریست کن:
        // token.enteredHomePath = false;
        // token.pathProgress = 0f;

        int slot = FindFirstFreeHomeSlot(owner, token);
        Transform target = owner.spawnPoints[Mathf.Clamp(slot, 0, owner.spawnPoints.Count - 1)];

        // detach از هر والد
        token.transform.SetParent(null, true);
        // برای اطمینان از روی هم نیفتادن فیزیکی:
        var col = token.GetComponent<Collider>();
        if (col) col.enabled = false;
        token.transform.position = target.position;
        token.transform.rotation = Quaternion.identity;
        if (col) col.enabled = true;

        // اگر Token متد ریست خودش را دارد:
        // token.ResetToHome();

        Debug.Log($"[SendHome] {token.name} -> {owner.color} spawn slot {slot}");
    }

    private int FindFirstFreeHomeSlot(PlayerController owner, Token exceptThis = null)
    {
        int n = owner.spawnPoints.Count;
        bool[] occ = new bool[n];

        foreach (var t in owner.GetTokens())
        {
            if (t == null || t == exceptThis) continue;
            if (t.isOnBoard) continue;

            int idx = NearestSpawnIndex(owner, t.transform.position);
            if (idx >= 0 && idx < n) occ[idx] = true;
        }

        for (int i = 0; i < n; i++)
            if (!occ[i] && owner.spawnPoints[i] != null) return i;

        // اگر همه پر بود، نزدیک‌ترین رو انتخاب کن تا overlap کمتر باشه
        int best = NearestSpawnIndex(owner, exceptThis != null ? exceptThis.transform.position : owner.spawnPoints[0].position);
        return best >= 0 ? best : 0;
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

    private void PrimePlayersAndTokens()
    {
        if (players == null) players = new List<PlayerController>();
        foreach (var p in players)
        {
            if (p == null) continue;
            var tokens = p.GetTokens();
            if (tokens == null) continue;
            foreach (var t in tokens)
            {
                if (t == null) continue;
                if (!lastMoving.ContainsKey(t)) lastMoving.Add(t, t.isMoving);
                if (!lastIndex.ContainsKey(t))  lastIndex.Add(t, t.currentTileIndex);
            }
        }
        _findTimer = autoFindInterval;
    }
}
