using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;   // assign in Inspector
    public Dice dice;                 // assign in Inspector (the same dice used by humans)
    public BoardManager boardManager; // assign in Inspector
    public PlayerController self;     // this AI's PlayerController (assign this same object)

    [Header("Timing")]
    public float rollDelay = 0.6f;    // wait before rolling
    public float selectDelay = 0.6f;  // wait before selecting a token

    [Header("Behavior")]
    public bool preferEnterOnSix = true;  // if dice=6 and token off-board exists → enter
    public bool preferFarthestAdvance = true; // otherwise pick token that advances the most

    // internal
    private bool awaitingMyRollResult = false;
    private int lastRolledValue = 0;

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
        if (dice != null)
            dice.OnDiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        if (dice != null)
            dice.OnDiceRolled -= OnDiceRolled;
    }

    private void Update()
    {
        if (gameManager == null || self == null) return;

        // Only act on our turn
        if (gameManager.CurrentPlayer != self) return;

        // If any token is still moving, wait
        if (self.IsMoving()) return;

        // If we are already awaiting a roll result (i.e., we rolled), do nothing
        if (awaitingMyRollResult) return;

        // If GM از قبل تاس رو لاک کرده یا هنوز منتظر انتخاب انسانه، ممکنه Roll مجاز نباشه.
        // چون نمی‌خوایم فایل GM رو تغییر بدیم، ساده‌ترین راه: فقط وقتی GM آخرین تاس را مصرف/ریست می‌کند،
        // ما رول می‌کنیم. با یک تاخیر کوچک:
        StartCoroutine(AIRollAfterDelay());
    }

    private IEnumerator AIRollAfterDelay()
    {
        awaitingMyRollResult = true; // lock to avoid duplicate rolls
        yield return new WaitForSeconds(rollDelay);

        // Try to roll: اگر GM دکمه‌ی UI دارد و با متدی مثل OnDiceButtonPressed قفل می‌کند، بهتر است همان را صدا بزنیم.
        // اگر چنین متدی نداری، مستقیماً dice.RollDice() را صدا بزن.
        bool rolled = false;

        // Try to find a public method on GM via reflection (optional safety). Commented out to keep simple.
        // Otherwise directly roll:
        if (dice != null)
        {
            dice.RollDice();
            rolled = true;
        }

        if (!rolled)
        {
            Debug.LogWarning("[AI] Could not roll the dice (no method wired).");
            awaitingMyRollResult = false; // allow retry
        }
    }

    private void OnDiceRolled(int value)
    {
        // Only react if it's our turn
        if (gameManager == null || gameManager.CurrentPlayer != self) return;

        lastRolledValue = value;
        StartCoroutine(AISelectAfterDelay(value));
    }

    private IEnumerator AISelectAfterDelay(int value)
    {
        yield return new WaitForSeconds(selectDelay);

        var tokens = self.GetTokens();
        if (tokens == null || tokens.Count == 0)
        {
            awaitingMyRollResult = false;
            yield break;
        }

        Token choice = ChooseToken(tokens, value);
        if (choice == null)
        {
            // No legal move → GM logic should pass the turn automatically
            awaitingMyRollResult = false;
            yield break;
        }

        // Signal selection to GameManager (same as human click)
        gameManager.OnTokenSelected(choice);

        // We selected; now wait until GM finishes move/turn.
        // We'll release awaiting flag when Update() sees it's our turn again or not.
        StartCoroutine(ReleaseAwaitingWhenDone());
    }

    private IEnumerator ReleaseAwaitingWhenDone()
    {
        // wait a bit, then free the flag; GM will manage extra turn on six / pass turn
        while (self.IsMoving())
            yield return null;

        awaitingMyRollResult = false;
    }

    private Token ChooseToken(List<Token> tokens, int diceVal)
    {
        Token best = null;

        // Rule: if dice == 6 and any off-board token exists and we prefer entering, do that.
        if (preferEnterOnSix && diceVal == 6)
        {
            foreach (var t in tokens)
            {
                if (t == null) continue;
                if (!t.isOnBoard && IsLegalMove(t, diceVal))
                    return t; // enter immediately on first off-board token
            }
        }

        // Otherwise pick an on-board token with best advancement
        int bestScore = int.MinValue;

        foreach (var t in tokens)
        {
            if (t == null) continue;
            if (t.isMoving) continue;

            if (!IsLegalMove(t, diceVal)) continue;

            int score;
            if (!t.isOnBoard)
            {
                // only legal if dice == 6; score as entering
                score = 1000; // entering is usually strong; tweak if needed
            }
            else
            {
                // prefer farther progress along flattened path
                int projected = t.currentTileIndex + diceVal;
                score = preferFarthestAdvance ? projected : Random.Range(0, 100);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private bool IsLegalMove(Token token, int diceVal)
    {
        // Match your current rules in GameManager:
        // - If token is off-board, only legal if dice == 6
        if (!token.isOnBoard)
            return diceVal == 6;

        // On-board: assume legal (your GM/PlayerController will clamp/prevent overflow)
        return true;
    }
}
