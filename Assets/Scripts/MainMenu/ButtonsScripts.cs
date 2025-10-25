using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsScripts : MonoBehaviour
{
     public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
