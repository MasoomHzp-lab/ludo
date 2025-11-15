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

        if (turnTimer != null)
    {
        // وقتی تایمر تموم شد → نوبت بسوزه
       
    }


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

    [Header("Rules")]
    public RulesManager rules; // ← در Inspector ست کن


    [Header("Turn Timer")]
    public TurnTimer turnTimer; // در Inspector ست کن
    public float turnSeconds = 20f;

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
        ApplyPlayerCountFromSettings();
        SetCurrentPlayer(0);
        canRoll = true;

        // تعیین اسلات خانۀ اختصاصی مهره‌ها (برای بازگشت صحیح به Home)
        rules?.EnsureHomeSlotAssignedForAll(players);
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
            if (dice != null) dice.currentPlayer = CurrentPlayer;
            Debug.Log($"[Turn] {CurrentPlayer.playerName}");
        }

      // خاموش کردن انیمیشن همه مهره‌ها
    foreach (var p in players)
    {
        if (p == null || p.Tokens == null) continue;
        foreach (var t in p.Tokens)
        {
            if (t == null) continue;
            var animator = t.GetComponent<PieceAnimator>();
            if (animator != null) animator.SetActive(false);
        }
    }

    // روشن کردن انیمیشن مهره‌های بازیکن فعلی (درصورت وجود)
    if (CurrentPlayer != null && CurrentPlayer.Tokens != null)
    {
        foreach (var t in CurrentPlayer.Tokens)
        {
            if (t == null) continue;
            var animator = t.GetComponent<PieceAnimator>();
            if (animator != null) animator.SetActive(true);
        }
    }

        canRoll = true; // بازیکن جدید می‌تواند تاس بیندازد

        // شروع شمارش معکوس برای بازیکن جدید
        if (turnTimer != null)
            turnTimer.StartTimer(turnSeconds);

    }

    /// <summary>
    /// هوک رول از طرف Dice
    /// </summary>
    private void HandleDiceRolled(int steps)
    {
        if (!canRoll)
        {
            Debug.Log("[GM] Dice roll ignored (canRoll=false).");
            return;
        }

        if (turnTimer != null && turnTimer.IsRunning)
         turnTimer.CancelTimer();


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

        rules?.HandleIfFinished(token);

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

            if (turnTimer != null)
                turnTimer.StartTimer(turnSeconds);
                    
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

        if (turnTimer != null)
          turnTimer.StartTimer(turnSeconds);


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

    // public تا AI بتواند صدا بزند
    public bool IsLegalMove(PlayerController player, Token token, int steps)
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

    public void OnTurnTimeout()
    {
        // اگر هنوز اجازه‌ی تاس دادن داشته و کاری نکرده، نوبتش می‌سوزه
        if (canRoll)
        {
            Debug.Log("[GM] Turn timeout → passing turn.");
            StartCoroutine(PassTurnImmediately());
        }
        else
        {
            // اگر از قبل رول کرده بوده و تایمر هنوز روشن مونده، بی‌اثر.
            Debug.Log("[GM] Timeout ignored (already rolled).");
        }
    }


}
