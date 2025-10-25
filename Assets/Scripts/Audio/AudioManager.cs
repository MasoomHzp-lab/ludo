using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    
    [Header("Audio Source")]

    [SerializeField]public AudioSource musicSource;
    [SerializeField]public AudioSource SFXSource;

    [Header("Audio Clip")]

    public AudioClip background;
    public AudioClip Button;
    public AudioClip DiceSound;
    public AudioClip TokenSound;
      

    private void Start()
    {
        // musicSource.clip = background;
        musicSource.Play();

    }


    void Awake()
{
      
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        
        }
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

