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

    [Header("تنظیمات بازیکن")]
    public string playerName = "Red Player";
    public PlayerColor color;                       // رنگ بازیکن
    public BoardManager boardManager;               // ارجاع به برد منیجر
    public GameObject tokenPrefab;                  // پریفب مهره
    public Transform spawnParent;                   // محل ظاهر شدن مهره‌ها (منطقه خانه)

    [Header("مهره‌ها")]
    public List<GameObject> tokens = new List<GameObject>();
    public List<Transform> path = new List<Transform>();

    private bool isMoving = false;

    private void Start()
    {
        // مسیر مخصوص این رنگ رو از برد بگیر
        path = boardManager.GetFullPath(color);

        // ساخت ۴ تا مهره برای بازیکن
        for (int i = 0; i < 4; i++)
        {
            GameObject token = Instantiate(tokenPrefab, spawnParent);
            token.name = $"{playerName}_Token_{i + 1}";
            tokens.Add(token);
        }
    }

    // متد حرکت مهره (با عدد تاس)
    public void MoveToken(int tokenIndex, int steps)
    {
        if (isMoving || tokenIndex < 0 || tokenIndex >= tokens.Count)
            return;

        StartCoroutine(MoveTokenRoutine(tokens[tokenIndex], steps));
    }

    private IEnumerator MoveTokenRoutine(GameObject token, int steps)
    {
        isMoving = true;

        TokenData tokenData = token.GetComponent<TokenData>();
        if (tokenData == null)
        {
            tokenData = token.AddComponent<TokenData>();
            tokenData.currentTileIndex = 0;
        }

        // حرکت یکی‌یکی با تاخیر برای انیمیشن
        for (int i = 0; i < steps; i++)
        {
            tokenData.currentTileIndex++;

            // اگه به انتهای مسیر رسید
            if (tokenData.currentTileIndex >= path.Count)
            {
                tokenData.currentTileIndex = path.Count - 1;
                break;
            }

            Vector3 targetPos = path[tokenData.currentTileIndex].position;
            yield return MoveToPosition(token.transform, targetPos, 0.2f);
        }

        // بررسی برخورد با مهره دشمن
        CheckCollision(token);

        // بررسی رسیدن به خونه آخر
        if (tokenData.currentTileIndex == path.Count - 1)
        {
            tokenData.isFinished = true;
            Debug.Log($"{playerName} یکی از مهره‌هاشو تموم کرد 🎯");
        }

        isMoving = false;
    }

    // انیمیشن حرکت
    private IEnumerator MoveToPosition(Transform piece, Vector3 target, float duration)
    {
        Vector3 start = piece.position;
        float time = 0f;

        while (time < duration)
        {
            piece.position = Vector3.Lerp(start, target, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        piece.position = target;
    }


    // بررسی برخورد با مهره دشمن
    private void CheckCollision(GameObject movingToken)
    {
        TokenData myData = movingToken.GetComponent<TokenData>();
        Vector3 myPos = movingToken.transform.position;

        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player == this) continue; // خودش نباشه

            foreach (var enemy in player.tokens)
            {
                if (enemy == null) continue;
                if (Vector3.Distance(myPos, enemy.transform.position) < 0.1f)
                {
                    // برخورد!
                    TokenData enemyData = enemy.GetComponent<TokenData>();
                    enemyData.currentTileIndex = 0;

                    // برگردوندن به خانه
                    enemy.transform.position = enemyData.homePosition;
                    Debug.Log($"{playerName} مهره {player.playerName} رو زد 💥");
                }
            }
        }
    }

    // بررسی برد
    public bool HasAllTokensFinished()
    {
        foreach (var token in tokens)
        {
            TokenData data = token.GetComponent<TokenData>();
            if (data == null || !data.isFinished)
                return false;
        }
        return true;
    }

}
