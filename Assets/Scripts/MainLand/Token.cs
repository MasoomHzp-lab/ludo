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

    public int homeSlot = -1;   // اسلات خانه‌ی اختصاصی این مهره (0..3). -1 یعنی هنوز ست نشده.

public void AssignHomeSlotIfNeeded()
{
    if (homeSlot >= 0 || owner == null || owner.spawnPoints == null || owner.spawnPoints.Count == 0)
        return;

    // نزدیک‌ترین اسلات به موقعیت فعلی (وقتی مهره تو Home است)
    float best = float.MaxValue;
    int bestIdx = 0;
    for (int i = 0; i < owner.spawnPoints.Count; i++)
    {
        var sp = owner.spawnPoints[i];
        if (sp == null) continue;
        float d = (transform.position - sp.position).sqrMagnitude;
        if (d < best) { best = d; bestIdx = i; }
    }
    homeSlot = bestIdx;
}

public void SnapToHomeSlot()
{
    if (owner == null || owner.spawnPoints == null || owner.spawnPoints.Count == 0) return;
    int idx = Mathf.Clamp(homeSlot < 0 ? 0 : homeSlot, 0, owner.spawnPoints.Count - 1);
    var sp = owner.spawnPoints[idx];
    if (sp != null) transform.position = sp.position;
}


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
            transform.position = boardManager.GetTilePosition(color, currentTileIndex);
            isOnBoard = true;
            steps -= 1;


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
    public void EnterAtStart()
{
    // فرض: اندیس 0 خانه شروع است؛ اگر تابع اختصاصی داری همان را صدا بزن
    currentTileIndex = 0;
    Vector3 startPos = boardManager.GetTilePosition(owner.color, currentTileIndex);
    transform.position = startPos;

    isOnBoard = true;
    isMoving = false;

    // اگر اسنپ انیمیشن می‌خواهی، می‌توانی یک کوروتینِ کوتاه با لِرپ بگذاری
    // ولی مهم: steps کم نشود و حرکت جلوتر نرود
}
}
