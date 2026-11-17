
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class NewMopedWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Auto target stuff")]
    [SerializeField] private float searchRadius = 8f;
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Moped Movement stuff")]
    [SerializeField] private float playerOrbitRadius = 3f;
    [SerializeField] private float orbitSpeed = 1.5f;
    [SerializeField] private float orbitRadiusVariation = 0.5f;

    [SerializeField] private float movementSpeed = 5.5f;
    [SerializeField] private float attackMovementSpeed = 7f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float turnSpeed = 160f;

    [SerializeField] private float passThroughDistance = 4f;
    [SerializeField] private float turnArcOffset = 1.5f;
    [SerializeField] private float recoveryDistance = 5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 1.5f;
    [SerializeField] private float obstacleAvoidanceStrength = 2f;

    [Header("Collision Damage")]
    [SerializeField] private float damageCooldown = 0.5f;

    /*[Header("Tier Modifiers")]
    [SerializeField] private float tier2ScaleMultiplier = 1.3f;
    [SerializeField] private float tier3ScaleMultiplier = 1.5f;
    */

    [Header("Audio")]
    [SerializeField] private AudioClip idleEngineSound;
    [Range(0f, 1f)][SerializeField] private float idleEngineVolume = 1f;
    [SerializeField] private AudioClip travelEngineSound;
    [Range(0f, 1f)][SerializeField] private float travelEngineVolume = 1f;
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)][SerializeField] private float hitVolume = 1f;

    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;

    private AudioSource mopedAudioSource;
    private Rigidbody mopedRigidbody;
    private SphereCollider mopedCollider;

    private PlayerStats playerStats;
    private Transform playerTransform;
    private Transform currentEnemyTarget;

    private float currentSpeed;
    private float orbitAngle;
    private float orbitRadiusOffset;
    private float searchTimer;

    private Vector3 attackDirection;
    private Vector3 attackSideDirection;

    private bool hasPlayedTravelSound;

    //Tracks the next time each enemy can receive damage from the moped.
    //This prevents multiple colliders on one enemy from causing duplicate damage.
    private Dictionary<Transform, float> enemyDamageTimers =
        new Dictionary<Transform, float>();

    private enum MopedState
    {
        OrbitingPlayer,
        DrivingToEnemy,
        ReturningToPlayer
    }

    private MopedState currentState = MopedState.OrbitingPlayer;


    private void Awake()
    {
        //Finds the player and their stats.
        playerStats = GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerTransform = playerStats.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
            else
            {
                Debug.LogError("[MOPED] Could not find Player!");
                enabled = false;
                return;
            }
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.Log("[MOPED] No attack point assigned. Using Moped transform.");
        }

        //The Rigidbody moves the moped while allowing its trigger collider
        //to detect enemies.
        mopedRigidbody = GetComponent<Rigidbody>();

        if (mopedRigidbody == null)
            mopedRigidbody = gameObject.AddComponent<Rigidbody>();

        mopedRigidbody.isKinematic = true;
        mopedRigidbody.useGravity = false;
        mopedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        //The collider detects when the moped runs into enemies.
        mopedCollider = GetComponent<SphereCollider>();

        if (mopedCollider == null)
        {
            Debug.LogError("[MOPED] Missing Sphere Collider!");
            enabled = false;
            return;
        }

        mopedCollider.isTrigger = true;

        mopedAudioSource = GetComponent<AudioSource>();

        if (mopedAudioSource == null)
        {
            mopedAudioSource = gameObject.AddComponent<AudioSource>();
            mopedAudioSource.spatialBlend = 1f;
        }
    }


    private void Start()
    {
        orbitAngle = Random.Range(0f, Mathf.PI * 2f);
        orbitRadiusOffset = Random.Range(-orbitRadiusVariation, orbitRadiusVariation);

        PlaySoundOnce(idleEngineSound, idleEngineVolume);

        Debug.Log("[MOPED] Movement system initialized.");
    }


    private void FixedUpdate()
    {
        if (playerTransform == null || weaponData == null || mopedRigidbody == null)
            return;

        //Searches for enemies while the moped is orbiting or returning.
        searchTimer -= Time.fixedDeltaTime;

        if (currentState != MopedState.DrivingToEnemy && searchTimer <= 0f)
        {
            searchTimer = targetSearchInterval;

            Transform nearestEnemy = GetNearestEnemy();

            if (nearestEnemy != null)
            {
                StartDrivingToEnemy(nearestEnemy);
            }
        }

        //Gets the movement direction based on the current state.
        Vector3 desiredDirection = Vector3.zero;

        switch (currentState)
        {
            case MopedState.OrbitingPlayer:
                desiredDirection = GetOrbitDirection();
                break;

            case MopedState.DrivingToEnemy:
                desiredDirection = GetAttackDirection();
                break;

            case MopedState.ReturningToPlayer:
                desiredDirection = GetReturnDirection();
                break;
        }

        //Avoids obstacles before steering toward the desired direction.
        desiredDirection = ApplyObstacleAvoidance(desiredDirection);

        MoveMoped(desiredDirection);
    }

    private Vector3 GetOrbitDirection()
    {
        //The moped continuously circles the player.
        //Instead of driving between random destinations, it follows a tangent
        //direction around the player with a small correction toward the orbit radius.

        orbitAngle += orbitSpeed * Time.fixedDeltaTime;

        float currentRadius = playerOrbitRadius +
            Mathf.Sin(orbitAngle * 0.7f) * orbitRadiusVariation +
            orbitRadiusOffset;

        Vector3 offset = transform.position - playerTransform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude < 0.01f)
        {
            offset = transform.forward;
        }

        Vector3 radialDirection = offset.normalized;

        //The tangent direction makes the moped travel around the player.
        Vector3 tangentDirection =
            Vector3.Cross(Vector3.up, radialDirection).normalized;

        //Corrects the distance from the player so the moped stays near its orbit.
        float distanceError = currentRadius - offset.magnitude;

        Vector3 correctionDirection =
            radialDirection * distanceError * 1.5f;

        Vector3 desiredDirection =
            tangentDirection + correctionDirection;

        return desiredDirection.normalized;
    }

    private void StartDrivingToEnemy(Transform target)
    {
        if (target == null)
            return;

        currentEnemyTarget = target;
        currentState = MopedState.DrivingToEnemy;

        //Stores the direction the moped should travel through the enemy.
        //This direction remains consistent during the attack run.
        attackDirection = currentEnemyTarget.position - transform.position;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude < 0.01f)
        {
            attackDirection = transform.forward;
            attackDirection.y = 0f;
        }

        attackDirection.Normalize();

        //Creates a sideways offset to make the attack approach less direct.
        attackSideDirection =
            Vector3.Cross(Vector3.up, attackDirection).normalized;

        if (Random.value > 0.5f)
        {
            attackSideDirection *= -1f;
        }

        hasPlayedTravelSound = false;

        Debug.Log("[MOPED STATE] Driving to enemy: " + target.name);
    }


    private Vector3 GetAttackDirection()
    {
        //If the target dies before the moped reaches it, return to the player.
        if (currentEnemyTarget == null)
        {
            StartReturningToPlayer();
            return GetReturnDirection();
        }

        Vector3 targetPosition = currentEnemyTarget.position;

        //The moped aims beyond the enemy so it drives through them.
        //The sideways offset creates a sweeping approach instead of a straight line.
        Vector3 passPosition =
            targetPosition +
            attackDirection * passThroughDistance +
            attackSideDirection * turnArcOffset;

        Vector3 directionToPassPosition = passPosition - transform.position;
        directionToPassPosition.y = 0f;

        //Checks whether the moped has passed the enemy.
        Vector3 positionPastEnemy = transform.position - targetPosition;
        positionPastEnemy.y = 0f;

        float distancePastEnemy =
            Vector3.Dot(positionPastEnemy, attackDirection);

        if (distancePastEnemy >= recoveryDistance)
        {
            StartReturningToPlayer();
            return GetReturnDirection();
        }

        return directionToPassPosition.normalized;
    }

    private void StartReturningToPlayer()
    {
        currentEnemyTarget = null;
        currentState = MopedState.ReturningToPlayer;

        hasPlayedTravelSound = false;

        //The moped will naturally curve back toward its orbit.
        Debug.Log("[MOPED STATE] Returning to player");
    }


    private Vector3 GetReturnDirection()
    {
        Vector3 offset = transform.position - playerTransform.position;
        offset.y = 0f;

        float distanceFromPlayer = offset.magnitude;

        if (distanceFromPlayer < 0.01f)
        {
            currentState = MopedState.OrbitingPlayer;
            return GetOrbitDirection();
        }

        Vector3 radialDirection = offset.normalized;

        //Creates a curved return path by combining inward movement
        //with movement along the orbit.
        Vector3 tangentDirection =
            Vector3.Cross(Vector3.up, radialDirection).normalized;

        float distanceError = distanceFromPlayer - playerOrbitRadius;

        Vector3 inwardCorrection =
            -radialDirection * Mathf.Max(distanceError, 0f) * 0.5f;

        Vector3 desiredDirection =
            tangentDirection + inwardCorrection;

        //Once the moped is back near the player, resume normal orbiting.
        if (distanceFromPlayer <= playerOrbitRadius + 0.75f)
        {
            currentState = MopedState.OrbitingPlayer;
            Debug.Log("[MOPED STATE] Orbiting player");
        }

        return desiredDirection.normalized;
    }

    private void MoveMoped(Vector3 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.001f)
            return;

        //The moped always uses local Z+ as its forward direction.
        Vector3 currentForward = transform.forward;
        currentForward.y = 0f;
        currentForward.Normalize();

        desiredDirection.y = 0f;
        desiredDirection.Normalize();

        //Gradually rotates toward the desired direction instead of snapping.
        Quaternion targetRotation =
            Quaternion.LookRotation(desiredDirection, Vector3.up);

        Quaternion newRotation = Quaternion.RotateTowards(
            mopedRigidbody.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        mopedRigidbody.MoveRotation(newRotation);

        //Changes speed gradually to create acceleration and deceleration.
        float targetSpeed = currentState == MopedState.DrivingToEnemy
            ? attackMovementSpeed
            : movementSpeed;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.fixedDeltaTime
        );

        //Moves in the direction the moped is actually facing.
        //This prevents sideways sliding when it is turning.
        Vector3 movement = newRotation * Vector3.forward;

        Vector3 newPosition =
            mopedRigidbody.position +
            movement * currentSpeed * Time.fixedDeltaTime;

        //Keeps the moped at its existing height.
        newPosition.y = mopedRigidbody.position.y;

        mopedRigidbody.MovePosition(newPosition);
    }


    private Vector3 ApplyObstacleAvoidance(Vector3 desiredDirection)
    {
        //Optional obstacle avoidance.
        //Assign the environment layer to Obstacle Layer in the Inspector.
        //The moped steers away from objects directly in front of it.
        if (obstacleLayer.value == 0 || desiredDirection.sqrMagnitude < 0.001f)
            return desiredDirection;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(
            origin,
            0.4f,
            desiredDirection,
            out RaycastHit hit,
            obstacleCheckDistance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore))
        {
            Vector3 avoidDirection = hit.normal;
            avoidDirection.y = 0f;

            desiredDirection += avoidDirection * obstacleAvoidanceStrength;

            return desiredDirection.normalized;
        }

        return desiredDirection;
    }

    private Transform GetNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            playerTransform.position,
            searchRadius,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return null;

        Transform nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            //Gets the actual enemy root instead of selecting a child collider.
            Transform enemyTransform = GetEnemyTransform(hit);

            if (enemyTransform == null)
                continue;

            float distanceToEnemy =
                Vector3.Distance(transform.position, enemyTransform.position);

            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemyTransform;
            }
        }

        return nearestEnemy;
    }


    private Transform GetEnemyTransform(Collider hit)
    {
        // if melee enemy, find the enemy root
        EnemyController meleeEnemy =
            hit.GetComponentInParent<EnemyController>();

        if (meleeEnemy != null)
            return meleeEnemy.transform;

        // if ranged enemy, find the enemy root
        RangedEnemyController rangedEnemy =
            hit.GetComponentInParent<RangedEnemyController>();

        if (rangedEnemy != null)
            return rangedEnemy.transform;

        // if it LOOKS like a rat, find the enemy root
        RatEnemyController ratEnemy =
            hit.GetComponentInParent<RatEnemyController>();

        if (ratEnemy != null)
            return ratEnemy.transform;

        // ADD MORE ENEMIES HERE

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Only deals damage while driving toward an enemy.
        if (currentState != MopedState.DrivingToEnemy)
            return;

        DealCollisionDamage(other);
    }


    private void DealCollisionDamage(Collider hit)
    {
        //Finding combined damage
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData.baseWeaponDamage + extraDamage;

        //Gets the enemy root so multiple colliders on one enemy don't count separately.
        Transform enemyTransform = GetEnemyTransform(hit);

        if (enemyTransform == null)
            return;

        //Uses the enemy Transform to prevent duplicate damage from multiple colliders.
        if (enemyDamageTimers.TryGetValue(enemyTransform, out float nextDamageTime))
        {
            if (Time.time < nextDamageTime)
                return;
        }

        //Attack speed controls how frequently the moped can damage the same enemy.
        float speedMultiplier =
            playerStats != null ? playerStats.AttackSpeed : 1f;

        float totalAttackSpeed =
            weaponData.baseWeaponAttackSpeed * speedMultiplier;

        float cooldown =
            1f / Mathf.Max(totalAttackSpeed, 0.01f);

        float actualCooldown = Mathf.Max(damageCooldown, cooldown);

        enemyDamageTimers[enemyTransform] = Time.time + actualCooldown;

        // if melee enemy, do damage
        EnemyController meleeEnemy =
            enemyTransform.GetComponent<EnemyController>();

        if (meleeEnemy != null)
        {
            meleeEnemy.TakeDamage(combinedDamage);
            SpawnHitVFX(enemyTransform);

            Debug.Log($"[MOPED] Hit {meleeEnemy.name} for {combinedDamage} total damage!");

            PlayHitSound();
            PlayCollisionVFX();

            return;
        }

        // if ranged enemy, do damage
        RangedEnemyController rangedEnemy =
            enemyTransform.GetComponent<RangedEnemyController>();

        if (rangedEnemy != null)
        {
            rangedEnemy.TakeDamage(combinedDamage);
            SpawnHitVFX(enemyTransform);

            Debug.Log($"[MOPED] Hit {rangedEnemy.name} for {combinedDamage} total damage!");

            PlayHitSound();
            PlayCollisionVFX();

            return;
        }

        // if it LOOKS like a rat, do damage
        RatEnemyController ratEnemy =
            enemyTransform.GetComponent<RatEnemyController>();

        if (ratEnemy != null)
        {
            ratEnemy.TakeDamage(combinedDamage);
            SpawnHitVFX(enemyTransform);

            Debug.Log($"[MOPED] Hit Rat for {combinedDamage} damage!");

            PlayHitSound();
            PlayCollisionVFX();

            return;
        }

        // ADD MORE ENEMIES HERE
    }

    private void PlayHitSound()
    {
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(
                hitSound,
                transform.position,
                hitVolume
            );
        }
    }


    private void PlayCollisionVFX()
    {
        //Play vfx/animation
        if (weaponData.attackVFX != null && attackPoint != null)
        {
            GameObject vfxInstance = Instantiate(
                weaponData.attackVFX,
                attackPoint.position,
                attackPoint.rotation
            );

            VisualEffect vfx;

            vfxInstance.transform.SetParent(transform); //whether vfx attatches to player

            //vfxInstance.transform.localScale = Vector3.one; //scales the vfx

            if (vfxInstance.TryGetComponent(out vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }

            //COME BACK AND CHANGE THISSS
            Destroy(vfxInstance, 1.5f);
        }
    }

    private void SpawnHitVFX(Transform enemyTransform)
    {
        if (hitVFXPrefab != null && enemyTransform != null)
        {
            Vector3 spawnPos = enemyTransform.position + Vector3.up * 1f;

            GameObject vfxInstance = Instantiate(
                hitVFXPrefab,
                spawnPos,
                Quaternion.identity
            );

            if (vfxInstance.TryGetComponent(out VisualEffect vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }

            Destroy(vfxInstance, 1.5f);
        }
    }

    private void PlaySoundOnce(AudioClip clip, float volume)
    {
        if (clip != null && mopedAudioSource != null)
        {
            mopedAudioSource.PlayOneShot(clip, volume);
        }
    }

    public void SetWeaponData(WeaponData newData)
    {
        weaponData = newData;
    }
}