using UnityEngine;
using UnityEngine.SceneManagement;


public class MainLandBootstrap : MonoBehaviour
{
 [Header("Scene References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Dice dice;
    [SerializeField] private BoardManager boardManager;

    [SerializeField] private PlayerController player1;
    [SerializeField] private PlayerController player2;
    [SerializeField] private PlayerController player3;
    [SerializeField] private PlayerController player4;

    private PlayerController[] slots;

    void Awake()
    {
        slots = new[] { player1, player2, player3, player4 };
        ApplyMatchSettings();
    }

    void ApplyMatchSettings()
    {
        var s = MatchSettings.Instance;
        int count = (s != null) ? s.playerCount : 4;
        var mode  = (s != null) ? s.opponentMode : OpponentMode.Players;

        // فعال/غیرفعال کردن اسلات‌ها
        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < count && slots[i] != null;
            if (slots[i] != null) slots[i].gameObject.SetActive(active);
        }

        // حالت Bots: P1 انسانی، 2..N بات
        if (mode == OpponentMode.Bots)
        {
            for (int i = 1; i < count; i++)
            {
                var pc = slots[i];
                if (pc == null || !pc.gameObject.activeInHierarchy) continue;

                var ai = pc.GetComponent<AIController>();
                if (ai == null) ai = pc.gameObject.AddComponent<AIController>();

                ai.gameManager  = gameManager;
                ai.dice         = dice;
                ai.self         = pc;
            }
            // اطمینان از انسانی بودن Player1
            if (slots[0] != null)
            {
                var aiOnP1 = slots[0].GetComponent<AIController>();
                if (aiOnP1) Destroy(aiOnP1);
            }
        }
        else // Players: همه انسان
        {
            for (int i = 0; i < count; i++)
            {
                var pc = slots[i];
                if (pc == null) continue;
                var ai = pc.GetComponent<AIController>();
                if (ai) Destroy(ai);
            }
        }

        // اضافی‌ها را خاموش نگه‌دار
        for (int i = count; i < slots.Length; i++)
        {
            if (slots[i] != null) slots[i].gameObject.SetActive(false);
        }
    }
}
