using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source for 2D UI / One-Shots")]
    [SerializeField] private AudioSource effectsSource;

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
        }
    }

    // Play a 2D sound from anywhere (UI clicks, level-up chimes, etc.)
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && effectsSource != null)
        {
            effectsSource.PlayOneShot(clip, volume);
        }
    }
}