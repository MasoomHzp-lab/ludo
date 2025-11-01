using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerColor
{
    Red,
    Blue,
    Green,
    Yellow
}
public class PlayerController : MonoBehaviour
{
  [Header("Player Info")]
    public string playerName;
    public PlayerColor color;
    public Color playerColor;

    [Header("References")]
    public BoardManager boardManager;
    public GameManager gameManager;           // حتما در Inspector ست شود
    public GameObject tokenPrefab;

    [Header("Spawn Points (exactly 4)")]
    public List<Transform> spawnPoints = new List<Transform>();

    [SerializeField] private List<Token> tokens = new List<Token>(); 
    public IReadOnlyList<Token> Tokens => tokens;


    private void Start()
    {
        if (boardManager == null) { Debug.LogError($"{playerName}: BoardManager ست نیست."); return; }
        if (gameManager == null)  { Debug.LogError($"{playerName}: GameManager ست نیست."); return; }

        if (spawnPoints.Count != 4)
        {
            Debug.LogError($"{playerName}: باید دقیقاً ۴ SpawnPoint بدهی.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            var obj = Instantiate(tokenPrefab, spawnPoints[i].position, Quaternion.identity);
            obj.name = $"{playerName}_Token_{i+1}";

            var token = obj.GetComponent<Token>();
            if (token == null)
            {
                Debug.LogError($"{playerName}: Token prefab اسکریپت Token ندارد!");
                continue;
            }

            token.color = color;
            token.Initialize(boardManager, this, gameManager);
            tokens.Add(token);
        }
    }

    public List<Token> GetTokens() => tokens;

    public bool IsMoving()
    {
        foreach (var t in tokens) if (t.isMoving) return true;
        return false;
    }

    public bool MoveToken(Token token, int steps)
    {
        if (token == null || token.isMoving) return false;
        if (!tokens.Contains(token)) return false; // فقط مهره‌های خودش
        token.MoveSteps(steps);
        // PlayTokenSound();
        return true;
        
    }

    public bool HasAllTokensFinished()
    {
        var pathLen = boardManager.GetFullPath(color).Count;
        foreach (var t in tokens)
            if (t.currentTileIndex < pathLen - 1) return false;

        return true;
    }

    public void PlayTokenSound()
    {

        AudioManager.Instance.PlaySFX(AudioManager.Instance.TokenSound);


    }
}
