
using UnityEngine;
using System.Collections.Generic;



public enum PlayerColor { Red, Blue, Green, Yellow }

public class BoardManager : MonoBehaviour
{
    [Header("Common Path (shared 52 tiles)")]
    public List<Transform> commonPath = new();

    [Header("Each color's final 6 tiles")]
    public List<Transform> redHome = new();
    public List<Transform> blueHome = new();
    public List<Transform> greenHome = new();
    public List<Transform> yellowHome = new();

    [Header("Start indexes in common path")]
    public int redStartIndex = 0;
    public int blueStartIndex = 13;
    public int greenStartIndex = 26;
    public int yellowStartIndex = 39;

    public List<Transform> GetFullPath(PlayerColor color)
    {
        List<Transform> path = new();
        int startIndex = 0;

        switch (color)
        {
            case PlayerColor.Red: startIndex = redStartIndex; break;
            case PlayerColor.Blue: startIndex = blueStartIndex; break;
            case PlayerColor.Green: startIndex = greenStartIndex; break;
            case PlayerColor.Yellow: startIndex = yellowStartIndex; break;
        }

        // اضافه کردن مسیر مشترک از نقطه شروع مخصوص بازیکن
        for (int i = 0; i < commonPath.Count; i++)
        {
            int index = (startIndex + i) % commonPath.Count;
            path.Add(commonPath[index]);
        }

        // اضافه کردن مسیر خانه‌ی پایانی مخصوص رنگ
        switch (color)
        {
            case PlayerColor.Red: path.AddRange(redHome); break;
            case PlayerColor.Blue: path.AddRange(blueHome); break;
            case PlayerColor.Green: path.AddRange(greenHome); break;
            case PlayerColor.Yellow: path.AddRange(yellowHome); break;
        }

        return path;
    }
}
