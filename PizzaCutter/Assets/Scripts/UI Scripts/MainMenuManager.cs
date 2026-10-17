
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string firstLevelScene = "Level_01";

    [Header("Optional UI Panels")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio")]
    [SerializeField] private AudioClip startSound;
    [Range(0f, 1f)][SerializeField] private float startVolume = 1.0f;


    private void Start()
    {
        Time.timeScale = 1f;

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void StartGame()
    {
        if (startSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(startSound, startVolume);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(firstLevelScene);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
        else
            Debug.LogWarning("no credits panel assigned.");
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        else
            Debug.LogWarning("no settings panel assigned.");
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("mama mia no more pizza. bye bye.");

        Application.Quit();
    }
}
