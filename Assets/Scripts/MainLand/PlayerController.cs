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

    [Header("References")]
    public BoardManager boardManager;
    public GameObject tokenPrefab;

    [Header("Spawn Points for Tokens")]
    public List<Transform> spawnPoints = new List<Transform>(); // ۴ تا نقطه مشخص در Inspector

    private List<Token> tokens = new List<Token>();
    private bool isMoving = false;

    private void Start()
{
    if (spawnPoints.Count < 1)
    {
        Debug.LogError($"{playerName} هیچ spawn pointی ندارد!");
        return;
    }

    int tokenCount = Mathf.Min(4, spawnPoints.Count);

    for (int i = 0; i < tokenCount; i++)
    {
        GameObject tokenObj = Instantiate(tokenPrefab, spawnPoints[i].position, Quaternion.identity);
        Token token = tokenObj.GetComponent<Token>();
        token.color = color;
        token.Initialize(boardManager);

        tokenObj.name = $"{playerName}_Token_{i + 1}";
        tokens.Add(token);
    }

    if (tokenCount < 4)
        Debug.LogWarning($"{playerName} فقط {tokenCount} مهره ساخته شد، باید ۴ تا spawn point داشته باشه!");
}

    public void MoveToken(int tokenIndex, int steps)
    {
        if (isMoving || tokenIndex < 0 || tokenIndex >= tokens.Count) return;

        Token token = tokens[tokenIndex];
        if (token != null)
        {
            isMoving = true;
            token.MoveSteps(steps);
            StartCoroutine(WaitForToken(token));
        }
    }

    private IEnumerator WaitForToken(Token token)
    {
        while (token.isMoving)
            yield return null;
        isMoving = false;
    }

    public bool HasAllTokensFinished()
    {
        foreach (var t in tokens)
        {
            if (t.currentTileIndex < boardManager.GetFullPath(color).Count - 1)
                return false;
        }
        return true;
    }

    public List<Token> GetTokens()
    {
        return tokens;
    }
}
