using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
     [Header("Players (order of turns)")]
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Dice")]
    public Dice dice; // در Inspector ست شود

    public PlayerController CurrentPlayer => players.Count > 0 ? players[currentPlayerIndex] : null;

    private int currentPlayerIndex = 0;
    private int lastDice = 0;
    private Token pendingSelected = null;

    private void Awake()
    {
        if (dice == null)
            Debug.LogError("[GameManager] DiceController ست نشده.");

        // هر Player باید به همین GameManager لینک باشد
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
        currentPlayerIndex = Mathf.Clamp(index, 0, players.Count - 1);
        lastDice = 0;
        pendingSelected = null;
        Debug.Log($"نوبت: {CurrentPlayer.playerName}");
    }

    private void HandleDiceRolled(int steps)
    {
        lastDice = steps;
        Debug.Log($"[{CurrentPlayer.playerName}] مقدار تاس: {steps}. حالا روی یکی از مهره‌های {CurrentPlayer.playerName} کلیک کن.");
    }

    // از Token صدا زده می‌شود
    public void OnTokenSelected(Token token)
    {
        if (token == null) return;
        if (token.owner != CurrentPlayer) return;
        if (lastDice <= 0)
        {
            Debug.Log("اول تاس بریز، بعد مهره رو انتخاب کن.");
            return;
        }

        if (CurrentPlayer.MoveToken(token, lastDice))
        {
            pendingSelected = token;
            StartCoroutine(WaitAndNextTurn());
        }
    }

    private IEnumerator WaitAndNextTurn()
    {
        // صبر تا پایان حرکت هر مهره‌ی فعلی
        while (CurrentPlayer.IsMoving())
            yield return null;

        // TODO: قوانین ویژه (جایزه گرفتن 6، خوردن مهره‌ی حریف، ورود به خانه‌ی پایان...) را اینجا اعمال کن

        // نوبت بعدی
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        SetCurrentPlayer(currentPlayerIndex);
    }
}
