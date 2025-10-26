using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class BoardBootstrapper : MonoBehaviour
{
   [System.Serializable]
    public class TokenArchetype
    {
        public PlayerColor color;
        public GameObject tokenPrefab;          // همون پریفب توکن همین رنگ
        public Transform spawnRoot;             // اختیاری
        public List<Transform> spawnPoints = new List<Transform>(); // 4 خانه‌ی هوم
        public string playerNameOverride;       // اختیاری
    }

    [Header("Managers (auto if left empty)")]
    public GameManager gameManager;
    public BoardManager boardManager;

    [Header("Token prefabs & layout per color")]
    public List<TokenArchetype> tokenArchetypes = new List<TokenArchetype>();

    [Header("Fallback colors if menu didn't set")]
    public List<PlayerColor> defaultColors = new List<PlayerColor>
    { PlayerColor.Red, PlayerColor.Blue, PlayerColor.Green, PlayerColor.Yellow };

    private void Start()
    {
        // 1) resolve managers (prefer assigned, else singleton)
        gameManager  = gameManager  ?? GameManager.I;
        boardManager = boardManager ?? BoardManager.I;

        if (gameManager == null || boardManager == null)
        {
            Debug.LogError("[Bootstrap] GameManager/BoardManager not found. " +
                           "Make sure they exist in SOME loaded scene, or assign them.");
            return;
        }

        // 2) which colors to spawn?
        var targetColors = (GameSetup.I != null &&
                            GameSetup.I.selectedColors != null &&
                            GameSetup.I.selectedColors.Count > 0)
                           ? GameSetup.I.selectedColors
                           : defaultColors;

        var activePlayers = new List<PlayerController>();

        foreach (var color in targetColors)
        {
            var arch = tokenArchetypes.Find(a => a.color == color);
            if (arch == null || arch.tokenPrefab == null)
            {
                Debug.LogError($"[Bootstrap] Missing TokenArchetype or tokenPrefab for {color}");
                continue;
            }

            // make a simple player GO at runtime
            Transform parent = arch.spawnRoot != null ? arch.spawnRoot : this.transform;
            var go = new GameObject($"Player_{color}");
            go.transform.SetParent(parent, false);

            var pc = go.AddComponent<PlayerController>();
            // wire BEFORE PlayerController.Start
            pc.color        = color;
            pc.boardManager = boardManager;
            pc.gameManager  = gameManager;         // اگر این فیلد رو داری
            pc.tokenPrefab  = arch.tokenPrefab;
            if (!string.IsNullOrEmpty(arch.playerNameOverride))
                pc.playerName = arch.playerNameOverride;
            if (arch.spawnPoints != null && arch.spawnPoints.Count > 0)
                pc.spawnPoints = new List<Transform>(arch.spawnPoints);

            activePlayers.Add(pc);
        }

        // 3) hand to GameManager
        gameManager.players = activePlayers;

        Debug.Log($"[Bootstrap] Spawned players: {activePlayers.Count}");
    }
    
}
