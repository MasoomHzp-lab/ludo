using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header(" Original references ")]
    public Dice dice;                           // تاس اصلی بازی
    public BoardManager boardManager;           // زمین بازی (خونه‌ها)
    public List<PlayerController> players;      // لیست همه بازیکن‌ها

    [Header(" Game Setting ")]
    public int currentPlayerIndex = 0;          // نوبت کیه
    public bool gameActive = true;

    private void Start()
    {
        // اتصال رویداد تاس به مدیریت بازی
        dice.OnDiceRolled += HandleDiceRolled;

        // شروع با بازیکن اول
        SetCurrentPlayer(0);
    }

    private void SetCurrentPlayer(int index)
    {
        // ایمن: نکنه عدد از محدوده بزنه بیرون
        if (index < 0 || index >= players.Count) return;

        currentPlayerIndex = index;

        // قفل/آزاد کردن تاس
        EnableDiceForCurrentPlayer(true);

        Debug.Log($"player turn : {players[currentPlayerIndex].playerName}");
    }

    private void EnableDiceForCurrentPlayer(bool enable)
    {
        dice.gameObject.SetActive(enable);
    }

    private void HandleDiceRolled(int rolledNumber)
    {
        // وقتی تاس انداخته شد، قفلش کن تا بازیکن بعدی نوبت بگیره
        EnableDiceForCurrentPlayer(false);

        Debug.Log($" dice   {players[currentPlayerIndex].playerName}: {rolledNumber}");

        // حرکت مهره فعلی با عدد تاس
        players[currentPlayerIndex].MoveToken(0, rolledNumber);

        // بعد از حرکت مهره، صبر کن تا انیمیشن تموم شه
        Invoke(nameof(NextTurn), 2f);
    }

    private void NextTurn()
    {
        if (!gameActive) return;

        // بعدی در لیست
        currentPlayerIndex++;

        // اگه آخر لیست بود، برگرد به اول
        if (currentPlayerIndex >= players.Count)
            currentPlayerIndex = 0;

        SetCurrentPlayer(currentPlayerIndex);
    }

    public void CheckForWinner(PlayerController player)
    {
        // در آینده: بررسی کن مهره‌های همه‌اش به خونه آخر رسیدن یا نه
        if (player.HasAllTokensFinished())
        {
            gameActive = false;
            Debug.Log($"🏆 {player.playerName}  winner ");
        }
    }
}
