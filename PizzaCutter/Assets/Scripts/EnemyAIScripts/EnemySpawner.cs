

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private int currentWaveIndex = 0;

    private bool spawningFinished = false;
    private bool waveComplete = false;

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private void Update()
    {
        if (!spawningFinished || waveComplete)
            return;

        CheckWaveComplete();
    }

    private IEnumerator RunWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("No waves assigned dumass.");
            yield break;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned dumass.");
            yield break;
        }

        for (currentWaveIndex = 0;
             currentWaveIndex < waves.Length;
             currentWaveIndex++)
        {
            waveComplete = false;
            spawningFinished = false;

            yield return StartCoroutine(
                SpawnWave(waves[currentWaveIndex])
            );

            yield return new WaitUntil(
                () => waveComplete
            );

            Debug.Log(
                $"Wave {currentWaveIndex + 1} complete, about damn time."
            );
        }

        Debug.Log("YOU DID IT! WOOOOO!");
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        if (wave == null)
        {
            Debug.LogError(
                $"Wave {currentWaveIndex + 1} is empty you bum!"
            );

            spawningFinished = true;
            waveComplete = true;

            yield break;
        }

        if (wave.enemyCount > 0 && wave.enemyPrefab == null)
        {
            Debug.LogError(
                $"{wave.waveName} has no melee enemy prefab dumass."
            );

            spawningFinished = true;
            waveComplete = true;

            yield break;
        }

        if (wave.rangedEnemyCount > 0 &&
            wave.rangedEnemyPrefab == null)
        {
            Debug.LogError(
                $"{wave.waveName} has no ranged enemy prefab."
            );

            spawningFinished = true;
            waveComplete = true;

            yield break;
        }

        if (wave.ratEnemyCount > 0 &&
            wave.ratEnemyPrefab == null)
        {
            Debug.LogError(
                $"{wave.waveName} has Rat enemies configured, " +
                "but no Rat prefab assigned."
            );

            spawningFinished = true;
            waveComplete = true;

            yield break;
        }

        int totalEnemies =
            wave.enemyCount +
            wave.rangedEnemyCount +
            wave.ratEnemyCount;

        Debug.Log(
            $"Starting... {wave.waveName}: " +
            $"{totalEnemies} enemies to kill mf."
        );

        int meleeRemaining = wave.enemyCount;
        int rangedRemaining = wave.rangedEnemyCount;
        int ratRemaining = wave.ratEnemyCount;

        while (meleeRemaining > 0 ||
               rangedRemaining > 0 ||
               ratRemaining > 0)
        {
            // Randomly choose between enemy types still remaining.
            int availableTypes = 0;

            if (meleeRemaining > 0) availableTypes++;
            if (rangedRemaining > 0) availableTypes++;
            if (ratRemaining > 0) availableTypes++;

            int chosenType = Random.Range(0, availableTypes);

            if (meleeRemaining > 0)
            {
                if (chosenType == 0)
                {
                    SpawnEnemy(wave.enemyPrefab);
                    meleeRemaining--;
                }
                else
                {
                    chosenType--;
                    SpawnOtherEnemy(
                        chosenType,
                        wave,
                        ref rangedRemaining,
                        ref ratRemaining
                    );
                }
            }
            else
            {
                SpawnOtherEnemy(
                    chosenType,
                    wave,
                    ref rangedRemaining,
                    ref ratRemaining
                );
            }

            yield return new WaitForSeconds(wave.spawnDelay);
        }

        spawningFinished = true;
    }

    private void SpawnOtherEnemy(
        int chosenType,
        WaveData wave,
        ref int rangedRemaining,
        ref int ratRemaining)
    {
        if (rangedRemaining > 0)
        {
            if (chosenType == 0)
            {
                SpawnEnemy(wave.rangedEnemyPrefab);
                rangedRemaining--;
                return;
            }

            chosenType--;
        }

        if (ratRemaining > 0 && chosenType == 0)
        {
            SpawnEnemy(wave.ratEnemyPrefab);
            ratRemaining--;
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        Transform spawnPoint =
            spawnPoints[
                Random.Range(0, spawnPoints.Length)
            ];

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        spawnedEnemies.Add(enemy);
    }

    private void CheckWaveComplete()
    {
        spawnedEnemies.RemoveAll(
            enemy => enemy == null
        );

        if (spawnedEnemies.Count == 0)
        {
            waveComplete = true;
        }
    }

    public int GetActiveEnemyCount()
    {
        spawnedEnemies.RemoveAll(
            enemy => enemy == null
        );

        return spawnedEnemies.Count;
    }

    public int GetCurrentWave()
    {
        return currentWaveIndex + 1;
    }

    public bool IsWaveComplete()
    {
        return waveComplete;
    }

    public bool IsAllWavesComplete()
    {
        return currentWaveIndex >= waves.Length;
    }
}
