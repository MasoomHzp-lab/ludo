using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Dice : MonoBehaviour
{
    AudioManager audioManager;
    public AudioClip diceRollingSound;

    [Header("Dice Settings")]
    public Sprite[] diceSides;         // 6 تصویر
    public int[] diceValues = new int[6]; // 6 مقدار متناظر
    public Image diceImage;

    [Header("Roll FX")]
    public float rollDuration = 1f;
    public float rollSpeed = 0.05f;

    public event Action<int> OnDiceRolled;

    private bool isRolling = false;

    public PlayerController currentPlayer;


    void Awake()
    {
        // audioManager = GameObject.FindGameObjectWithTag("AudioManager").GetComponent<AudioManager>();
        
    }
    public void Roll()
    {
        if (isRolling) return;
        if (GameManager.I != null && !GameManager.I.CanRoll()) return;

        StartCoroutine(RollRoutine());
    }
    public void RollDice()
    {
        if (!isRolling && diceSides != null && diceSides.Length > 0)
            StartCoroutine(RollRoutine());
    }


    private IEnumerator RollRoutine()
    {
        isRolling = true;
        float t = 0f;
        int idx = 0;

        // PlayDiceSound();
        while (t < rollDuration)
        {
            idx = UnityEngine.Random.Range(0, diceSides.Length);
            if (diceImage) diceImage.sprite = diceSides[idx];
            t += rollSpeed;
            yield return new WaitForSeconds(rollSpeed);

        }

        idx = UnityEngine.Random.Range(0, diceSides.Length);
        if (diceImage) diceImage.sprite = diceSides[idx];

        int steps = (diceValues != null && diceValues.Length == diceSides.Length)
            ? diceValues[idx]
            : (idx + 1); // fallback

        OnDiceRolled?.Invoke(steps);
        isRolling = false;
    }


    public void PlayDiceSound()
    {

        AudioManager.Instance.PlaySFX(diceRollingSound);


    }

}
