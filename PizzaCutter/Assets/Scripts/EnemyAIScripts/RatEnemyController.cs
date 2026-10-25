
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RatEnemyController : MonoBehaviour
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

    [Header("Erratic Movement")]
    [SerializeField] private float directionChangeMinTime = 0.3f;
    [SerializeField] private float directionChangeMaxTime = 0.65f;
    [SerializeField] private float wanderRadius = 1.5f;
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamageRadius = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)][SerializeField] private float attackVolume = 1.0f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private float currentHealth;
    private float contactDamageTimer;
    private float directionChangeTimer;
    private bool isDead;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (enemyData == null)
        {
            Debug.LogError($"{gameObject.name} is missing EnemyData!");
            enabled = false;
            return;
        }

        currentHealth = enemyData.maxHealth;

        if (agent == null)
        {
            Debug.LogError($"{gameObject.name} is missing a NavMeshAgent!");
            enabled = false;
            return;
        }

        agent.speed = enemyData.moveSpeed;
        agent.stoppingDistance = 0f;

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        PickNextDirection();
    }

    private void Update()
    {
        if (isDead || enemyData == null ||
            player == null || agent == null)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        contactDamageTimer -= Time.deltaTime;
        CheckContactDamage();
        directionChangeTimer -= Time.deltaTime;

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > enemyData.detectionRange)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        if (directionChangeTimer <= 0f ||
            !agent.hasPath ||
            agent.remainingDistance <= 0.3f)
        {
            PickNextDirection();
        }
    }

    private void PickNextDirection()
    {
        if (player == null || agent == null ||
            !agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        Vector3 towardPlayer =
            toPlayer.sqrMagnitude > 0.01f
                ? toPlayer.normalized
                : transform.forward;

        // make that mf chase the player but with a lil bit of spiceee
        Vector3 sideways =
            Vector3.Cross(Vector3.up, towardPlayer) *
            Random.Range(-wanderRadius, wanderRadius);

        Vector3 destination = player.position + sideways;

        if (NavMesh.SamplePosition(
            destination,
            out NavMeshHit hit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(player.position);
        }

        directionChangeTimer = Random.Range(
            directionChangeMinTime,
            directionChangeMaxTime);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other.transform);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamagePlayer(collision.transform);
    }

    private void CheckContactDamage()
    {
        if (isDead || enemyData == null || contactDamageTimer > 0f)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            contactDamageRadius
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth =
                hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = hit.GetComponentInChildren<PlayerHealth>();

            if (playerHealth == null)
                continue;

            playerHealth.TakeDamage(enemyData.attackDamage);

            contactDamageTimer = Mathf.Max(
                0.1f,
                enemyData.attackCooldown
            );

            Debug.Log(
                $"[RAT] Contact damage attempted: {enemyData.attackDamage}"
            );

            return;
        }
    }

    private void TryDamagePlayer(Transform other)
    {
        if (isDead || contactDamageTimer > 0f || enemyData == null)
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInChildren<PlayerHealth>();

        if (playerHealth == null)
            return;

        
        playerHealth.TakeDamage(enemyData.attackDamage);
        contactDamageTimer = Mathf.Max(0.1f, enemyData.attackCooldown);

        Debug.Log($"[RAT] Hit player for {enemyData.attackDamage} damage!");
    }

    public void TakeDamage(float damage)
    {

        if (isDead)
            return;
        GetComponentInChildren<Animator>().SetTrigger("AttackOn");
        AudioSource.PlayClipAtPoint(attackSound, transform.position, attackVolume);
        currentHealth -= damage;

        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(
                hitVFXPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(vfx, vfxDestroyDelay);
        }

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (agent != null && agent.enabled)
            agent.enabled = false;

        if (xpOrbPrefab != null)
        {
            GameObject orbObject = Instantiate(
                xpOrbPrefab,
                transform.position,
                Quaternion.identity);

            EXPOrb orb = orbObject.GetComponent<EXPOrb>();

            if (orb != null)
                orb.SetXPValue(enemyData.xpReward);
        }

        if (healthPickupPrefab != null &&
            Random.value <= enemyData.healthDropChance)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f));

            GameObject pickupObject = Instantiate(
                healthPickupPrefab,
                transform.position + offset,
                Quaternion.identity);

            HealthPickup pickup =
                pickupObject.GetComponent<HealthPickup>();

            if (pickup != null)
                pickup.SetHealValue(enemyData.healthReward);
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

    public void ApplyKnockback(
        Vector3 direction,
        float force,
        float duration = 0.2f)
    {
        StartCoroutine(
            KnockbackRoutine(direction, force, duration));
    }

    private IEnumerator KnockbackRoutine(
        Vector3 direction,
        float force,
        float duration)
    {
        if (agent != null && agent.enabled)
            agent.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(duration);

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        if (agent != null)
        {
            agent.enabled = true;

            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                1.5f,
                NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }
}

