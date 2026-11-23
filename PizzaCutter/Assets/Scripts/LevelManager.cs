using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private int currentLevel = 1;

    [Header("Scene Names")]
    [SerializeField] private string nextLevelSceneName;

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Completion Slowdown Effect")]
    [SerializeField] private CinemachineCamera completionCamera;
    [SerializeField] private float zoomedFOV = 30f;
    [SerializeField] private float cinematicDuration = 3f;
    private float defaultFOV;

    [Header("Completion UI")]
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private GameObject gameCompleteUI;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverUI;

    [Header("Audio")]
    [SerializeField] private AudioClip winSound;
    [Range(0f, 1f)][SerializeField] private float winVolume = 1.0f;
    //lose sound stuff is in playerhealth

    [Header("Enemy Difficulty")]
    //each value is the extra % added per level after Level 1 (0.1 = +10% per level)
    [SerializeField] private float healthIncreasePerLevel = 0.1f;
    [SerializeField] private float damageIncreasePerLevel = 0.1f;
    [SerializeField] private float speedIncreasePerLevel = 0.1f;
    [SerializeField] private float ExpOrbDropIncreasePerLevel = 0.1f;

    //enemies read these when they spawn so their EnemyData assets stay untouched
    public static float HealthMultiplier { get; private set; } = 1f;
    public static float DamageMultiplier { get; private set; } = 1f;
    public static float SpeedMultiplier { get; private set; } = 1f;
    public static float XpMultiplier { get; private set; } = 1f;


    public static bool IsLevelCompleted { get; private set; } = false;

    private bool levelComplete = false;

    // Works out this level's enemy multipliers before the spawner creates anything
    private void Awake()
    {
        if (completionCamera == null)
        {
            completionCamera = FindObjectOfType<CinemachineCamera>();
        }

            int levelsCleared = Mathf.Max(0, currentLevel - 1);

        HealthMultiplier = 1f + healthIncreasePerLevel * levelsCleared;
        DamageMultiplier = 1f + damageIncreasePerLevel * levelsCleared;
        SpeedMultiplier = 1f + speedIncreasePerLevel * levelsCleared;
        XpMultiplier = 1f + ExpOrbDropIncreasePerLevel * levelsCleared;

        Debug.Log(
            $"[DIFFICULTY] Level {currentLevel}: " +
            $"HP x{HealthMultiplier:F2}, DMG x{DamageMultiplier:F2}, " +
            $"SPD x{SpeedMultiplier:F2}, XP x{XpMultiplier:F2}"
        );
    }

    private void Start()
    {
        IsLevelCompleted = false;
        Time.timeScale = 1f;

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(false);

        if (gameCompleteUI != null)
            gameCompleteUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (completionCamera != null)
            defaultFOV = completionCamera.Lens.FieldOfView;
        

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
        if (levelComplete) return;
        levelComplete = true;
        IsLevelCompleted = true;       

        StartCoroutine(LevelCompleteCinematicRoutine());


        /*
        Debug.Log($"Level {currentLevel} complete!");

        if (currentLevel >= 9)
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

        */
    }

    //slowdown for killing the last enemy
    private IEnumerator LevelCompleteCinematicRoutine()
    {
        //time slow
        Time.timeScale = 0.2f;

        if (completionCamera == null)
        {
            completionCamera = FindAnyObjectByType<CinemachineCamera>();
            if (completionCamera != null) defaultFOV = completionCamera.Lens.FieldOfView;

        }
        float elapsed = 0f;
        float startFOV = completionCamera != null ? completionCamera.Lens.FieldOfView : defaultFOV;

       //zoom in
        while (elapsed < cinematicDuration)
        {
            float t = elapsed / cinematicDuration;


            if (completionCamera != null)
            {
                    var currentLens = completionCamera.Lens;
                    currentLens.FieldOfView = Mathf.Lerp(startFOV, zoomedFOV, t);
                    completionCamera.Lens = currentLens;
            }
            else
            {
                Debug.LogWarning("[LEVEL MANAGER] completionCamera is still NULL during zoom!");
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        

          

            //showing the completion stuff
            ShowCompletionUI();
    }

    private void ShowCompletionUI()
    {
        if (winSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(winSound, winVolume);
        }

        Debug.Log($"Level {currentLevel} complete!");

        if (currentLevel >= 9)
        {
            if (gameCompleteUI != null)
            {
                gameCompleteUI.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Game Complete UI is not assigned.");
            }

            Time.timeScale = 0f;
            return;
        }

        if (levelCompleteUI != null)
            levelCompleteUI.SetActive(true);
        else
            Debug.LogWarning("Level Complete UI is not assigned.");

        Time.timeScale = 0f;


        

    }


    public void ShowGameOverUI()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Game Over UI is not assigned.");
        }

        Time.timeScale = 0f;
    }

    public void LoadNextLevel()
    {
        if (currentLevel >= 9)
            return;

        if (string.IsNullOrWhiteSpace(nextLevelSceneName))
        {
            Debug.LogError("Next level scene name is missing!");
            return;
        }


        //reseting camera stuff back to normal from level complete slowdown

        if (completionCamera != null)
        {
            var resetLens = completionCamera.Lens;
            resetLens.FieldOfView = defaultFOV;
            completionCamera.Lens = resetLens;
        }

        Time.timeScale = 1f;
        SceneTransitionManager.Instance.TransitionToScene(nextLevelSceneName);
        //SceneManager.LoadScene(nextLevelSceneName);
    }

    public void DebugSkipToNextStage()
    {
        string targetScene = "";

        if (currentLevel >= 1 && currentLevel <= 3)
        {
            targetScene = "Level_04";
        }
        else if (currentLevel >= 4 && currentLevel <= 6)
        {
            targetScene = "Level_07";
        }
        else
        {
            Debug.Log("[DEBUG] No next stage available.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[DEBUG] SceneTransitionManager not found!");
            return;
        }

        Time.timeScale = 1f;

        Debug.Log($"[DEBUG] Skipping from Level {currentLevel} to {targetScene}");

        SceneTransitionManager.Instance.TransitionToScene(targetScene);
    }

    public void RestartLevel()
    {
        RestartRunFromBeginning();
    }

    public void PlayAgain()
    {
        RestartRunFromBeginning();
    }

    // Both Game Over > Restart and Game Complete > Play Again start a fresh run.
    private void RestartRunFromBeginning()
    {
        Time.timeScale = 1f;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
            PlayerWeaponManager weaponManager = playerObject.GetComponent<PlayerWeaponManager>();

            if (playerStats != null)
                playerStats.ResetForNewRun();
            else
                Debug.LogError("[RESTART] PlayerStats was not found on the Player.");

            if (weaponManager != null)
                weaponManager.ResetToStartingLoadout();
            else
                Debug.LogError("[RESTART] PlayerWeaponManager was not found on the Player.");

            if (playerHealth != null)
                playerHealth.ResetHealthForRestart();
            else
                Debug.LogError("[RESTART] PlayerHealth was not found on the Player.");
        }
        else
        {
            Debug.LogError("[RESTART] Could not find the GameObject tagged Player.");
        }

        if (LevelUpUI.Instance != null)
            LevelUpUI.Instance.ResetForNewRun();

        //SceneManager.LoadScene("Level_01");
        SceneTransitionManager.Instance.TransitionToScene("Level_01");
    }

}

