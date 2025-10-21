using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Token : MonoBehaviour
{
    [HideInInspector] public PlayerColor color;
    [HideInInspector] public int currentTileIndex = -1; // -1 یعنی هنوز روی برد نیست (خانه)
    [HideInInspector] public bool isMoving = false;
    [HideInInspector] public bool isOnBoard = false;

    private BoardManager boardManager;
    [HideInInspector] public PlayerController owner;
    private GameManager gameManager;

    public void Initialize(BoardManager manager, PlayerController player, GameManager gm)
    {
        boardManager = manager;
        owner = player;
        gameManager = gm;
        isOnBoard = false;          // از خانه شروع می‌کند
        currentTileIndex = -1;      // هنوز وارد مسیر نشده
    }

    private void OnMouseDown()
    {
        if (gameManager == null || owner == null) return;
        if (gameManager.CurrentPlayer != owner) return;
        if (isMoving) return;

        gameManager.OnTokenSelected(this);
    }

    public void MoveSteps(int steps)
    {
        if (!gameManager) return;
        if (!isMoving)
            gameManager.StartCoroutine(MoveCoroutine(steps)); // از GameManager coroutine اجرا شود
    }

    private IEnumerator MoveCoroutine(int steps)
    {
        isMoving = true;

        // اگر مهره هنوز وارد زمین نشده، اول ببریمش خانه‌ی شروع مسیر
        if (!isOnBoard)
        {
            currentTileIndex = 0;
            Vector3 startPos = boardManager.GetTilePosition(color, currentTileIndex);
            transform.position = startPos;
            isOnBoard = true;

            // اگر قوانینت ایجاب می‌کند که با ورود، یک قدم هم جلو برود:
            // اگر می‌خواهی ورود هم جزو steps حساب شود، steps -= 1;
        }

        for (int i = 0; i < steps; i++)
        {
            currentTileIndex++;
            var path = boardManager.GetFullPath(color);
            if (currentTileIndex >= path.Count)
            {
                currentTileIndex = path.Count - 1; // رسید به پایان
                break;
            }

            Vector3 target = boardManager.GetTilePosition(color, currentTileIndex);
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 5f);
                yield return null;
            }
        }

        isMoving = false;
    }
}
