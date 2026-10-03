using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyData enemyData;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Enemy Drops")]
    [SerializeField] private GameObject xpOrbPrefab;
    [SerializeField] private GameObject healthOrbPrefab;
    [SerializeField] private float healthDropChance = 20f;

    private float currentHealth;
    private float attackTimer;

    private void Start()
    {
        if (enemyData == null)
        {
            Debug.LogError($"{gameObject.name} is missing EnemyData data duhh!");
            return;
        }

        currentHealth = enemyData.maxHealth;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (enemyData == null || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distanceToPlayer > enemyData.detectionRange)
            return;

        if (distanceToPlayer > enemyData.attackRange)
        {
            MoveTowardPlayer();
        }
        else
        {
            AttackPlayer();
        }
    }

    private void MoveTowardPlayer()
    {
        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        direction.Normalize();

        transform.position +=
            direction * enemyData.moveSpeed * Time.deltaTime;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            DealDamageToPlayer();
            attackTimer = enemyData.attackCooldown;
        }
    }

    private void DealDamageToPlayer()
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            playerHealth.TakeDamage(enemyData.attackDamage);

            Debug.Log(
                $"{enemyData.enemyName} attacked player " +
                $"for {enemyData.attackDamage} damage."
            );
        }

    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            $"{enemyData.enemyName} took {damage} damage. " +
            $"HP: {currentHealth}/{enemyData.maxHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{enemyData.enemyName} is ded. DED I TELL YA!");

        //spawns exp orb
        if (xpOrbPrefab != null)
        {
            Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            if (xpOrbPrefab.TryGetComponent<EXPOrb>(out var orb))
            {
                orb.SetXPValue(enemyData.xpReward);
            }
        }
        else
        {
           Debug.Log("Me when i hate giving the enemy the exp orb prefab");
        }


            

        Destroy(gameObject);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return enemyData != null ? enemyData.maxHealth : 0f;
    }
}