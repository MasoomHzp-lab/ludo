using UnityEngine;
using UnityEngine.SceneManagement;


public class OfflinePageController : MonoBehaviour
{
   [Header("Scenes")]
    [SerializeField] private string playersSceneName = "MainLand";
    [SerializeField] private string botsSceneName    = "MainLandWithAi";

    [Header("Current Selection (debug)")]
    [SerializeField, Range(2,4)] private int chosenPlayers = 4;
    [SerializeField] private OpponentMode chosenMode = OpponentMode.Players;

    // تعداد بازیکن‌ها
    public void SetPlayersCount(int count)  { chosenPlayers = Mathf.Clamp(count, 2, 4); }
    public void Choose2Players()            { SetPlayersCount(2); }
    public void Choose3Players()            { SetPlayersCount(3); }
    public void Choose4Players()            { SetPlayersCount(4); }

    // نوع حریف
    public void SetOpponentMode(int mode)   { chosenMode = (mode == 1) ? OpponentMode.Bots : OpponentMode.Players; }
    public void ChooseBots()                { chosenMode = OpponentMode.Bots; }
    public void ChoosePlayers()             { chosenMode = OpponentMode.Players; }

    // Start match
    public void StartMatch()
    {
        // تضمین MatchSettings
        var settings = MatchSettings.Instance;
        if (settings == null)
        {
            var go = new GameObject("MatchSettings");
            settings = go.AddComponent<MatchSettings>(); // Awake → DontDestroyOnLoad
        }

        // ذخیره انتخاب‌ها (تعداد بازیکن‌ها برای هر دو سِن لازم می‌شود)
        settings.Configure(chosenPlayers, chosenMode);

        // رفتن به سِن مناسب بر اساس حالت حریف
        string target = (chosenMode == OpponentMode.Bots) ? botsSceneName : playersSceneName;
        SceneManager.LoadScene(target, LoadSceneMode.Single);
    }
}
