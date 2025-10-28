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
    }

    /// <summary>
    /// هوک رول از طرف Dice
    /// </summary>
    private void HandleDiceRolled(int steps)
    {
        if (CurrentPlayer == null) return;

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

        // بعد از اتمام حرکت، نوبت را جلو ببریم یا نگه داریم
        StartCoroutine(WaitAndAdvanceTurn());
        yield break;
    }

    /// <summary>
    /// این هوک را با سیستم حرکت واقعی خودت جایگزین کن.
    /// الان صرفاً یک شبیه‌سازی ۰.۷s می‌کند (برای اینکه پروژه نپره).
    /// </summary>
    private IEnumerator PerformMove(Token token, int steps)
    {
        // TODO: اینجا به متد واقعی حرکتت وصل شو، مثلاً:
        // yield return StartCoroutine(token.MoveSteps(steps));
        // یا owner: yield return StartCoroutine(token.owner.MoveToken(token, steps));
        // یا هر متدی که در پروژه‌ات داری.

        // شبیه‌سازی ساده:
        token.isMoving = true;
        CurrentPlayer?.PlayTokenSound();
        yield return new WaitForSeconds(0.7f);
        token.isMoving = false;

        // اگر روی ۶ ورود از خانه داریم، می‌توانی اینجا منطق ورود واقعی را جایگزین کنی
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
            yield break; // نوبت عوض نشود
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
        yield return null;
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(currentPlayerIndex);
        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;
    }

    // ====== بررسی‌های قانونی ساده (متناسب با دیتاهای Token/PlayerController پروژه‌ات) ======
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

        // اگر هنوز روی بورد نیست:
        if (!token.isOnBoard)
            return (enterOnlyOnSix && steps == 6);

        // اگر روی بورد است، حداقل حرکت ۱ و توکن در حال حرکت نباشد
        if (steps <= 0) return false;
        if (token.isMoving) return false;

        // اگر می‌خواهی قوانین دقیق‌تر (ورود به خانه، رد نشدن از پایان مسیر، بلاک‌ها و…) را اعمال کنی
        // می‌توانی اینجا از BoardManager مسیر را چک کنی. فعلاً ساده نگه می‌داریم.
        return true;
    }
}
