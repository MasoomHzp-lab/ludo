using System.Collections.Generic;
using UnityEngine;

public enum PlayerKind { Human, Bot }
public class GameSetup : MonoBehaviour
{
    public static GameSetup I;

    [Header("Selections")]
    public int numPlayers = 4;                       // 2, 3, or 4
    public PlayerKind playersType = PlayerKind.Human;
    public List<PlayerColor> selectedColors = new(); // filled before loading the board
  private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetNumPlayers(int n)
    {
        numPlayers = Mathf.Clamp(n, 2, 4);
    }

    public void SetPlayersType(PlayerKind kind)
    {
        playersType = kind;
    }

    // Default color rosters for each mode (tweak to your taste)
    public List<PlayerColor> BuildDefaultColors()
    {
        var list = new List<PlayerColor>();
        switch (numPlayers)
        {
            case 2: list.AddRange(new[] { PlayerColor.Red, PlayerColor.Blue }); break;
            case 3: list.AddRange(new[] { PlayerColor.Red, PlayerColor.Blue, PlayerColor.Green }); break;
            default: list.AddRange(new[] { PlayerColor.Red, PlayerColor.Blue, PlayerColor.Green, PlayerColor.Yellow }); break;
        }
        return list;
    }

}
