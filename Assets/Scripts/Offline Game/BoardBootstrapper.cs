using System.Collections.Generic;
using UnityEngine;

public class BoardBootstrapper : MonoBehaviour
{
  [Header("Existing scene references")]
    public GameManager gameManager;
    public List<PlayerController> allPlayersInScene; // drag Red/Blue/Green/Yellow order

    private void Start()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

        // Fallback to 4P if no setup found
        List<PlayerColor> targetColors;
        if (GameSetup.I == null || GameSetup.I.selectedColors == null || GameSetup.I.selectedColors.Count == 0)
        {
            targetColors = new List<PlayerColor> { PlayerColor.Red, PlayerColor.Blue, PlayerColor.Green, PlayerColor.Yellow };
        }
        else
        {
            targetColors = GameSetup.I.selectedColors;
        }

        var activeList = new List<PlayerController>();
        foreach (var p in allPlayersInScene)
        {
            if (p == null) continue;

            bool enable = targetColors.Contains(p.color);
            p.gameObject.SetActive(enable);
            if (enable) activeList.Add(p);
        }

        // push the filtered order into GameManager
        gameManager.players = activeList;

        Debug.Log($"[Bootstrap] Active players: {activeList.Count} ({string.Join(", ", targetColors)})");
    }

    
}
