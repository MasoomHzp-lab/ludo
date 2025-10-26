using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSliderBinder : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

     void Start()
    {
        if (VolumeSettings.Instance != null)
        {
            VolumeSettings.Instance.RegisterSliders(musicSlider, sfxSlider);
        }
    }
}
