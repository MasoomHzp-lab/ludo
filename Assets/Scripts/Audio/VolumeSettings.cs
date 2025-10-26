using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;

    private List<Slider> musicSliders = new List<Slider>();
    private List<Slider> sfxSliders = new List<Slider>();


    private float currentMusicVolume = 1f;
    private float currentSFXVolume = 1f;

    
    public static VolumeSettings Instance;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {

        currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        ApplyVolumes();


    }


    private void ApplyVolumes()
    {
        myMixer.SetFloat("Music", Mathf.Log10(currentMusicVolume) * 20);
        myMixer.SetFloat("SFX", Mathf.Log10(currentSFXVolume) * 20);
    }


    public void RegisterSliders(Slider musicSlider, Slider sfxSlider)
    {
        if (musicSlider != null)
        {
            musicSliders.Add(musicSlider);
            musicSlider.value = currentMusicVolume;
            musicSlider.onValueChanged.AddListener(delegate { UpdateMusicVolume(musicSlider.value); });
        }

        if (sfxSlider != null)
        {
            sfxSliders.Add(sfxSlider);
            sfxSlider.value = currentSFXVolume;
            sfxSlider.onValueChanged.AddListener(delegate { UpdateSFXVolume(sfxSlider.value); });
        }
    }


     private void UpdateMusicVolume(float value)
    {
        currentMusicVolume = value;
        myMixer.SetFloat("Music", Mathf.Log10(value) * 20);
        SyncAllMusicSliders();
    }

    private void UpdateSFXVolume(float value)
    {
        currentSFXVolume = value;
        myMixer.SetFloat("SFX", Mathf.Log10(value) * 20);
        SyncAllSFXSliders();
    }

    private void SyncAllMusicSliders()
    {
        foreach (var s in musicSliders)
            if (s != null && s.value != currentMusicVolume)
                s.value = currentMusicVolume;
    }

    private void SyncAllSFXSliders()
    {
        foreach (var s in sfxSliders)
            if (s != null && s.value != currentSFXVolume)
                s.value = currentSFXVolume;
    }






    public void AcceptSettingButton()
    {PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
        PlayerPrefs.Save();

    }
    
    
}
