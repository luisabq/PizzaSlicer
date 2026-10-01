using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveData", menuName = "Pizza Slicer-Cutter/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Settings")]
    public string waveName = "Wave 1";

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public int enemyCount = 5;

    [Header("Spawning")]
    public float spawnDelay = 1f;
}
