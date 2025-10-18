using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
   public List<PlayerController> players = new();
    private int currentPlayerIndex = 0;

    private void Start()
    {
        StartCoroutine(RunTurn());
    }

    IEnumerator RunTurn()
    {
        var player = players[currentPlayerIndex];
        int dice = Random.Range(1, 7);
        Debug.Log($"{player.color} rolled {dice}");

        var movable = player.GetMovableTokens(dice);

        if (movable.Count > 0)
        {
            yield return StartCoroutine(movable[0].MoveSteps(dice));

            if (dice == 6)
            {
                yield return new WaitForSeconds(0.3f);
                StartCoroutine(RunTurn());
                yield break;
            }
        }

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(RunTurn());
    }
}
