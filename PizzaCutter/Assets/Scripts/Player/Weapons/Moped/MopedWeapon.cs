
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
   // [SerializeField] private float directionChangeMinTime = 0.5f; //for randomized movement 
   // [SerializeField] private float directionChangeMaxTime = 1.5f;
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


    private float primaryTargetNextDamageTime;
    [SerializeField] private float primaryDamageCooldown = 2f;

    private UnityEngine.AI.NavMeshAgent agent;
    private PlayerStats playerStats;
    private Transform playerTransform;

    private Transform currentEnemyTarget;
   // private float nextActionTimer;
    private float nextAttackTime;
    private float donutAngleTimer; //basically represents the angle the moped is at when doing donuts, it increases overtime during a donut
   // private float wanderAngleTimer;


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
            agent.speed = weaponData.baseWeaponAttackSpeed * 3f; //Adjusts moped speed by the base weapon speed....sorta
        }
        //PickNewWanderDestination();
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

    /* 
     * OLD PLAYER ORBIT
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

     */

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


        if (playerTransform != null)
        {
            agent.isStopped = true;
            transform.position = playerTransform.position;
            transform.Rotate(Vector3.down, 360f * Time.deltaTime); //can change spinning speed here
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
          //  PickNewWanderDestination();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(currentEnemyTarget.position);
        
      
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.position);

        //When in range to the enemy, switch to doing donuts around them. rn these donuts USUALLY dont hit the target enemy, but for tier 2+ i made it so it manually just periodically deals damage to the target
        if (distanceToEnemy <= donutOrbitRadius + 1f)
        {
            
            donutAngleTimer = 0f;

            CalculateNextAttackTime();
            nextAttackTime = Time.time;

            currentState = MopedState.DoingDonuts;
            Debug.Log("[MOPED STATE] Starting Donuts around enemy: " + currentEnemyTarget.name);
        }
    }
   





    /* OLD DONUT STATE

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

        if (agent.enabled && agent.isOnNavMesh)
        {
 
            agent.isStopped = false;

            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out UnityEngine.AI.NavMeshHit hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
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

    */

    //donuts 2.0

    private void HandleDoingDonutsState()
    {
        if (currentEnemyTarget == null)
        {
            Transform newEnemy = GetNearestEnemy();
            if (newEnemy != null)
            {
                currentEnemyTarget = newEnemy;
                PlaySoundOnce(travelEngineSound, travelEngineVolume);
                currentState = MopedState.DrivingToEnemy;
            }
            else
            {
                PlaySoundOnce(idleEngineSound, idleEngineVolume);
                Debug.Log("[MOPED STATE] Orbiting player");
                currentState = MopedState.OrbitingPlayer;
                //PickNewWanderDestination();
            }
            return;
        }

        //stops at enemy position
        agent.isStopped = true;
        transform.position = currentEnemyTarget.position;

        //spinning the object, was going to do the model originally but being locked onto the enemy and spinning it this way is much better
        transform.Rotate(Vector3.down, 720f * Time.deltaTime); 

        //attack stuff
        if (Time.time >= nextAttackTime)
        {
            PerformDonutAttack(); 
            CalculateNextAttackTime();
        }

        //range check, if enemy moves range (somehow)
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.position);
        if (distanceToEnemy > searchRadius)
        {
            Debug.Log("[MOPED STATE] Enemy moved away, going back to driving to enemy");
            PlaySoundOnce(travelEngineSound, travelEngineVolume);
            currentState = MopedState.DrivingToEnemy;
        }
    }







    /*
    private void PickNewWanderDestination()
    {
        agent.isStopped = false;

        wanderAngleTimer += Random.Range(1f, 2.5f);

        //gettting a point along the player orbit radius
        float x = Mathf.Cos(wanderAngleTimer) * playerOrbitRadius;
        float z = Mathf.Sin(wanderAngleTimer) * playerOrbitRadius;
        Vector3 destination = playerTransform.position + new Vector3(x, 0f, z);

        //checks if these spots can actually be moved on
        if (UnityEngine.AI.NavMesh.SamplePosition(destination, out UnityEngine.AI.NavMeshHit hit, navMeshSampleDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            Vector3 directionToDest = hit.position - playerTransform.position;
            float distToDest = directionToDest.magnitude;

            if (!Physics.Raycast(playerTransform.position, directionToDest.normalized, distToDest, LayerMask.GetMask("Default", "Obstacles"))) // Adjust layer mask to match your walls/environment
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                //if a wall is in the way,go back to the player
                //agent.SetDestination(playerTransform.position);
            }
        }
        else
        {
            wanderAngleTimer += Mathf.PI;
            agent.SetDestination(playerTransform.position);
        }

        //How often it picks a new target point along its sweeping arc
        nextActionTimer = Random.Range(directionChangeMinTime, directionChangeMaxTime);
    }

    */




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


        //to make it hit the targetted enemy at tier 2 and up
        if (currentTier >= 2 && currentEnemyTarget != null && Time.time >= primaryTargetNextDamageTime)
        {
            bool hitSuccessful = false; //this is for the targetted enemy in the donut, it has a timer so it doesnt take 10000 hits in one second

            if (currentEnemyTarget.TryGetComponent(out EnemyController meleeEnemy))
            {
                meleeEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(currentEnemyTarget);
                Debug.Log($"[MOPED] hit target {meleeEnemy.name} for {combinedDamage} damage!");
                hitSuccessful = true;
            }
            else if (currentEnemyTarget.TryGetComponent(out RangedEnemyController rangedEnemy))
            {
                rangedEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(currentEnemyTarget);
                Debug.Log($"[MOPED] hit target {rangedEnemy.name} for {combinedDamage} damage!");
                hitSuccessful = true;
            }
            else if (currentEnemyTarget.TryGetComponent(out RatEnemyController ratEnemy))
            {
                ratEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(currentEnemyTarget);
                Debug.Log($"[MOPED] hit target {ratEnemy.name} for {combinedDamage} damage!");
                hitSuccessful = true;
            }
            //ADD MORE ENEMIES HERE

            if (hitSuccessful)
            {
                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }

                primaryTargetNextDamageTime = Time.time + primaryDamageCooldown;
            }
        }






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

