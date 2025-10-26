using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OfflineLobbyController : MonoBehaviour
{
    [Header("Buttons (assign from Inspector)")]
    public Button btn2P;
    public Button btn3P;
    public Button btn4P;

    public Button btnBots;
    public Button btnPlayers;

    public Button btnStart;

    [Header("Optional visuals")]
    public Image selector2P; // e.g., highlight frame/icon
    public Image selector3P;
    public Image selector4P;
    public Image selectorBots;
    public Image selectorPlayers;

    [Header("Board Scene Name")]
    public string boardSceneName = "MainBoard";

private void Start()
    {
        // Default UI state
        if (GameSetup.I == null)
        {
            var go = new GameObject("GameSetup");
            go.AddComponent<GameSetup>();
        }
        SyncVisuals();
        WireButtons();
    }

    private void WireButtons()
    {
        btn2P.onClick.AddListener(() => { GameSetup.I.SetNumPlayers(2); SyncVisuals(); });
        btn3P.onClick.AddListener(() => { GameSetup.I.SetNumPlayers(3); SyncVisuals(); });
        btn4P.onClick.AddListener(() => { GameSetup.I.SetNumPlayers(4); SyncVisuals(); });

        btnBots.onClick.AddListener(() => { GameSetup.I.SetPlayersType(PlayerKind.Bot); SyncVisuals(); });
        btnPlayers.onClick.AddListener(() => { GameSetup.I.SetPlayersType(PlayerKind.Human); SyncVisuals(); });

        btnStart.onClick.AddListener(OnStartMatch);
    }

    private void SyncVisuals()
    {
        // Simple highlight toggles (null checks allow you to skip these)
        if (selector2P) selector2P.enabled = (GameSetup.I.numPlayers == 2);
        if (selector3P) selector3P.enabled = (GameSetup.I.numPlayers == 3);
        if (selector4P) selector4P.enabled = (GameSetup.I.numPlayers == 4);

        if (selectorBots) selectorBots.enabled = (GameSetup.I.playersType == PlayerKind.Bot);
        if (selectorPlayers) selectorPlayers.enabled = (GameSetup.I.playersType == PlayerKind.Human);
    }

    private void OnStartMatch()
    {
        // Build default colors from selection (or you can open a color picker UI later)
        GameSetup.I.selectedColors = GameSetup.I.BuildDefaultColors();

        // Load board scene
        SceneManager.LoadScene(boardSceneName);
    }
}
