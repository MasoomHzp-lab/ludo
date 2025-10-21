using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LudoExtraTurn : MonoBehaviour
{
    [Header("References")]
    public Dice dice;
    public List<PlayerController> players = new List<PlayerController>();

    [Header("Settings")]
    public bool enabledExtraTurn = true;

    private bool lastRollWasSix = false;
    private readonly Dictionary<PlayerController, bool> movingMap = new Dictionary<PlayerController, bool>();
    private PlayerController snapshotPlayerOnRoll;

    private void OnEnable()
    {
        if (dice != null) dice.OnDiceRolled += OnDiceRolled;
    }

    private void OnDisable()
    {
        if (dice != null) dice.OnDiceRolled -= OnDiceRolled;
    }

    private void Start()
    {
        foreach (var p in players)
            movingMap[p] = false;
    }

    private void OnDiceRolled(int value)
    {
        lastRollWasSix = (value == 6);
        snapshotPlayerOnRoll = dice.currentPlayer; // remember who rolled
        StartCoroutine(WatchForMoveEnd());
    }

    private IEnumerator WatchForMoveEnd()
    {
        // wait while any token of the rolling player is moving
        if (snapshotPlayerOnRoll == null) yield break;

        while (snapshotPlayerOnRoll.IsMoving())
            yield return null;

        // extra roll
        if (enabledExtraTurn && lastRollWasSix)
        {
            // ensure dice points to the same player again (so UI/logic align)
            dice.currentPlayer = snapshotPlayerOnRoll;
            yield return new WaitForSeconds(0.1f);
            dice.RollDice();
        }
    }
}
