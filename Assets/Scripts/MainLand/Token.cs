using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Token : MonoBehaviour
{
   public PlayerColor color;
    public int currentIndex = -1; // -1 یعنی هنوز تو Base هست
    public bool isFinished = false;

    private BoardManager board;
    private List<Transform> myPath;

    private void Start()
    {
        board = FindObjectOfType<BoardManager>();
        myPath = board.GetFullPath(color);
    }

    public IEnumerator MoveSteps(int steps)
    {
        if (isFinished) yield break;

        // اگر هنوز تو Base هست و تاس 6 اومده:
        if (currentIndex == -1)
        {
            if (steps == 6)
            {
                currentIndex = 0;
                yield return MoveToPosition(myPath[currentIndex].position);
            }
            yield break;
        }

        for (int i = 0; i < steps; i++)
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex >= myPath.Count)
            {
                isFinished = true;
                yield break;
            }

            yield return MoveToPosition(myPath[nextIndex].position);
            currentIndex = nextIndex;
        }
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
    }
}
