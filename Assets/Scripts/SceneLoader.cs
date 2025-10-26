using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
  [Header("Scene Names")]
    public string FirstPage = "FirstPage"; // for knowing the scene
    public string FriendlyGame   = "FriendlyGame";   
    public string MainLand       = "MainLand";
    public string MainLandWithAi = "MainLandWithAi";     
    public string OFFlineGame       = "OFFlineGame";
    public string OnlinePage       = "OnlinePage";       


    [Header("Options")]
    public bool autoLoadMainLand = true;  // اگه تیک‌خورده باشه، خودش Layout رو لود می‌کنه
    public bool autoLoadUI     = false; // اگه خواستی همزمان UI رو هم Additive لود کنه

    private IEnumerator Start()
    {
        // اطمینان از این‌که Managers اکتیوه
        if (SceneManager.GetActiveScene().name != OFFlineGame)
        {
            var sc = SceneManager.GetSceneByName(OFFlineGame);
            if (sc.IsValid()) SceneManager.SetActiveScene(sc);
        }

        // لود Layout
        if (autoLoadMainLand && !SceneManager.GetSceneByName(MainLand).isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(MainLand, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            Debug.Log($"[SceneLoader] Loaded layout scene: {MainLand}");
        }

        // لود UI (اختیاری)
        if (autoLoadUI && !string.IsNullOrEmpty(OnlinePage))
        {
            var opUI = SceneManager.LoadSceneAsync(OnlinePage, LoadSceneMode.Additive);
            while (!opUI.isDone) yield return null;
            Debug.Log($"[SceneLoader] Loaded UI scene: {OnlinePage}");
        }

        // لود FX (اختیاری)
        if (!string.IsNullOrEmpty(MainLandWithAi))
        {
            var opFX = SceneManager.LoadSceneAsync(MainLandWithAi, LoadSceneMode.Additive);
            while (!opFX.isDone) yield return null;
            Debug.Log($"[SceneLoader] Loaded FX scene: {MainLandWithAi}");
        }

        if (!string.IsNullOrEmpty(FirstPage))
        {
            var opFX = SceneManager.LoadSceneAsync(FirstPage, LoadSceneMode.Additive);
            while (!opFX.isDone) yield return null;
            Debug.Log($"[SceneLoader] Loaded FX scene: {FirstPage}");
        }
        
         if (!string.IsNullOrEmpty(FriendlyGame))
        {
            var opFX = SceneManager.LoadSceneAsync(FriendlyGame, LoadSceneMode.Additive);
            while (!opFX.isDone) yield return null;
            Debug.Log($"[SceneLoader] Loaded FX scene: {FriendlyGame}");
        }
    }

    // متد عمومی برای لود Additive هر سین خاص
    public void LoadAdditive(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    // متد عمومی برای Unload هر سین خاص
    public void Unload(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            SceneManager.UnloadSceneAsync(sceneName);
    }


  
}
