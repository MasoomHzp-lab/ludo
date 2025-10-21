using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Audio Clip")]

    public AudioClip background;
    public AudioClip Button;

    private void Start()
    {
        // musicSource.clip = background;
        musicSource.Play();

    }


    public void PlayButtonSound()
    {
        SFXSource.PlayOneShot(Button);
        Debug.Log("Button Click");

    }


    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);

    }



    //  برای صدا زدن این متد باید دستورات زیر در اسکریپت مربوطه اضافه شود

    // AudioMAnager audioManager;
    // Awake()
    // { audioManager= GameObject.FindeGameObjectWithTag("AudioManager").GetComponent<AudioManager>(); }
    //  audioManager.PlaySFX(audioManager.duse)

}

