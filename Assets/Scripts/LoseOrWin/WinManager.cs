using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class WinManager : MonoBehaviour
{
    [Header("Win Condition")]
    [Tooltip("چند مهره باید به خانه‌ی آخر برسند تا آن رنگ برنده شود؟")]
    public int tokensToWin = 4;

    [Header("UI References")]
    public GameObject winPanel;          // پنل برد
    public TextMeshProUGUI winText;      // متن داخل پنل

    private readonly Dictionary<PlayerColor, int> finishCounters =
        new Dictionary<PlayerColor, int>();

    private bool gameEnded = false;

    /// وقتی یک مهره وارد خانه‌های آخر خودش شد این متد را صدا بزن
   public void RegisterFinishedToken(Token token)
{
    if (token == null || gameEnded) return;

    var owner = token.owner;
    if (owner == null)
    {
        Debug.LogWarning("[WinManager] Token has no owner!");
        return;
    }

    PlayerColor color = owner.color;
    Debug.Log($"[WinManager] RegisterFinishedToken called for {color}, token = {token.name}");

    if (!finishCounters.ContainsKey(color))
        finishCounters[color] = 0;

    finishCounters[color]++;

    Debug.Log($"[WinManager] {color} finished tokens = {finishCounters[color]}");

    if (finishCounters[color] >= tokensToWin)
    {
        DeclareWinner(color);
    }
}


   private void DeclareWinner(PlayerColor winnerColor)
{
    if (gameEnded) return;
    gameEnded = true;

    Debug.Log($"[WinManager] Winner = {winnerColor}");

    if (winPanel != null)
        winPanel.SetActive(true);
    else
        Debug.LogWarning("[WinManager] winPanel is NOT assigned!");

    if (winText != null)
    {
        // متن فعلی داخل TMP، مثلاً: "{0} برنده شد!"
        string template = winText.text;
        string winnerNameFa = GetColorNameFa(winnerColor);

        // {0} رو با اسم رنگ فارسی جایگزین می‌کنیم
        winText.text = string.Format(template, winnerNameFa);
    }
    else
    {
        Debug.LogWarning("[WinManager] winText is NOT assigned!");
    }
}


private string GetColorNameFa(PlayerColor color)
{
    switch (color)
    {
        case PlayerColor.Red:   return "Red player ";
        case PlayerColor.Blue:  return "Blue Player ";
        case PlayerColor.Yellow:return "Yellow Player ";
        case PlayerColor.Green: return "Green Player ";
        default:                return color.ToString();
    }
}

}
