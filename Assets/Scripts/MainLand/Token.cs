using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Token : MonoBehaviour
{
       [Header("تنظیمات عمومی")]
    public PlayerColor color;              // رنگ مهره
    public BoardManager boardManager;      // ارجاع به BoardManager (در Inspector ست کن)
    
    private List<Transform> path;          // مسیر اختصاصی این مهره
    private int currentTileIndex = -1;
    private bool isMoving = false;

    private void Start()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        // مسیر اختصاصی مهره از روی رنگ
        path = boardManager.GetFullPath(color);

        // تنظیم موقعیت اولیه (اگر خواستی از خونه خانه شروع کن)
        transform.position = path[0].position;
        currentTileIndex = 0;
    }

    public void MoveSteps(int steps)
    {
        if (isMoving) return;
        StartCoroutine(MoveCoroutine(steps));
    }

    private IEnumerator MoveCoroutine(int steps)
    {
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            int nextIndex = currentTileIndex + 1;

            // بررسی رسیدن به انتهای مسیر
            if (nextIndex >= path.Count)
                break;

            Vector3 nextPos = path[nextIndex].position;
            yield return MoveTo(nextPos, 0.25f);

            currentTileIndex = nextIndex;
        }

        isMoving = false;
    }

    private IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }
}
