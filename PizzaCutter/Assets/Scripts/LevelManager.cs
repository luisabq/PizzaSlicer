
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private int currentLevel = 1;

    [Header("Scene Names")]
    [SerializeField] private string nextLevelSceneName;

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Completion UI")]
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private GameObject gameCompleteUI;

    private bool levelComplete = false;

    private void Start()
    {
        Time.timeScale = 1f;

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(false);

        if (gameCompleteUI != null)
            gameCompleteUI.SetActive(false);

        StartCoroutine(PositionPlayerAtSpawn());
    }

    private IEnumerator PositionPlayerAtSpawn()
    {
        yield return null;

        if (playerSpawnPoint == null)
        {
            Debug.LogWarning(
                $"Level {currentLevel} has no Player Spawn Point assigned."
            );
            yield break;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("Could not find a GameObject tagged Player.");
            yield break;
        }

        CharacterController characterController =
            playerObject.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        playerObject.transform.SetPositionAndRotation(
            playerSpawnPoint.position,
            playerSpawnPoint.rotation
        );

        if (characterController != null)
            characterController.enabled = true;

        Debug.Log($"Player positioned at spawn point for Level {currentLevel}.");
    }

    private void Update()
    {
        if (levelComplete || enemySpawner == null)
            return;

        if (enemySpawner.IsAllWavesComplete())
            CompleteLevel();
    }

    private void CompleteLevel()
    {
        levelComplete = true;

        Debug.Log($"Level {currentLevel} complete!");

        if (currentLevel >= 3)
        {
            if (gameCompleteUI != null)
                gameCompleteUI.SetActive(true);
            else
                Debug.LogWarning("Game Complete UI is not assigned.");

            Time.timeScale = 0f;
            return;
        }

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(true);
        else
            Debug.LogWarning("Level Complete UI is not assigned.");

        Time.timeScale = 0f;
    }

    public void LoadNextLevel()
    {
        if (currentLevel >= 3)
            return;

        if (string.IsNullOrWhiteSpace(nextLevelSceneName))
        {
            Debug.LogError("Next level scene name is missing!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();

            // stats reset then health.
            if (playerStats != null)
                playerStats.ResetForNewRun();

           // if (playerHealth != null)
               // playerHealth.ResetForNewRun();
        }
        else
        {
            Debug.LogWarning(
                "No player, creating new one"
            );
        }

        SceneManager.LoadScene("Level_01");
    }


}

