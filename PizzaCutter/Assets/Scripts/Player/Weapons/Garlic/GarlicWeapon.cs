
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

public class GarlicWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform attackPoint; //Spot where VFX spawns
    [SerializeField] private LayerMask enemyLayer;

   

    [Header("Tier Modifiers")]
    //scale is vfx size btw
    [SerializeField] private float tier2ScaleMultiplier = 1.3f; //lowkey these are all kinda useless but ill keep them for now
    [SerializeField] private float tier3ScaleMultiplier = 1.5f;
    [SerializeField] private float tier3KnockbackForce = 5f; //unused for now

    [Header("AOE Entry Settings")]
    [SerializeField] private float entryCooldown = 4f; //the cooldown before the same enemy can trigger entry damage again

    [Header("Poison Settings")]
    [SerializeField] private float poisonDuration = 4f; //how long in seconds the poison lasts on an enemy 
    [SerializeField] private float poisonTickInterval = 1f; //how often poison deals damage initially
    [SerializeField] private float poisonDamagePercent = 0.5f; //the amount of damage poison deals, as a fraction of the aoe damage
    [SerializeField] private GameObject poisonVFXPrefab; //poison effect that spawns on enemies

    [Header("Audio")]
    [SerializeField] private AudioClip pulseSound;
    [Range(0f, 1f)][SerializeField] private float pulseVolume = 1.0f;
    [SerializeField] private AudioClip damageSound;
    [Range(0f, 1f)][SerializeField] private float damageVolume = 1.0f;



    private PlayerStats playerStats;
    private Transform playerTransform;
    private SphereCollider auraCollider; //collision for aoe
    private int lastKnownTier = -1;  
    private GameObject activeVFXInstance; //for tracking the currently active VFX
    //private float nextPulseTime;

    private HashSet<Transform> enemiesInsideAura = new HashSet<Transform>(); //basically a list of whos in the aura, less buggy than with just checking in collider only
    private float nextTickTime;


    private class RecentHit //for handling the entry hit damage, specifically the cooldown before they can get hit again
    {
        public Transform enemyTransform;
        public float expireTime;
    }
    private List<RecentHit> recentHits = new List<RecentHit>();

    private class PoisonedEnemy //for handling the poison effect of enemies
    {
        public MonoBehaviour enemyController; //grabs all enemy scripts as long as theyre set up down below where it says add more enemies
        public Transform enemyTransform;
        public float durationTimer;
        public float nextTickTime;
        public float damagePerTick;
        public GameObject activePoisonVFX;
    }
    private List<PoisonedEnemy> activePoisonedEnemies = new List<PoisonedEnemy>(); //for tracking every enemy thats been poisoned 



    private void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerTransform = playerStats.transform;
        }
        else
        {
            playerTransform = transform.root;
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.Log("no attackpoint set for vfx");
        }

        auraCollider = GetComponent<SphereCollider>();
        auraCollider.isTrigger = true;

    }


    void Start()
    {
        
      if (weaponData != null)
        {
           lastKnownTier = weaponData.tier;
        }
        //CalculateNextPulseTime();
    }




    void Update()
    {
        //for in aoe ticks, OLD LOGIC
        // if (Time.time >= nextPulseTime)
        //{

        //   PerformGarlicPulse();
        // CalculateNextPulseTime();
        // }


        if (weaponData != null && weaponData.tier != lastKnownTier)
        {
            lastKnownTier = weaponData.tier;
            SetupAura(); // for recalculating range and vfx scale when you upgrade
        }

        if (weaponData != null && weaponData.tier >= 2)
        {
            HandleAoeTicks();
        }

        //for ongoing poison damage outside aoe
        UpdatePoisonedEnemies(); 
    }

    //Theres gotta be a better way to do this but this is for making it so the vfx dont rotate with the player's rotation, but still follows their position
    void LateUpdate()
    {
        //for scene transitions 
        if (activeVFXInstance == null && weaponData != null)
        {
            SetupAura();
        }

        if (activeVFXInstance != null && attackPoint != null)
        {
            activeVFXInstance.transform.position = attackPoint.position;
        }
    }



    /* OLD FUNCTION
       void CalculateNextPulseTime()
    {

        float speedMultiplier = playerStats != null ? playerStats.AttackSpeed : 1f;
        float totalAttackSpeed = weaponData.baseWeaponAttackSpeed * speedMultiplier;



        //Converts attacks per second into cooldown seconds, aka pulse frequency in here
        float cooldown = 1f / (Mathf.Max(totalAttackSpeed, 0.01f));
        nextPulseTime = Time.time + cooldown;
    }
    */

    private void SetupAura()
    {
        //Finding combined damage
        //float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        //  float combinedDamage = weaponData.baseWeaponDamage + extraDamage;

        if (weaponData == null) return;

        int currentTier = weaponData.tier;
       float currentRange = weaponData.baseAttackRange * 2;
       float vfxScale = 1;

        if (currentTier == 2)
        {
            currentRange *= 1.75f; //more reach for tier 2
            vfxScale *= 1.75f; 
        }
        else if (currentTier >= 3)
        {
            //even more reach, knockback logic further down + in knockback function
            //(copied from melee weapon but maybe we could use kb, who knows)
            currentRange *= 1.5f; 
            vfxScale *=1.5f;
        }

        auraCollider.radius = currentRange;

        if (activeVFXInstance != null) //destroys old vfx if adding new one
        {
            Destroy(activeVFXInstance);
        }

        //Play vfx/animation
        if (weaponData.attackVFX != null)
        {
            activeVFXInstance = Instantiate(weaponData.attackVFX, attackPoint.position, attackPoint.rotation);
            VisualEffect vfx;
            //activeVFXInstance.transform.SetParent(transform); //whether vfx attatches to player
            activeVFXInstance.transform.localScale = Vector3.one * vfxScale; //scales the vfx


            if (activeVFXInstance.TryGetComponent(out vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }


            //CHECK HERE FOR VFX UPDATING BS
           //Destroy(vfxInstance, 1.0f);
        }

        // OLD CODE FROM MELEE WEAPON

        //Play audio
       /* if (pulseSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(pulseSound, pulseVolume);
        }




        //Hitbox/Enemy detection 
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, currentAttackRange, enemyLayer);

        foreach (var hit in hits)
        {
            //for kb just in case
            Vector3 hitDirection = (hit.transform.position - playerTransform.position).normalized;
            hitDirection.y = 0f;




            // if melee enemy, do damage
            EnemyController meleeEnemy =
                hit.GetComponentInParent<EnemyController>();

            if (meleeEnemy != null)
            {
                meleeEnemy.TakeDamage(combinedDamage);
                ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                Debug.Log($"[MELEE ATTACK] Hit {meleeEnemy.name} for {combinedDamage} total damage!");
                if (damageSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(damageSound, transform.position, damageVolume);
                }

                
                if (currentTier >= 3)
                {
                    ApplyOrRefreshPoison(meleeEnemy, hit.transform, combinedDamage * poisonDamagePercent); 
                }
                continue;
            }

            // if ranged enemy, do damage
            RangedEnemyController rangedEnemy =
                hit.GetComponentInParent<RangedEnemyController>();

            if (rangedEnemy != null)
            {
                rangedEnemy.TakeDamage(combinedDamage);
                ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                Debug.Log($"[MELEE ATTACK] Hit {rangedEnemy.name} for {combinedDamage} total damage!");
                if (damageSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(damageSound, transform.position, damageVolume);
                }

                if (currentTier >= 3)
                {
                    ApplyOrRefreshPoison(meleeEnemy, hit.transform, combinedDamage * poisonDamagePercent);
                }
            }

            // ADD MORE ENEMIES HERE

            


        }
       */ 
    }


    private void OnTriggerEnter(Collider other)
    {
        //making sure on enemy layer 
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        Transform enemyRoot = other.transform.root;
        
        //no duplicates
        if (enemiesInsideAura.Contains(enemyRoot)) return;

        enemiesInsideAura.Add(enemyRoot);

        //preventing back to back/repeated entry hits
        bool alreadyHitRecently = false;
        foreach (var hit in recentHits)
        {
            if (hit.enemyTransform == enemyRoot)
            {
                alreadyHitRecently = true;
                break;
            }
        }

        if (alreadyHitRecently) return; //skips the entry damage if was hit recently

        //Then sets the timer for the entry damage cooldown
        recentHits.Add(new RecentHit
        {
            enemyTransform = enemyRoot,
            expireTime = Time.time + entryCooldown
        });


        Debug.Log("[GARLIC] Enemy entered garlic aoe");

        AudioSource.PlayClipAtPoint(pulseSound, other.transform.position, pulseVolume);


        //TIER 1: Enemeis take damage when initially entering the garlic aoe
        DealDamageToEnemy(other.gameObject, GetCombinedDamage());
    }

    private void OnTriggerExit(Collider other)
    {
        //making sure on enemy layer
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        
        Transform enemyRoot = other.transform.root;
        
        //anything leaving the collider is removed from the list of things in the aura
        enemiesInsideAura.Remove(enemyRoot);

        Debug.Log("[GARLIC] Enemy left garlic aoe");
        // TIER 3: gives lingering poison dot when leaving the aoe
        if (weaponData != null && weaponData.tier >= 3)
        {
            MonoBehaviour enemyScript = GetEnemyController(other.gameObject);
            if (enemyScript != null)
            {
                float poisonDmg = GetCombinedDamage() * poisonDamagePercent;
                ApplyOrRefreshPoison(enemyScript, enemyRoot, poisonDmg);
            }
        }
    


    

        
    }

    private void HandleAoeTicks()
    {
        float speedMultiplier = playerStats != null ? playerStats.AttackSpeed : 1f;
        float totalAttackSpeed = weaponData.baseWeaponAttackSpeed * speedMultiplier;
        float tickInterval = 1f / Mathf.Max(totalAttackSpeed, 0.01f);

        if (Time.time >= nextTickTime)
        {
            //makes sure to remove null enemies (aka when they die)
            enemiesInsideAura.RemoveWhere(t => t == null);

            foreach (var enemyTf in enemiesInsideAura)
            {
                if (enemyTf != null)
                {
                    DealDamageToEnemy(enemyTf.gameObject, GetCombinedDamage() * 0.5f); //Dot damage INSIDE aoe
                    Debug.Log("[POISON] Enemy took " + GetCombinedDamage() * 0.5f + " damage from AOE DOT");
                }
            }

            nextTickTime = Time.time + tickInterval;
        }
    }

    //The timer for the entry cooldown
    private void UpdateRecentHitsCooldown()
    {
        for (int i = recentHits.Count - 1; i >= 0; i--)
        {
            if (recentHits[i].enemyTransform == null || Time.time >= recentHits[i].expireTime)
            {
                recentHits.RemoveAt(i);
            }
        }
    }

    private float GetCombinedDamage() //seperating damage calcs to call it easier for dot and aura enter 
    {
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        return (weaponData != null ? weaponData.baseWeaponDamage : 10f) + extraDamage;
    }

    
    private MonoBehaviour GetEnemyController(GameObject obj) //same thing here, but just getting scripts of enemies
    {
        EnemyController meleeEnemy = obj.GetComponentInParent<EnemyController>();
        if (meleeEnemy != null) return meleeEnemy;

        RangedEnemyController rangedEnemy = obj.GetComponentInParent<RangedEnemyController>();
        if (rangedEnemy != null) return rangedEnemy;

        RatEnemyController ratEnemy = obj.GetComponentInParent<RatEnemyController>();
        if (ratEnemy != null) return ratEnemy;
        
        //ADD MORE ENEMIES HERE

        return null;
    }

    //for actually dealing the calculated damage
    private void DealDamageToEnemy(GameObject enemyObj, float damageAmount)
    {
        Vector3 hitDirection = (enemyObj.transform.position - playerTransform.position).normalized;
        hitDirection.y = 0f;

        EnemyController meleeEnemy = enemyObj.GetComponentInParent<EnemyController>();
        if (meleeEnemy != null)
        {
            meleeEnemy.TakeDamage(damageAmount);
            ApplyKnockbackIfNeeded(enemyObj, hitDirection, 3);
            AudioSource.PlayClipAtPoint(damageSound, enemyObj.transform.position, damageVolume);
            Debug.Log("Enemy took garlic damage");
            return;
        }

        RangedEnemyController rangedEnemy = enemyObj.GetComponentInParent<RangedEnemyController>();
        if (rangedEnemy != null)
        {
            rangedEnemy.TakeDamage(damageAmount);
            ApplyKnockbackIfNeeded(enemyObj, hitDirection, 3);
            AudioSource.PlayClipAtPoint(damageSound,enemyObj.transform.position, damageVolume);
            Debug.Log("Enemy took garlic damage");
            return;
        }
        
        RatEnemyController ratEnemy = enemyObj.GetComponentInParent<RatEnemyController>();
        if (ratEnemy != null)
        {
            ratEnemy.TakeDamage(damageAmount);
            ApplyKnockbackIfNeeded(enemyObj, hitDirection, 3);
            AudioSource.PlayClipAtPoint(damageSound, enemyObj.transform.position, damageVolume);
            Debug.Log("Enemy took garlic damage");
            return;
        }
        //ADD MORE ENEMIES HERE
    }


    //TIER 3 POISON MECHANICS

    private void ApplyOrRefreshPoison(MonoBehaviour enemyScript, Transform enemyTf, float poisonDamage)
    {
        //Checking if enemy is already poisoned
        foreach (var poisoned in activePoisonedEnemies)
        {
            if (poisoned.enemyTransform == enemyTf) //aka if theyre in the aoe again
            {
                //Refreshes duration
                poisoned.durationTimer = poisonDuration;
                return;
            }
        }

        //gives enemy poison vfx
        GameObject spawnedVFX = null;
        if (poisonVFXPrefab != null)
        {
            spawnedVFX = Instantiate(poisonVFXPrefab, enemyTf.position, Quaternion.identity, enemyTf);
        }

        //adds vfx to the class
        activePoisonedEnemies.Add(new PoisonedEnemy
        {
            enemyController = enemyScript,
            enemyTransform = enemyTf,
            durationTimer = poisonDuration,
            nextTickTime = Time.time + poisonTickInterval,
            damagePerTick = poisonDamage,
            activePoisonVFX = spawnedVFX
        });


    }

        //The poison dot/timer logic

        void UpdatePoisonedEnemies()
       {
        for (int i = activePoisonedEnemies.Count - 1; i >= 0; i--)
        {
            PoisonedEnemy p = activePoisonedEnemies[i];

            //If enemy dies, its removed from the list
            if (p.enemyTransform == null || p.enemyController == null)
            {
                activePoisonedEnemies.RemoveAt(i);
                continue;
            }

            p.durationTimer -= Time.deltaTime;

            //Apply poison tick damage over time
            if (Time.time >= p.nextTickTime)
            {
                if (p.enemyController is EnemyController mEnemy)
                {
                    mEnemy.TakeDamage(p.damagePerTick);
                    //add sounds and stuff here
                }
                else if (p.enemyController is RangedEnemyController rEnemy)
                {
                    rEnemy.TakeDamage(p.damagePerTick);
                }

                else if (p.enemyController is RatEnemyController ratEnemy)
                {
                    ratEnemy.TakeDamage(p.damagePerTick);
                }
                
                //ADD MORE ENEMIES HERE

                AudioSource.PlayClipAtPoint(damageSound, p.enemyTransform.position, damageVolume);
                Debug.Log("[POISON] Enemy took " + p.damagePerTick + " damage from poison");
                p.nextTickTime = Time.time + poisonTickInterval;
            }

            //removes poison after timers done along with vfx on them
            if (p.durationTimer <= 0f)
            {
                if (p.activePoisonVFX != null) Destroy(p.activePoisonVFX);
                activePoisonedEnemies.RemoveAt(i);
            }
        }
       }
        


        void ApplyKnockbackIfNeeded(GameObject enemyObj, Vector3 hitDirection, int currentTier)
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






        public void SetWeaponData(WeaponData newData)
        {
            weaponData = newData;
        }

        void OnDrawGizmosSelected()
        {
            if (weaponData != null && attackPoint != null)
            {
                Gizmos.color = Color.green;
                float gizmoRange = weaponData.baseAttackRange * 2;
                if (weaponData.tier == 2) gizmoRange *= 1.75f;
                else if (weaponData.tier >= 3) gizmoRange *= 1.5f;

                Gizmos.DrawWireSphere(attackPoint.position, gizmoRange);
            }
        }

    }



