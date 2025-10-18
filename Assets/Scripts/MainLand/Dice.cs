using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Dice : MonoBehaviour
{
[Header("تصاویر تاس (از 1 تا 6)")]
    public Sprite[] diceFaces; // ۶ تا تصویر
    public Image diceImage;    // همون UI Image وسط زمین

    [Header("تاخیر در نمایش انیمیشن")]
    public float rollDuration = 0.8f;

    [HideInInspector] public int currentNumber;
    [HideInInspector] public bool isRolling = false;

    public System.Action<int> OnDiceRolled; // رویداد برای ارتباط با GameManager

    public void OnDiceButtonClick()
    {
        if (isRolling) return;
        StartCoroutine(RollAnimation());
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;

        float elapsed = 0f;
        int randomFace = 1;

        // انیمیشن سریع تاس
        while (elapsed < rollDuration)
        {
            randomFace = Random.Range(1, 7);
            diceImage.sprite = diceFaces[randomFace - 1];
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // عدد نهایی
        currentNumber = randomFace;
        diceImage.sprite = diceFaces[currentNumber - 1];

        // اطلاع بده به گیم‌منیجر که تاس افتاد
        OnDiceRolled?.Invoke(currentNumber);

        isRolling = false;
    }
}
