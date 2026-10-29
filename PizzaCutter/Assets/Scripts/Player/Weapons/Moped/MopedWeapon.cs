
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.AI;

public class MopedWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform attackPoint; //Spot where VFX spawns (would prob call it like, exhaust point but im lazy im sorry)
    [SerializeField] private LayerMask enemyLayer;

    [Header("Auto target stuff")]
    [SerializeField] private float navMeshSampleDistance = 2f; //basically the search radius for navmesh move points (?)
    [SerializeField] private float searchRadius = 8f; //The bike's detection range for enemies

    [Header("Moped Movement stuff")]
    [SerializeField] private float playerOrbitRadius = 3f;    //orbit radius around player when no enemies detected 
    [SerializeField] private float directionChangeMinTime = 0.5f; //for randomized movement 
    [SerializeField] private float directionChangeMaxTime = 1.5f;
    [SerializeField] private float donutOrbitRadius = 2.5f;     //Radius of the donut circle when moped goes around an enemy

    /*[Header("Tier Modifiers")]
    [SerializeField] private float tier2ScaleMultiplier = 1.3f;
    [SerializeField] private float tier3ScaleMultiplier = 1.5f;
    */

    [Header("Audio")]
    [SerializeField] private AudioClip idleEngineSound;
    [Range(0f, 1f)][SerializeField] private float idleEngineVolume = 1.0f;
    [SerializeField] private AudioClip travelEngineSound;
    [Range(0f, 1f)][SerializeField] private float travelEngineVolume = 1.0f;
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)][SerializeField] private float hitVolume = 1.0f;

    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;

    private AudioSource mopedAudioSource;


    private UnityEngine.AI.NavMeshAgent agent;
    private PlayerStats playerStats;
    private Transform playerTransform;

    private Transform currentEnemyTarget;
    private float nextActionTimer;
    private float nextAttackTime;
    private float donutAngleTimer; //basically represents the angle the moped is at when doing donuts, it increases overtime during a donut

    private enum MopedState { OrbitingPlayer, DrivingToEnemy, DoingDonuts } //easy handling of different moped states
    private MopedState currentState = MopedState.OrbitingPlayer;


    private void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        agent = GetComponent<NavMeshAgent>();

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
                Debug.LogError("[MOPED] isnt finding the player object" );
            }
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.Log("no attackpoint set for vfx");
        }

        if (agent == null)
        {
            Debug.LogError($"{gameObject.name} is missing a NavMeshAgent!");
            enabled = false;
            return;
        }

        mopedAudioSource = GetComponent<AudioSource>();
        if (mopedAudioSource == null)
        {
            mopedAudioSource = gameObject.AddComponent<AudioSource>();
            mopedAudioSource.spatialBlend = 1f;

        }
    }



    void Start()
    {
        if (agent != null && weaponData != null)
        {
            agent.speed = weaponData.baseWeaponAttackSpeed * 3f; //Adjusts moped speed by the base weapon speed. currently no change in speed 
        }
        PickNewWanderDestination();
    }




    void Update()
    {
        if (playerTransform == null || agent == null || weaponData == null)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        //constantly checks for the moped's current state and runs the related function
        switch (currentState)
        {
            case MopedState.OrbitingPlayer:
                HandleOrbitingPlayerState();
                break;

            case MopedState.DrivingToEnemy:
                HandleDrivingToEnemyState();
                break;

            case MopedState.DoingDonuts:
                HandleDoingDonutsState();
                break;
        }

    }

    
    private void HandleOrbitingPlayerState()
    {
       
        //constantly scans for nearby enemies and locks on if found
        Transform nearestEnemy = GetNearestEnemy();
        if (nearestEnemy != null)
        {
            currentEnemyTarget = nearestEnemy;
            currentState = MopedState.DrivingToEnemy;
            Debug.Log("[MOPED STATE] Driving to enemy");
            PlaySoundOnce(travelEngineSound, travelEngineVolume);
            return;
        }

       
        //Otherwise, will orbit/wander around the player
        nextActionTimer -= Time.deltaTime; //when its allowed to pick a new random spot around the player
        if (nextActionTimer <= 0f || !agent.hasPath || agent.remainingDistance <= 0.5f) //this helps deal with sus positioning, so itll pick a new spot if it takes too long to get to where it wants to go, theres no path, or its in range of the desired spot
        {
            PickNewWanderDestination();
        }
    }

    private void HandleDrivingToEnemyState()
    {
        
        //when locked on target dies go back to player 
        if (currentEnemyTarget == null)
        {
            PlaySoundOnce(idleEngineSound, idleEngineVolume);
            Debug.Log("[MOPED STATE] Orbiting player");
            currentState = MopedState.OrbitingPlayer;
            PickNewWanderDestination();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(currentEnemyTarget.position);
        
      
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.position);

        //When in range to the enemy, switch to doing donuts around them. Can also offset the donut here if we wanna make the locked on enemy not be hit by the donut or not
        if (distanceToEnemy <= donutOrbitRadius + 1f)
        {
            
            donutAngleTimer = 0f;

            CalculateNextAttackTime();
            nextAttackTime = Time.time;

            currentState = MopedState.DoingDonuts;
            Debug.Log("[MOPED STATE] Starting Donuts around enemy: " + currentEnemyTarget.name);
        }
    }

    private void HandleDoingDonutsState()
    {
       
        //again, if target died, look for new enemy or return to player
        if (currentEnemyTarget == null)
        {
            Transform newEnemy = GetNearestEnemy();
            if (newEnemy != null)
            {
                currentEnemyTarget = newEnemy;
                currentState = MopedState.DrivingToEnemy;
                //Debug.Log("[MOPED STATE] Donuts around enemy:" + currentEnemyTarget + " !");
            }
            else
            {
                PlaySoundOnce(idleEngineSound, idleEngineVolume);
                Debug.Log("[MOPED STATE] Orbiting player");
                currentState = MopedState.OrbitingPlayer;
                PickNewWanderDestination();
            }
            return;
        }

        //calculating circular position around the target enemy
        donutAngleTimer += Time.deltaTime * (agent.speed * 0.5f); //wpuld donut faster with faster attack speed
        float x = Mathf.Cos(donutAngleTimer) * donutOrbitRadius; //the cos and sin return values between -1 and 1 based on the timer, and then the orbit radius multiplication increases this size if needed
        float z = Mathf.Sin(donutAngleTimer) * donutOrbitRadius;
        Vector3 donutOffset = new Vector3(x, 0f, z); //sort of like the distance and direction to the circle edge
        Vector3 targetPosition = currentEnemyTarget.position + donutOffset; //targetPosion is the moped's target on the "donut track", that it keeps following to go in a circle

        if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out UnityEngine.AI.NavMeshHit hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas)) //checks if the moped can actually go to that spot on the donut circle track, and if so, it does.
        {
            agent.SetDestination(hit.position);
        }

        //during donuts, periodically calls to deal damage to enemies it collides with
        if (Time.time >= nextAttackTime)
        {
            PerformDonutAttack();
            CalculateNextAttackTime();
        }

        //might be buggy but if the enemy gets too far from the search range itll go back to driving at it and then do the donuts again
        if (Vector3.Distance(transform.position, currentEnemyTarget.position) > searchRadius * 1.5f)
        {
            Debug.Log("[MOPED STATE] going back to driving to enemy");
            currentState = MopedState.DrivingToEnemy;
        }
    }


    private void PickNewWanderDestination()
    {
        agent.isStopped = false;

        //strays randomly when around player to look like a loose vehicle
        Vector2 randomCircle = Random.insideUnitCircle * playerOrbitRadius; //picks a random point in a circle, sized to the custom size.
        Vector3 destination = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y); //putting that circle in 3d space based ont he players current position

        //checks if these spots can actually be moved on
        if (UnityEngine.AI.NavMesh.SamplePosition(destination, out UnityEngine.AI.NavMeshHit hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        //for now, if it cant get to a spot itll go to the player (since thats def a spot it can go to)
        else
        {
            agent.SetDestination(playerTransform.position);
        }

        nextActionTimer = Random.Range(directionChangeMinTime, directionChangeMaxTime); //resets the timer and picks a random time to swerve off path
    }






    //the same as melee weapon detection
    private Transform GetNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, searchRadius, enemyLayer);

        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        Transform nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            float distanceToEnemy = Vector3.Distance(playerTransform.position, hit.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = hit.transform;
            }
        }

        return nearestEnemy;
    }

       


    void CalculateNextAttackTime()
    {

        // float totalAttackSpeed = weaponData.baseWeaponAttackSpeed * (playerStats != null ? playerStats.attackSpeedMultiplier : 1f); //if no attackspeed found, set to 1
        float speedMultiplier = playerStats != null ? playerStats.AttackSpeed : 1f;
        float totalAttackSpeed = weaponData.baseWeaponAttackSpeed * speedMultiplier;
       
        //Converts attacks per second into cooldown seconds
        float cooldown = 1f / (Mathf.Max(totalAttackSpeed, 0.01f));
        nextAttackTime = Time.time + cooldown;
    }

    void PerformDonutAttack()
    {
        //Finding combined damage
        
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData.baseWeaponDamage + extraDamage;
        int currentTier = weaponData.tier;

        float currentAttackRange = weaponData.baseAttackRange; //kinda unused but keeping just in case
       // float vfxScale = 1;

       /* if (currentTier == 2)
        {
            currentAttackRange *= 1.25f; //more reach for tier 2
            vfxScale = tier2ScaleMultiplier;
        }
        else if (currentTier >= 3)
        {
            currentAttackRange *= 1.4f; //even more reach, knockback logic further down + in knockback function
            vfxScale = tier3ScaleMultiplier;
        }
       */

        //Play vfx/animation
        if (weaponData.attackVFX != null)
        {
            GameObject vfxInstance = Instantiate(weaponData.attackVFX, attackPoint.position, attackPoint.rotation);
            VisualEffect vfx;
            vfxInstance.transform.SetParent(transform); //whether vfx attatches to player
            //vfxInstance.transform.localScale = Vector3.one * vfxScale; //scales the vfx


            if (vfxInstance.TryGetComponent(out vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }
            
            //COME BACK AND CHANGE THISSS
            Destroy(vfxInstance, 1.5f);
        }

       

        //Hitbox/Enemy detection 
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, weaponData.baseAttackRange, enemyLayer);

        foreach (var hit in hits)
        {

            Vector3 hitDirection = (hit.transform.position - playerTransform.position).normalized;
            hitDirection.y = 0f;




            // if melee enemy, do damage
            EnemyController meleeEnemy =
                hit.GetComponentInParent<EnemyController>();

            if (meleeEnemy != null)
            {
                meleeEnemy.TakeDamage(combinedDamage);
                // ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                SpawnHitVFX(hit.transform);
                Debug.Log($"[MOPED] Hit {meleeEnemy.name} for {combinedDamage} total damage!");
                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }
                continue;
            }

            // if ranged enemy, do damage
            RangedEnemyController rangedEnemy =
                hit.GetComponentInParent<RangedEnemyController>();

            if (rangedEnemy != null)
            {
                rangedEnemy.TakeDamage(combinedDamage);
                // ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                SpawnHitVFX(hit.transform);
                Debug.Log($"[MOPED] Hit {rangedEnemy.name} for {combinedDamage} total damage!");
                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }
                continue;
            }

            // if it LOOKS like a rat, do damage
            RatEnemyController ratEnemy =
                hit.GetComponentInParent<RatEnemyController>();

            if (ratEnemy != null)
            {
                ratEnemy.TakeDamage(combinedDamage);
                // ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                SpawnHitVFX(hit.transform);

                Debug.Log($"[MOPED] Hit Rat for {combinedDamage} damage!");

                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }
            }

            // ADD MORE ENEMIES HERE

        }
    }


    /*  private void ApplyKnockbackIfNeeded(GameObject enemyObj, Vector3 hitDirection, int currentTier)
      {
          if (currentTier >= 3)
          {
              EnemyController meleeEnemy = enemyObj.GetComponentInParent<EnemyController>();
              if (meleeEnemy != null)
              {
                  meleeEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
                  return;
              }

              RangedEnemyController rangedEnemy = enemyObj.GetComponentInParent<RangedEnemyController>();
              if (rangedEnemy != null)
              {
                  rangedEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
              }

              RatEnemyController ratEnemy = enemyObj.GetComponentInParent<RatEnemyController>();

              if (ratEnemy != null)
              {
                  ratEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
              }

              //ADD MORE ENEMIES HERE

          }
      }

      */


    private void SpawnHitVFX(Transform enemyTransform)
    {
        if (hitVFXPrefab != null && enemyTransform != null)
        {
            Vector3 spawnPos = enemyTransform.position + Vector3.up * 1f;
            GameObject vfxInstance = Instantiate(hitVFXPrefab, spawnPos, Quaternion.identity);
            
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
        if (agent != null && weaponData != null)
        {
            agent.speed = weaponData.baseWeaponAttackSpeed * 3f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (weaponData != null && attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            float gizmoRange = weaponData.baseAttackRange;
           
            Gizmos.DrawWireSphere(attackPoint.position, gizmoRange);
        }
    }

}

