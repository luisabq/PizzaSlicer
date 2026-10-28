using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuPanel; //the clipboard panel
    [SerializeField] private GameObject pauseButton; //the HUD pause button, hidden while the menu is open

    [Header("Scene Names")]
    [SerializeField] private string firstLevelSceneName = "Level_01"; //only used if no LevelManager is in the scene
    [SerializeField] private string mainMenuSceneName = "mainScreen";

    [Header("Audio")]
    [SerializeField] private AudioClip pauseSound;
    [Range(0f, 1f)][SerializeField] private float pauseVolume = 1.0f;
    [SerializeField] private AudioClip resumeSound;
    [Range(0f, 1f)][SerializeField] private float resumeVolume = 1.0f;


    private PlayerStats playerStats;


    // Makes sure the menu starts closed
    private void Start()
    {
        IsPaused = false;
        SetMenuVisible(false);
    }

    // Opens the menu if closed, closes it if open (hook the HUD pause button to this)
    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    // Freezes the game and shows the clipboard
    public void Pause()
    {
        if (IsPaused) return;

        //if time is already stopped, something else owns the pause (level up, game over, level complete) so dont stack on top of it
        if (Time.timeScale <= 0f) return;

        IsPaused = true;
        Time.timeScale = 0f;
        SetMenuVisible(true);

        if (pauseSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(pauseSound, pauseVolume);
        }
    }

    // Hides the clipboard and unfreezes the game (hook the Resume button to this)
    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        SetMenuVisible(false);
        Time.timeScale = 1f;

        if (resumeSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(resumeSound, resumeVolume);
        }
    }

    // Starts a fresh run from Level 1 using the same reset as the Game Over restart (hook the Restart button to this)
    public void RestartRun()
    {
        CloseWithoutSound();

        LevelManager levelManager = FindAnyObjectByType<LevelManager>();

        if (levelManager != null)
        {
            levelManager.RestartLevel();
        }
        else
        {
            Debug.LogWarning("[PauseMenu] No LevelManager found, loading the first level directly.");
            Time.timeScale = 1f;
            SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    // Goes back to the title screen and removes the persistent player so it doesnt follow you into the menu (hook the Main Menu button to this)
    public void QuitToMainMenu()
    {
        CloseWithoutSound();
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);

        //PlayerEssentials uses DontDestroyOnLoad, so it has to be destroyed by hand or it keeps running on the title screen.
        //Level_01 has its own PlayerEssentials, so a fresh player spawns when Start is pressed again.
        PlayerPersistence persistentPlayer = FindAnyObjectByType<PlayerPersistence>();

        if (persistentPlayer != null)
        {
            Destroy(persistentPlayer.gameObject);
        }
    }

    // Closes the menu state without playing the resume sound, used before changing scenes
    private void CloseWithoutSound()
    {
        IsPaused = false;
        SetMenuVisible(false);
    }

    // Shows or hides the clipboard and swaps the HUD pause button the opposite way
    private void SetMenuVisible(bool visible)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(visible);
        }
        else
        {
            Debug.LogWarning("[PauseMenu] Pause Menu Panel is not assigned.");
        }

        if (pauseButton != null)
        {
            pauseButton.SetActive(!visible);
        }
    }

    // Clears the static flag if this object gets destroyed while paused
    private void OnDestroy()
    {
        IsPaused = false;
    }


    //gives the player a level, for testing purposes 
    public void InstantLevel()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }
        if (playerStats != null)
        {
            playerStats.LevelUp();
            Debug.Log("Pressed debug level up");
            Resume();
            Time.timeScale = 0f;
        }
    }
}
