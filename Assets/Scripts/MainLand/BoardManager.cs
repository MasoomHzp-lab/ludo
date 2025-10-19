using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Common Path (shared loop of 52 tiles)")]
    public List<Transform> commonPath = new();

    [Header("Final Home Paths (6 tiles each)")]
    public List<Transform> redHome = new();
    public List<Transform> blueHome = new();
    public List<Transform> yellowHome = new();
    public List<Transform> greenHome = new();


    [Header("Start Index on Common Path")]
    public int redStartIndex = 0;
    public int blueStartIndex = 11;
    public int yellowStartIndex = 22;
    public int greenStartIndex = 33;

    [Header("Home Entry Offset from Start")]
    [Tooltip("-1 = tile before start, 0 = start itself, commonPath.Count = after full loop")]
    public int redHomeEntryOffset = -1;
    public int blueHomeEntryOffset = -1;
    public int yellowHomeEntryOffset = -1;
    public int greenHomeEntryOffset = -1;

    public List<Transform> GetFullPath(PlayerColor color)
    {
        var path = new List<Transform>();

        if (commonPath == null || commonPath.Count == 0)
        {
            Debug.LogError("[BoardManager] commonPath is empty!");
            return path;
        }

        int startIndex = GetStartIndex(color);
        int entryOffset = GetHomeEntryOffset(color);
        int entryIndex = Mod(startIndex + entryOffset, commonPath.Count);

        // Add loop path from start to home entry
        int i = startIndex;
        path.Add(commonPath[i]);
        while (i != entryIndex)
        {
            i = (i + 1) % commonPath.Count;
            path.Add(commonPath[i]);
            if (path.Count > commonPath.Count + 2)
            {
                Debug.LogError("[BoardManager] Infinite loop detected while building path!");
                break;
            }
        }

        // Add home path for that color
        path.AddRange(GetHomeList(color));

        return path;
    }

    public Vector3 GetTilePosition(PlayerColor color, int index)
    {
        var full = GetFullPath(color);
        if (full == null || full.Count == 0) return Vector3.zero;

        if (index < 0)
        {
            Debug.LogWarning($"[BoardManager] index < 0 for {color}, clamped to 0.");
            index = 0;
        }
        else if (index >= full.Count)
        {
            Debug.LogWarning($"[BoardManager] index {index} out of range for {color}, clamped to end.");
            index = full.Count - 1;
        }

        if (full[index] == null)
        {
            Debug.LogError($"[BoardManager] Null Transform in {color} path at index {index}.");
            return Vector3.zero;
        }

        return full[index].position;
    }

    private int GetStartIndex(PlayerColor color) => color switch
    {
        PlayerColor.Red => ClampIndex(redStartIndex),
        PlayerColor.Blue => ClampIndex(blueStartIndex),
        PlayerColor.Yellow => ClampIndex(yellowStartIndex),
        PlayerColor.Green => ClampIndex(greenStartIndex),   
        _ => 0
    };

    private int GetHomeEntryOffset(PlayerColor color) => color switch
    {
        PlayerColor.Red => redHomeEntryOffset,
        PlayerColor.Blue => blueHomeEntryOffset,
        PlayerColor.Yellow => yellowHomeEntryOffset,
        PlayerColor.Green => greenHomeEntryOffset,
        _ => -1
    };

    private List<Transform> GetHomeList(PlayerColor color) => color switch
    {
        PlayerColor.Red => redHome,
        PlayerColor.Blue => blueHome,
        PlayerColor.Yellow => yellowHome,
        PlayerColor.Green => greenHome,
        _ => redHome
    };

    private int ClampIndex(int idx)
    {
        if (commonPath == null || commonPath.Count == 0) return 0;
        if (idx < 0 || idx >= commonPath.Count)
        {
            Debug.LogWarning($"[BoardManager] startIndex {idx} is out of range, clamped.");
            idx = Mod(idx, commonPath.Count);
        }
        return idx;
    }

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (commonPath.Count == 0)
            Debug.LogWarning("[BoardManager] commonPath is empty. Please assign tiles.");

        CheckHome("Red", redHome);
        CheckHome("Blue", blueHome);
        CheckHome("Yellow", yellowHome);
        CheckHome("Green", greenHome);
    }

    private void CheckHome(string name, List<Transform> list)
    {
        if (list == null || list.Count == 0)
            Debug.LogWarning($"[BoardManager] Home path for {name} is empty (can be temporary).");
    }

    private void OnDrawGizmosSelected()
    {
        if (commonPath != null && commonPath.Count > 1)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < commonPath.Count; i++)
            {
                var a = commonPath[i];
                var b = commonPath[(i + 1) % commonPath.Count];
                if (a != null && b != null) Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
#endif
}
