using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsScripts : MonoBehaviour
{
     public GameObject Panel;

    public void OpenPannel(GameObject panel)
    {

        panel.SetActive(true);

    }

    public void ClosePanel()
    {

        Panel.SetActive(false);

    }

    
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

     public void PlaySoundButton()
    {

        AudioManager.Instance.PlayButtonSound();
    }
}
