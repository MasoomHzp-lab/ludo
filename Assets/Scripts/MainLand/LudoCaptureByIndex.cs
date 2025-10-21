using System.Collections.Generic;
using UnityEngine;

public class LudoCaptureByIndex : MonoBehaviour
{
    [Header("References")]
    public List<PlayerController> players = new List<PlayerController>();
    public BoardManager boardManager;

    [Header("Rules")]
    public bool allowCaptureOnHomePath = false;   // usually false in Ludo
    public bool allowCaptureOnStartTiles = true;  // many rules protect start; set false to protect
    [Tooltip("Indices (on the common loop) that are safe (no capture).")]
    public List<int> safeLoopIndices = new List<int>();

    [Header("Owner Stack Behavior")]
    public bool allowStackSameColor = true; // own tokens can share a tile (typical Ludo)

    // track isMoving -> false transitions
    private readonly Dictionary<Token, bool> movingSnapshot = new Dictionary<Token, bool>();

    private void Start()
    {
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

        foreach (var p in players)
        {
            if (p == null) continue;
            foreach (var t in p.GetTokens())
            {
                if (t == null) continue;
                if (!movingSnapshot.ContainsKey(t))
                    movingSnapshot.Add(t, t.isMoving);
            }
        }
    }

    private void Update()
    {
        foreach (var p in players)
        {
            if (p == null) continue;
            foreach (var t in p.GetTokens())
            {
                if (t == null) continue;

                bool was = movingSnapshot.TryGetValue(t, out var prev) ? prev : false;
                bool now = t.isMoving;

                if (was && !now) // landed
                {
                    movingSnapshot[t] = now;
                    OnTokenLanded(t);
                }
                else
                {
                    movingSnapshot[t] = now;
                }
            }
        }
    }

    private void OnTokenLanded(Token landed)
    {
        if (!landed.isOnBoard || boardManager == null) return;

        int commonCount = (boardManager.commonPath != null) ? boardManager.commonPath.Count : 0;
        bool onHomePath = (landed.currentTileIndex >= commonCount);

        // home path capture?
        if (onHomePath && !allowCaptureOnHomePath) return;

        // safe loop indices
        if (!onHomePath && IsSafeLoopIndex(landed.currentTileIndex, commonCount))
            return;

        // protect start tiles?
        if (!onHomePath && !allowCaptureOnStartTiles && IsStartIndex(landed.currentTileIndex, commonCount))
            return;

        // check opponents on the same tile index
        foreach (var p in players)
        {
            if (p == null) continue;

            foreach (var other in p.GetTokens())
            {
                if (other == null || other == landed) continue;
                if (!other.isOnBoard) continue;

                // same color? optionally allow stacking
                if (other.color == landed.color)
                {
                    if (allowStackSameColor) continue; // no capture on same color
                    // else fall-through: if you want same color to bump (usually not in Ludo)
                }

                // compare by index (only reliable on the flattened path)
                if (other.currentTileIndex == landed.currentTileIndex)
                {
                    // send that opponent token home
                    SendHome(other);
                    Debug.Log($"[Capture] {landed.owner.playerName} captured {other.owner.playerName}'s token.");
                }
            }
        }
    }

    private bool IsSafeLoopIndex(int idx, int commonCount)
    {
        if (idx < 0 || idx >= commonCount) return false;
        return safeLoopIndices != null && safeLoopIndices.Contains(idx);
    }

    private bool IsStartIndex(int idx, int commonCount)
    {
        if (boardManager == null || commonCount <= 0) return false;

        int r = Mod(boardManager.redStartIndex, commonCount);
        int b = Mod(boardManager.blueStartIndex, commonCount);
        int g = Mod(boardManager.greenStartIndex, commonCount);
        int y = Mod(boardManager.yellowStartIndex, commonCount);

        return idx == r || idx == b || idx == g || idx == y;
    }

    private void SendHome(Token token)
    {
        var owner = token.owner;
        if (owner == null || owner.spawnPoints == null || owner.spawnPoints.Count == 0)
        {
            Debug.LogWarning("[LudoCaptureByIndex] Owner has no spawn points; cannot send home.");
            return;
        }

        // reset token state
        token.isMoving = false;
        token.isOnBoard = false;
        token.currentTileIndex = -1;

        // pick a spawn point (first free if you track occupancy; here just first)
        Transform target = owner.spawnPoints[0];
        token.transform.position = target.position;

        // If your Token has its own reset/init method, call it here instead (e.g., token.ResetToHome();)
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }
}
