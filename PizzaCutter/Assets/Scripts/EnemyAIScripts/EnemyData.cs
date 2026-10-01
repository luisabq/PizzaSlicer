using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Pizza Slicer'Cutter'/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Basic Enemy";

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float detectionRange = 15f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Rewards")]
    public int xpReward = 10;
}
