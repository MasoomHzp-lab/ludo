using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{

     // ===== Singleton (اختیاری) =====
    public static GameManager I;
    private void Awake()
    {
        I = this;

        if (dice == null)
            Debug.LogError("[GameManager] Dice is not assigned.");

        // back-reference برای هر PlayerController
        foreach (var p in players)
            if (p != null && p.gameManager != this)
                p.gameManager = this;
    }
    // ===============================

    [Header("Players (order of turns)")]
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Dice")]
    public Dice dice; // باید OnDiceRolled(int) داشته باشد

    // بازیکن فعلی
    public PlayerController CurrentPlayer => players.Count > 0 ? players[currentPlayerIndex] : null;

    // وضعیت نوبت/تاس
    private int currentPlayerIndex = 0;
    private int lastDice = 0;
    private bool rolledSix = false;

    // قوانین پایه لودو
    [Header("Ludo Rules")]
    [Tooltip("ورود به زمین فقط با ۶")]
    public bool enterOnlyOnSix = true;

    [Tooltip("اگر ۶ بیاید نوبت اضافه بماند")]
    public bool extraTurnOnSix = true;

    // کنترل انتخاب توکن
    private Token pendingSelected = null;

    // قفل‌کردن تاس
    [SerializeField] private bool canRoll = true;
    public bool CanRoll() => canRoll;

    private void OnEnable()
    {
        if (dice != null)
            dice.OnDiceRolled += HandleDiceRolled;
    }

    private void OnDisable()
    {
        if (dice != null)
            dice.OnDiceRolled -= HandleDiceRolled;
    }

    private void Start()
    {
        ApplyPlayerCountFromSettings();   // ← تعداد نفرات را از MatchSettings می‌گیرد و اعمال می‌کند
        SetCurrentPlayer(0);
        canRoll = true;                   // بازیکن شروع‌کننده می‌تواند تاس بیندازد
    }

    /// <summary>
    /// تعداد بازیکن‌ها را از MatchSettings گرفته و لیست players را کوتاه/فعال می‌کند.
    /// </summary>
    private void ApplyPlayerCountFromSettings()
    {
        // فقط بازیکن‌های معتبر
        players = players.Where(p => p != null && p.gameObject != null).ToList();

        int desired = players.Count;
        if (MatchSettings.Instance != null)
            desired = Mathf.Clamp(MatchSettings.Instance.playerCount, 2, 4);

        // فعال/غیرفعال کردن بر اساس desired
        for (int i = 0; i < players.Count; i++)
        {
            bool active = (i < desired) && players[i] != null;
            if (players[i] != null)
                players[i].gameObject.SetActive(active);
        }

        // لیست را کوتاه کن تا players.Count دقیق شود
        if (players.Count > desired)
            players = players.GetRange(0, desired);

        // back-reference دوباره
        foreach (var p in players)
            if (p != null && p.gameManager != this)
                p.gameManager = this;
    }

    private void SetCurrentPlayer(int index)
    {
        currentPlayerIndex = Mathf.Clamp(index, 0, Mathf.Max(0, players.Count - 1));
        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;

        if (CurrentPlayer != null)
        {
            // اگر dice به بازیکن فعلی نیاز دارد:
            if (dice != null) dice.currentPlayer = CurrentPlayer;
            Debug.Log($"[Turn] {CurrentPlayer.playerName}");
        }

        // خاموش کردن درخشش همه بازیکن‌ها
        foreach (var p in players)
        {
            if (p == null || p.Tokens == null) continue;
            foreach (var t in p.Tokens)
            {
                var glow = t.GetComponent<OutLineGlow>();
                if (glow != null) glow.SetGlow(false);
            }
        }

        // روشن کردن درخشش بازیکن فعلی
        if (CurrentPlayer != null && CurrentPlayer.Tokens != null)
        {
            foreach (var t in CurrentPlayer.Tokens)
            {
                var glow = t.GetComponent<OutLineGlow>();
                if (glow != null) glow.SetGlow(true);
            }
        }

        // بازیکن جدید می‌تواند تاس بیندازد
        canRoll = true;
    }

    /// <summary>
    /// هوک رول از طرف Dice
    /// </summary>
    private void HandleDiceRolled(int steps)
    {
        // اگر به هر دلیلی Dice قبل از چک CanRoll رویداد را فایر کرد، محافظت:
        if (!canRoll)
        {
            Debug.Log("[GM] Dice roll ignored (canRoll=false).");
            return;
        }

        if (CurrentPlayer == null) return;

        // پس از رول، تا پایان حرکت/پاس نوبت قفل شو
        canRoll = false;

        lastDice = steps;
        rolledSix = (steps == 6);

        if (!HasLegalMove(CurrentPlayer, lastDice))
        {
            Debug.Log("[GM] No legal move. Passing turn.");
            StartCoroutine(PassTurnImmediately());
            return;
        }

        Debug.Log($"[{CurrentPlayer.playerName}] rolled: {steps}. Select a token.");
        // حالا منتظر OnTokenSelected از طرف کلیک بازیکن می‌مانیم
    }

    /// <summary>
    /// از Token.OnMouseDown یا UI کال می‌شود.
    /// </summary>
    public void OnTokenSelected(Token token)
    {
        if (token == null) return;
        if (CurrentPlayer == null) return;
        if (token.owner != CurrentPlayer) return;

        if (lastDice <= 0)
        {
            Debug.Log("[GM] Roll the dice first, then select a token.");
            return;
        }

        // ورود فقط با ۶ (اگر روی بورد نیست)
        if (enterOnlyOnSix && !token.isOnBoard && lastDice != 6)
        {
            Debug.Log("[GM] You must roll a 6 to leave home.");
            return;
        }

        if (!IsLegalMove(CurrentPlayer, token, lastDice))
        {
            Debug.Log("[GM] Illegal move for this token.");
            return;
        }

        if (pendingSelected != null) return; // هنوز حرکت قبلی تمام نشده

        pendingSelected = token;
        StartCoroutine(PerformMoveRoutine(token, lastDice));
    }

    private IEnumerator PerformMoveRoutine(Token token, int steps)
    {
        // حرکت را به سیستم حرکت توکن بسپار (هوک)
        yield return StartCoroutine(PerformMove(token, steps));

        // ✅ بعد از اتمام حرکت، برخورد/کشتن را بررسی کن
        ResolveCaptures(token);

        // بعد از اتمام حرکت، نوبت را جلو ببریم یا نگه داریم
        yield return StartCoroutine(WaitAndAdvanceTurn());
    }

    private IEnumerator PerformMove(Token token, int steps)
    {
        if (token == null) yield break;

        // اگر مهره بیرونه و ۶ اومده: فقط وارد خانه‌ی شروع بشه و حرکت نکنه
        if (!token.isOnBoard && steps == 6)
        {
            token.currentTileIndex = 0;
            token.transform.position = CurrentPlayer.boardManager.GetTilePosition(CurrentPlayer.color, 0);
            token.isOnBoard = true;
            token.isMoving = false;
            yield break; // جایزه‌ی ۶ در WaitAndAdvanceTurn اعمال میشه
        }

        // حرکت عادی
        CurrentPlayer?.MoveToken(token, steps);

        // صبر تا حرکت تموم شه
        while (token != null && token.isMoving)
            yield return null;
    }

    private IEnumerator WaitAndAdvanceTurn()
    {
        // صبر تا حرکت تمام شود
        while (CurrentPlayer != null && CurrentPlayer.IsMoving())
            yield return null;

        // اگر ۶: نوبت اضافه بماند
        if (extraTurnOnSix && rolledSix)
        {
            Debug.Log($"[GM] Extra turn for {CurrentPlayer.playerName} (rolled 6).");
            lastDice = 0;
            rolledSix = false;
            pendingSelected = null;
            canRoll = true; // اجازه‌ی تاس دوباره برای همان بازیکن
            yield break;    // نوبت عوض نشود
        }

        // پاس نوبت
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(currentPlayerIndex);

        // ریست
        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;
    }

    private IEnumerator PassTurnImmediately()
    {
        // یک فریم تأخیر برای هم‌ترازی با UI
        yield return null;

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(currentPlayerIndex);

        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;

        // بازیکن جدید می‌تواند تاس بیندازد
        canRoll = true;
    }

    // ====== بررسی‌های قانونی ساده ======
    private bool HasLegalMove(PlayerController player, int steps)
    {
        if (player == null) return false;
        if (player.Tokens == null || player.Tokens.Count == 0) return false;

        // اگر ورود فقط با ۶ است و ۶ نیامده، حداقل باید یک توکن روی بورد باشد
        if (enterOnlyOnSix && steps != 6)
        {
            bool anyOnBoard = false;
            foreach (var t in player.Tokens)
                if (t != null && t.isOnBoard) { anyOnBoard = true; break; }
            if (!anyOnBoard) return false;
        }

        // اگر حتی یک توکن حرکت قانونی داشته باشد، true
        foreach (var t in player.Tokens)
            if (t != null && IsLegalMove(player, t, steps))
                return true;

        return false;
    }

    private bool IsLegalMove(PlayerController player, Token token, int steps)
    {
        if (player == null || token == null) return false;

        // ورود فقط با ۶
        if (!token.isOnBoard)
            return (enterOnlyOnSix && steps == 6);

        if (steps <= 0 || token.isMoving) return false;

        // قانون: باید دقیق برسد (از مسیر نگذرد)
        var path = player.boardManager.GetFullPath(player.color);
        int lastIndex = path.Count - 1;
        if (token.currentTileIndex + steps > lastIndex)
            return false;

        return true;
    }

    // ====== برخورد/کُشتن ======

    /// هم‌خانه بودن دو مهره (با تلورانس خیلی کم برای خطای ممیز)
    private bool AreOnSameTile(Token a, Token b)
    {
        if (a == null || b == null) return false;
        if (!a.isOnBoard || !b.isOnBoard) return false;
        return (a.owner != null && b.owner != null) &&
               (Vector3.SqrMagnitude(a.transform.position - b.transform.position) < 0.0001f);
    }

   private bool IsSafeTile(Token t)
{
    if (t == null || t.owner == null) return false;

    // قانون عمومی: خانه‌ی شروع هر رنگ امن است
    if (t.isOnBoard && t.currentTileIndex == 0) 
        return true;

    // اگر BoardManager متدهایی برای مسیر منزل/خانه‌های امن دارد، این‌ها را باز کن
    try
    {
        var bm = (t.owner != null) ? t.owner.boardManager : null;
        if (bm != null)
        {
            // نمونه‌های احتمالی—فقط اگر در پروژه‌ات وجود دارند، از حالت کامنت خارج کن:
            // if (bm.IsSafeTile(t.owner.color, t.currentTileIndex)) return true;
            // if (bm.IsInHomePath(t.owner.color, t.currentTileIndex)) return true;
            // اگر رنج مسیر منزل می‌ده: if (bm.IsHomeRange(t.owner.color, t.currentTileIndex)) return true;
        }
    }
    catch { /* نادیده بگیر */ }

    // وابستگی به GetTileObject/SafeTile یا نام‌گذاری حذف شد تا ارورها رفع شوند
    return false;
}


    /// اگر Token تازه‌حرکت‌کرده روی مهره‌ی حریف در خانه‌ی غیرایمن فرود بیاید، حریف را به خانه برگردان.
    private void ResolveCaptures(Token mover)
    {
        if (mover == null || !mover.isOnBoard) return;

        // خانه‌ی امن؟ هیچ برخوردی رخ نمی‌دهد
        if (IsSafeTile(mover)) return;

        foreach (var p in players)
        {
            if (p == null || p.Tokens == null) continue;
            foreach (var other in p.Tokens)
            {
                if (other == null || other == mover) continue;
                if (!other.isOnBoard) continue;

                if (AreOnSameTile(mover, other))
                {
                    // هم‌رنگ؟ (اگر قانون استک داری بعداً می‌افزاییم)
                    if (other.owner == mover.owner) continue;

                    // اگر خانه برای other امن باشد هم نکُش
                    if (IsSafeTile(other)) continue;

                    // کشتن
                    SendTokenHome(other);
                    Debug.Log($"[Capture] {mover.owner.playerName} captured {other.owner.playerName}'s token.");
                }
            }
        }
    }

    /// برگرداندن مهره به خانه (Home/Spawn)
    private void SendTokenHome(Token t)
    {
        if (t == null || t.owner == null) return;

        t.isOnBoard = false;
        t.isMoving = false;
        t.currentTileIndex = -1;

        // پیدا کردن یک اسپات خالی در خانه‌ی صاحب مهره
        var pc = t.owner;
        int spawnIdx = GetFreeHomeIndex(pc, t);
        if (spawnIdx < 0) spawnIdx = 0;

        if (pc != null && pc.spawnPoints != null && pc.spawnPoints.Count > 0)
        {
            var sp = pc.spawnPoints[Mathf.Clamp(spawnIdx, 0, pc.spawnPoints.Count - 1)];
            if (sp != null) t.transform.position = sp.position;
        }
    }

    /// یک خانه‌ی خالی در Home پیدا کن
    private int GetFreeHomeIndex(PlayerController pc, Token ignoreToken = null)
    {
        if (pc == null || pc.spawnPoints == null || pc.spawnPoints.Count == 0) return 0;

        for (int i = 0; i < pc.spawnPoints.Count; i++)
        {
            var spot = pc.spawnPoints[i];
            bool occupied = false;

            foreach (var tok in pc.Tokens)
            {
                if (tok == null || tok == ignoreToken) continue;
                if (!tok.isOnBoard && Vector3.SqrMagnitude(tok.transform.position - spot.position) < 0.0001f)
                {
                    occupied = true; break;
                }
            }
            if (!occupied) return i;
        }
        return -1;
    }


}
