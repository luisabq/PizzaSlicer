using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source for 2D UI")]
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;

    [System.Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("Scene Music")]
    [SerializeField] private List<SceneMusic> sceneMusicList = new List<SceneMusic>();
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.5f;

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


    //For playing 2D sounds.
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && effectsSource != null)
        {
            effectsSource.PlayOneShot(clip, volume);
        }
    }

    //copy and paste these 3 a bunch:
    //at the top of the script, change the 'Sound' to a name you want, along with 'Volume' 
    //[Header("Audio")]
    //[SerializeField] private AudioClip Sound;
    // [Range(0f, 1f)][SerializeField] private float Volume = 1.0f;


    //Use this for playing UI sounds. remember to rename Sound and Volume to what you put above
    //   if (Sound != null && SoundManager.Instance != null)
    // { 
    // SoundManager.Instance.PlaySound(Sound, Volume);
    // } 


    //Use this for 3d sounds, theyll be played on where the script's object is. remember to rename sound and volume 
    //AudioSource.PlayClipAtPoint(Sound, transform.position, Volume); 



    //MUSIC 


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip targetClip = null;

        foreach (var entry in sceneMusicList)
        {
            if (entry.sceneName == scene.name)
            {
                targetClip = entry.musicClip;
                break;
            }
        }

        //no music mapped for a level = stop playing
        if (targetClip == null)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            return;
        }

        //if same song is playing between two levels, keep it going.
        if (musicSource.isPlaying && musicSource.clip == targetClip)
        {
            return;
        }

        //Otherwise, switch to the new sonng
        musicSource.clip = targetClip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }


}