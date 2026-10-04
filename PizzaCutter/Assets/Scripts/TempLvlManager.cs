using UnityEngine;
using UnityEngine.SceneManagement;

public class MVPLevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Level Complete")]
    [SerializeField] private GameObject levelCompleteUI;

    private bool levelComplete = false;

    private void Start()
    {
        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (levelComplete || enemySpawner == null)
            return;

        if (enemySpawner.IsAllWavesComplete())
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        levelComplete = true;

        Debug.Log("MVP LEVEL COMPLETE!");

        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(true);
        }

        // bye bye game time, we are done here
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Debug.Log("RESTART BUTTON PRESSED!");

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}