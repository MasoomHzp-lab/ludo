using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Players (order of turns)")]
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Dice")]
    public Dice dice; // must expose OnDiceRolled(int)


    public PlayerController CurrentPlayer => players.Count > 0 ? players[currentPlayerIndex] : null;

    // turn/dice state
    private int currentPlayerIndex = 0;
    private int lastDice = 0;
    private bool rolledSix = false;

    // rules
    [Header("Ludo Rules")]
    public bool enterOnlyOnSix = true;        // must roll 6 to enter from home
    public bool consumeOneStepOnEntry = true; // after entering with 6, remaining steps = 5

    private Token pendingSelected = null;

    private void Awake()
    {
        if (dice == null)
            Debug.LogError("[GameManager] Dice is not assigned.");

        // link every PlayerController back to this GameManager
        foreach (var p in players)
            if (p != null && p.gameManager != this)
                p.gameManager = this;
    }

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
        SetCurrentPlayer(0);
    }

    private void SetCurrentPlayer(int index)
    {
        currentPlayerIndex = Mathf.Clamp(index, 0, Mathf.Max(0, players.Count - 1));
        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;

        if (CurrentPlayer != null)
            Debug.Log($"[Turn] {CurrentPlayer.playerName}");
    }

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

        Debug.Log($"[{CurrentPlayer.playerName}] rolled: {steps}. Click one of {CurrentPlayer.playerName}'s tokens.");
    }

    // Called by Token.OnMouseDown()
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

        // --- ENTER ONLY ON 6, AND STOP THERE ---
        if (!token.isOnBoard && lastDice == 6)
        {
            // place on start and DO NOT move further
            var bm = token.owner != null ? token.owner.boardManager : null;
            if (bm == null)
            {
                Debug.LogError("[GM] BoardManager missing on token.owner.");
                return;
            }

            // put token on the first tile of its path (start)
            token.transform.position = bm.GetTilePosition(token.color, 0);
            token.isOnBoard = true;
            token.currentTileIndex = 0;
            // PlayTokenSound();



            // keep extra turn for rolling 6
            Debug.Log($"[GM] {CurrentPlayer.playerName} entered the board (6). Extra turn.");
            // reset dice so the same player can roll again
            lastDice = 0;
            // نوبت را عوض نمی‌کنیم؛ همین‌جا برمی‌گردیم
            return;
        }

        // If token is still off-board and dice wasn't 6 → ignore
        if (!token.isOnBoard && lastDice != 6)
        {
            Debug.Log("[GM] Need a 6 to enter the board.");
            return;
        }

        // --- NORMAL MOVE ON BOARD ---
        int stepsToMove = lastDice; // no entry-consumption anymore
        bool accepted = CurrentPlayer.MoveToken(token, stepsToMove);
        if (!accepted)
        {
            Debug.LogWarning("[GM] Move rejected (token busy or invalid).");
            return;
        }

        // after movement, advance turn or keep (if 6)
        StartCoroutine(WaitAndAdvanceTurn());
    }


    private IEnumerator WaitAndAdvanceTurn()
    {
        // wait until current player's tokens finish moving
        while (CurrentPlayer != null && CurrentPlayer.IsMoving())
            yield return null;

        // Extra turn on 6: keep the same player
        if (rolledSix)
        {
            Debug.Log($"[GM] Extra turn for {CurrentPlayer.playerName} (rolled 6).");
            // reset for the next roll by the same player
            lastDice = 0;
            rolledSix = false;
            pendingSelected = null;
            yield break; // do NOT advance turn
        }

        // otherwise pass the turn
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(currentPlayerIndex);

        // reset state
        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;
    }

    private bool HasLegalMove(PlayerController p, int diceVal)
    {
        if (p == null) return false;
        var tokens = p.GetTokens();
        if (tokens == null || tokens.Count == 0) return false;

        bool anyOnBoard = false;

        foreach (var t in tokens)
        {
            if (t == null) continue;
            if (t.isMoving) continue;

            if (t.isOnBoard)
            {
                anyOnBoard = true;

                // می‌تونی این‌جا سخت‌گیرتر هم باشی (مثلاً چکِ جا داشتن در مسیر)
                // فعلاً ساده: اگر روی برد است و در حال حرکت نیست، حرکت قانونی دارد
                return true;
            }
        }

        if (!anyOnBoard)
            return diceVal == 6;

        return false;
    }

    // اگر حرکت قانونی نبود، نوبت را بده نفر بعدی (۶ هم نیامده)
    private IEnumerator PassTurnImmediately()
    {
        yield return null;

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        lastDice = 0;
        rolledSix = false;
        pendingSelected = null;

        SetCurrentPlayer(currentPlayerIndex);
    }

    public void PlayTokenSound()
    {

        AudioManager.Instance.PlaySFX(AudioManager.Instance.TokenSound);


    }
}
