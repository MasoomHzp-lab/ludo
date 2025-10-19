using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerPath
    {
        public PlayerColor color;
        public List<Transform> tiles = new List<Transform>(); // ترتیب مهم است
    }

    [Header("Paths (per player color)")]
    public List<PlayerPath> playerPaths = new List<PlayerPath>();

    public List<Transform> GetFullPath(PlayerColor color)
    {
        foreach (var p in playerPaths)
            if (p.color == color) return p.tiles;

        Debug.LogError($"[BoardManager] مسیر برای {color} پیدا نشد.");
        return new List<Transform>();
    }

    public Vector3 GetTilePosition(PlayerColor color, int index)
    {
        var path = GetFullPath(color);
        if (path == null || path.Count == 0) return Vector3.zero;
        index = Mathf.Clamp(index, 0, path.Count - 1);
        return path[index].position;
    }
}
