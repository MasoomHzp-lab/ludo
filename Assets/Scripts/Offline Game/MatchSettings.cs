using UnityEngine;

public enum OpponentMode { Players, Bots }
public class MatchSettings : MonoBehaviour
{

public static MatchSettings Instance;

    [Range(2,4)] public int playerCount = 4;
    public OpponentMode opponentMode = OpponentMode.Players;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Configure(int count, OpponentMode mode)
    {
        playerCount = Mathf.Clamp(count, 2, 4);
        opponentMode = mode;
    }

}
