using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class AIController : MonoBehaviour
{
  [Header("References")]
    public GameManager gameManager;   // assign in Inspector
    public Dice dice;                 // assign in Inspector (same dice)
    public PlayerController self;     // this AI's PlayerController

    [Header("Timing")]
    public float rollDelay = 0.6f;    // wait before rolling
    public float selectDelay = 0.6f;  // wait before selecting a token

    [Header("Behavior")]
    public bool preferEnterOnSix = true;        // enter when 6 if start is free
    public bool preferFarthestAdvance = true;   // otherwise choose farthest

    // internal
    private bool awaitingMyRollResult = false;

    private void Reset()
    {
        self = GetComponent<PlayerController>();
    }

    private void Awake()
    {
        if (self == null) self = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (dice != null) dice.OnDiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        if (dice != null) dice.OnDiceRolled -= OnDiceRolled;
    }

    private void Update()
    {
        if (gameManager == null || self == null) return;

        // فقط نوبت خودِ AI
        if (gameManager.CurrentPlayer != self) return;

        // اگر مهره‌ای در حال حرکت است، صبر کن
        if (self.IsMoving()) return;

        // اگر در انتظار نتیجه‌ی رول قبلی هستیم، صبر
        if (awaitingMyRollResult) return;

        // اگر تاس از دید GM مجاز است، رول کن
        if (gameManager.CanRoll())
            StartCoroutine(AIRollAfterDelay());
    }

    private IEnumerator AIRollAfterDelay()
    {
        awaitingMyRollResult = true; // جلوگیری از رول‌های تکراری
        yield return new WaitForSeconds(rollDelay);

        // دوباره چک کن، شاید وسط تاخیر نوبت عوض شده
        if (gameManager == null || gameManager.CurrentPlayer != self || !gameManager.CanRoll())
        {
            awaitingMyRollResult = false;
            yield break;
        }

        if (dice != null)
        {
            dice.Roll(); // مطمئن شو متد صحیح همینه (Roll/RollDice)
        }
        else
        {
            Debug.LogWarning("[AI] Dice reference is missing.");
            awaitingMyRollResult = false;
        }
    }

    private void OnDiceRolled(int value)
    {
        // فقط واکنش در نوبت خودمان
        if (gameManager == null || gameManager.CurrentPlayer != self) return;

        StartCoroutine(AISelectAfterDelay(value));
    }

    private IEnumerator AISelectAfterDelay(int value)
    {
        yield return new WaitForSeconds(selectDelay);

        if (gameManager == null || self == null) { awaitingMyRollResult = false; yield break; }

        var tokens = self.Tokens != null ? new List<Token>(self.Tokens) : null;
        if (tokens == null || tokens.Count == 0)
        {
            awaitingMyRollResult = false;
            yield break;
        }

        // از بین حرکت‌های قانونی انتخاب کن
        Token choice = ChooseToken(tokens, value);

        if (choice != null)
        {
            gameManager.OnTokenSelected(choice);
            // تا پایان حرکت/مدیریت نوبت صبر کن
            StartCoroutine(ReleaseAwaitingWhenDone());
        }
        else
        {
            // هیچ حرکت قانونی نداریم → GM خودش پاس می‌ده
            awaitingMyRollResult = false;
        }
    }

    private IEnumerator ReleaseAwaitingWhenDone()
    {
        // صبر تا حرکت/نوبت مدیریت شود
        while (self != null && self.IsMoving())
            yield return null;

        // یه فریم برای هماهنگی با GM
        yield return null;
        awaitingMyRollResult = false;
    }

    // ===================== انتخاب مهره =====================

    private Token ChooseToken(List<Token> tokens, int diceVal)
    {
        // لیست حرکت‌های قانونی با استناد به GM
        var movable = tokens
            .Where(t => t != null && gameManager.IsLegalMove(self, t, diceVal))
            .ToList();

        if (movable.Count == 0) return null;

        bool hasHomeTokens    = tokens.Any(t => t != null && !t.isOnBoard);
        bool startOccupiedBySelf = tokens.Any(t => t != null && t.isOnBoard && t.currentTileIndex == 0);

        // ۱) اگر ۶ آمده:
        if (diceVal == 6)
        {
            if (startOccupiedBySelf)
            {
                // اگر خانه‌ی شروع اشغال است، مهره‌ی روی Start را اول حرکت بده (درب را باز کن)
                var startTok = movable.FirstOrDefault(t => t.currentTileIndex == 0);
                if (startTok != null) return startTok;

                // اگر به هر دلیل حرکتش قانونی نبود، بیفت سراغ بهترینِ دیگر
                return ChooseBest(movable, diceVal);
            }
            else
            {
                // Start خالی است
                if (preferEnterOnSix && hasHomeTokens)
                {
                    // یکی از مهره‌های خانه را وارد کن (فقط اگر قانونی باشد که هست چون dice=6)
                    var offBoard = tokens.FirstOrDefault(t => t != null && !t.isOnBoard);
                    if (offBoard != null && gameManager.IsLegalMove(self, offBoard, diceVal))
                        return offBoard;
                }

                // اگر نخواستیم وارد کنیم، یکی از روی برد را انتخاب کن
                return ChooseBest(movable, diceVal);
            }
        }

        // ۲) غیر از ۶ → فقط از روی برد انتخاب کن
        return ChooseBest(movable, diceVal);
    }

    private Token ChooseBest(List<Token> movable, int diceVal)
    {
        // اولویت‌ها:
        // A) اگر مهره‌ای روی Start هست (و قانونی) همونو ببر تا گره‌ی Start باز شه
        var startTok = movable.FirstOrDefault(t => t.currentTileIndex == 0);
        if (startTok != null) return startTok;

        // B) اگر با این حرکت می‌تونیم حریف رو بزنیم، همونو انتخاب کن
        foreach (var t in movable)
            if (LandsOnEnemy(t, diceVal))
                return t;

        // C) در غیر این صورت، کسی که جلوتره/پیشرفت بیشتری داره
        if (preferFarthestAdvance)
            return movable.OrderByDescending(t => t.currentTileIndex + diceVal).First();
        else
            return movable[Random.Range(0, movable.Count)];
    }

    // تخمین سادهٔ فرود آمدن روی دشمن (ایمن‌بودن خانهٔ شروع لحاظ شده)
    private bool LandsOnEnemy(Token me, int steps)
    {
        if (me == null || self == null) return false;

        int targetIndex = me.isOnBoard ? me.currentTileIndex + steps : 0; // ورود با ۶ → 0
        if (targetIndex == 0) return false; // خانه شروع را امن درنظر می‌گیریم

        foreach (var p in gameManager.players)
        {
            if (p == null || p == self) continue;
            foreach (var ot in p.Tokens)
            {
                if (ot == null || !ot.isOnBoard) continue;
                if (ot.currentTileIndex == targetIndex)
                {
                    // اگر خانه امن پیچیده داری، می‌تونی اینجا هم چک اضافه کنی
                    return true;
                }
            }
        }
        return false;
    }
}
