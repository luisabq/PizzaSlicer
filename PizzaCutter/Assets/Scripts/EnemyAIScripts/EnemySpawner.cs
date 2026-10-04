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

        if (wave.enemyPrefab == null)
        {
            Debug.LogError(
                $"{wave.waveName} has no enemy prefab dumass."
            );

            spawningFinished = true;
            waveComplete = true;

            yield break;
        }

        Debug.Log(
            $"Starting... {wave.waveName}: " +
            $"{wave.enemyCount} enemies to kill mf."
        );

        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnEnemy(wave.enemyPrefab);

            yield return new WaitForSeconds(
                wave.spawnDelay
            );
        }

        spawningFinished = true;
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