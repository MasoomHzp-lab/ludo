using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider MusicSlider;
    [SerializeField] private Slider SFXSlider;


    void Start()
    {

        if (PlayerPrefs.HasKey("MusicVolume"))
        {

            LoadVolume();

        }
        else
        {
            SetMusicVolume(); 
        }


    }
    public void SetMusicVolume()
    {

        float volume = MusicSlider.value;
        float mixerVolum = Mathf.Log10(volume) * 20;
        myMixer.SetFloat("Music", mixerVolum);

    }

    public void SetSFXVolume()
    {

        float volume = SFXSlider.value;
        float mixerVolum = Mathf.Log10(volume) * 20;
        myMixer.SetFloat("SFX", mixerVolum);
      

    }

    public void AcceptSettingButton()
    {
        float SFXVolume = SFXSlider.value;
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        float MusicVolume = MusicSlider.value;
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);

    }
    
    private void LoadVolume()
    {

        MusicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        SetMusicVolume();


         SFXSlider.value = PlayerPrefs.GetFloat("SFXVolume");
        SetSFXVolume();
    }

    
}
