using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public PlayerColor color;
    public List<Token> tokens = new();

    public List<Token> GetMovableTokens(int dice)
    {
        var movable = new List<Token>();

        foreach (var token in tokens)
        {
            if (token.isFinished) continue;

            if (token.currentIndex == -1)
            {
                if (dice == 6)
                    movable.Add(token);
            }
            else
            {
                movable.Add(token);
            }
        }

        return movable;
    }
}
