
using UnityEngine;
using UnityEngine.AI;

public class RangedEnemyController : MonoBehaviour
{
    //same as EnemyController but with some ranged logic.

    [Header("Enemy Configuration")]
    [SerializeField] private EnemyData enemyData;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float retreatDistance = 3f;
    [SerializeField] private float lineOfSightHeight = 1f;
    [SerializeField] private float attackWindUp = 0.5f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Enemy Drops")]
    [SerializeField] private GameObject xpOrbPrefab;
    [SerializeField] private GameObject healthPickupPrefab;

    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private float vfxDestroyDelay = 2f;

    private float currentHealth;
    private float attackTimer;
    private float windUpTimer;
    private bool preparingAttack;

    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyData == null)
        {
            Debug.LogError($"{gameObject.name} is missing EnemyData");
            enabled = false;
            return;
        }

        currentHealth = enemyData.maxHealth;

        if (agent != null)
        {
            agent.speed = enemyData.moveSpeed;
        }
        else
        {
            Debug.LogError($"{gameObject.name} is missing a NavMeshAgent");
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        attackTimer = 0f;
    }

    private void Update()
    {
        if (player == null || enemyData == null || agent == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distanceToPlayer > enemyData.detectionRange)
        {
            agent.isStopped = true;
            preparingAttack = false;
            return;
        }

        bool canSeePlayer = HasLineOfSight();

        if (!canSeePlayer)
        {
            preparingAttack = false;
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        if (distanceToPlayer < retreatDistance)
        {
            preparingAttack = false;
            agent.isStopped = false;

            Vector3 retreatDirection =
                (transform.position - player.position).normalized;

            Vector3 retreatPosition =
                transform.position + retreatDirection * 3f;

            agent.SetDestination(retreatPosition);
            return;
        }

        if (distanceToPlayer > preferredDistance)
        {
            preparingAttack = false;
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        // make sure player is visible and within range
        agent.isStopped = true;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection),
                8f * Time.deltaTime
            );
        }

        AttackPlayer();
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position
            + Vector3.up * lineOfSightHeight;

        Vector3 target = player.position
            + Vector3.up * lineOfSightHeight;

        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(
            origin,
            direction.normalized,
            out RaycastHit hit,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            // this is so walls/obstacles can 'block' the shots
            if (hit.transform != player &&
                !hit.transform.IsChildOf(player))
            {
                return false;
            }
        }

        return true;
    }

    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        if (preparingAttack)
        {
            windUpTimer -= Time.deltaTime;

            if (windUpTimer <= 0f)
            {
                FireProjectile();
                preparingAttack = false;
                attackTimer = enemyData.attackCooldown;
            }

            return;
        }

        if (attackTimer <= 0f)
        {
            preparingAttack = true;
            windUpTimer = attackWindUp;
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError(
                $"{gameObject.name} is missing its projectile prefab or fire point!"
            );
            return;
        }

        Vector3 direction =
            (player.position - firePoint.position).normalized;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        // the damage and speed of the projectile is handled in EnemyProjectile script
        EnemyProjectile projectileScript =
            projectile.GetComponent<EnemyProjectile>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(
                direction,
                enemyData.attackDamage,
                transform
            );
        }
        else
        {
            Debug.LogError(
                "Projectile prefab is missing EnemyProjectile!"
            );
            Destroy(projectile);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(
                hitVFXPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(vfx, vfxDestroyDelay);
        }

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (xpOrbPrefab != null)
        {
            GameObject orbObject = Instantiate(
                xpOrbPrefab,
                transform.position,
                Quaternion.identity
            );

            if (orbObject.TryGetComponent<EXPOrb>(out var orb))
                orb.SetXPValue(enemyData.xpReward);
        }

        if (healthPickupPrefab != null &&
            Random.value <= enemyData.healthDropChance)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );

            GameObject healthObject = Instantiate(
                healthPickupPrefab,
                transform.position + offset,
                Quaternion.identity
            );

            if (healthObject.TryGetComponent<HealthPickup>(
                out var healthPickup))
            {
                healthPickup.SetHealValue(enemyData.healthReward);
            }
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

