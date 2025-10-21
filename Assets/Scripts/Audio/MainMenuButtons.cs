using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    public GameObject SettingPanel;

    public void OpenPannel(GameObject panel)
    {

        panel.SetActive(true);

    }

    public void CloseSettingPanel()
    {

        SettingPanel.SetActive(false);

    }

}
