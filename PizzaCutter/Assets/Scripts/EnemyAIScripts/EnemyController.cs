using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyData enemyData;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Enemy Drops")]
    [SerializeField] private GameObject xpOrbPrefab;
    [SerializeField] private GameObject healthPickupPrefab;

    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private float vfxDestroyDelay = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)][SerializeField] private float attackVolume = 1.0f;

    


    private float currentHealth;
    private float attackTimer;

    private Rigidbody rb;

    // NavMesh movement
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (enemyData == null)
        {
            Debug.LogError($"{gameObject.name} is missing EnemyData data duhh!");
            return;
        }

        currentHealth = enemyData.maxHealth;

        if (agent != null)
        {
            agent.speed = enemyData.moveSpeed;
        }
        else
        {
            Debug.LogError($"{gameObject.name} is missing a NavMeshAgent!");
        }

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
        if (enemyData == null || player == null || agent == null)
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
            return;
        }

        if (distanceToPlayer > enemyData.attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            AttackPlayer();
        }
    }

    private void AttackPlayer()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
           //sets a trigger to tell the animator to play the attack animation
           GetComponentInChildren<Animator>().SetTrigger("AttackOn");
            DealDamageToPlayer();
            attackTimer = enemyData.attackCooldown;
        }
    }

    private void DealDamageToPlayer()
    {
        PlayerHealth playerHealth = player != null
            ? player.GetComponentInParent<PlayerHealth>()
            : null;

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(enemyData.attackDamage);
            AudioSource.PlayClipAtPoint(attackSound, transform.position, attackVolume);
            Debug.Log($"{enemyData.enemyName} attacked player for {enemyData.attackDamage} damage.");
        }
        else
        {
            Debug.LogWarning("Melee enemy could not find PlayerHealth!");
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            $"{enemyData.enemyName} took {damage} damage. " +
            $"HP: {currentHealth}/{enemyData.maxHealth}"
        );

        SpawnHitVFX(transform.position);

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

        if (healthPickupPrefab != null && Random.value <= enemyData.healthDropChance)
        {
            //offsetting so xp and health arent overlapping
            Vector3 spawnOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
            GameObject healthObj = Instantiate(healthPickupPrefab, transform.position + spawnOffset, Quaternion.identity);

            if (healthObj.TryGetComponent<HealthPickup>(out var healthScript))
            {
                healthScript.SetHealValue(enemyData.healthReward);
            }
        }

        Destroy(gameObject);
    }

    private void SpawnHitVFX(Vector3 spawnPosition)
    {
        if (hitVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(hitVFXPrefab, spawnPosition, Quaternion.identity);
            Destroy(vfxInstance, vfxDestroyDelay);
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return enemyData != null ? enemyData.maxHealth : 0f;
    }


    public void ApplyKnockback(Vector3 direction, float force, float duration = 0.2f)
    {
        StartCoroutine(KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force, float duration)
    {
        if (agent != null && agent.enabled) //turns off navmesh stuff temporarily
        {
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(duration);

        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (agent != null)
        {
            //makes sure agent is in the correct position, then turns back on navmesh stuff
            
            agent.enabled = true;

            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                agent.Warp(transform.position);
            }

        }
    }


}
