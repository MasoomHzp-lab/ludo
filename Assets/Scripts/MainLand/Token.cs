using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Token : MonoBehaviour
{
     public PlayerColor color;              // رنگ مهره
    public int currentTileIndex = -1;      // موقعیت فعلی در مسیر
    public bool isMoving = false;
    public bool isAtHome = true;

    private List<Transform> path;          // مسیر مخصوص این رنگ

    public void Initialize(BoardManager board)
    {
        // مسیر مخصوص رنگ فعلی رو از BoardManager می‌گیریم
        path = board.GetFullPath(color);
    }

    public void MoveSteps(int steps)
    {
        if (isMoving || path == null) return;
        StartCoroutine(MoveCoroutine(steps));
    }

    private IEnumerator MoveCoroutine(int steps)
    {
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;

            if (currentTileIndex >= path.Count)
            {
                currentTileIndex = path.Count - 1;
                break;
            }

            Vector3 nextPos = path[currentTileIndex].position;
            yield return MoveTo(nextPos, 0.25f);
        }

        isMoving = false;
    }

    private IEnumerator MoveTo(Vector3 target, float time)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < time)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = target;
    }
}
