using System.Collections.Generic;
using UnityEngine;

public class LudoEntryGate : MonoBehaviour
{
     [Header("References")]
    public Dice dice;
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Settings")]
    public bool enterOnlyOnSix = true;

    private int lastRoll = 0;

    private void OnEnable()
    {
        if (dice != null) dice.OnDiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        if (dice != null) dice.OnDiceRolled -= OnDiceRolled;
    }

    private void OnDiceRolled(int value)
    {
        lastRoll = value;
        UpdateTokenColliders();
    }

    private void UpdateTokenColliders()
    {
        foreach (var p in players)
        {
            if (p == null) continue;
            foreach (var t in p.GetTokens())
            {
                if (t == null) continue;
                var col = t.GetComponent<Collider>();
                if (col == null) continue;

                if (!t.isOnBoard && enterOnlyOnSix)
                {
                    // off-board tokens: clickable only if rolled 6
                    col.enabled = (lastRoll == 6);
                }
                else
                {
                    // on-board tokens always clickable
                    col.enabled = true;
                }
            }
        }
    }
}
