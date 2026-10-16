
using UnityEngine;
using UnityEngine.VFX;

public class MeatballGunWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private GameObject meatballPrefab;
    [SerializeField] private Transform firePoint; //Spot where meatball spawns
    [SerializeField] private LayerMask enemyLayer;

    [Header("Auto target stuff")]
    [SerializeField] private bool autoRotateToTarget = true;
    [SerializeField] private float searchRadius = 12f; // The players target scan range


    private PlayerStats playerStats;
    private Transform playerTransform;
    private float nextAttackTime;

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

        if (firePoint == null)
        {
            firePoint = transform;
            Debug.Log("no firepoint for meatball gun");
        }
    }


    void Start()
    {
        CalculateNextAttackTime();
    }




    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            Transform target = GetNearestEnemy();

            if (autoRotateToTarget && target != null) RotateTowardsTarget(target);

            FireMeatball(target);
            CalculateNextAttackTime();
        }
    }

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

    void RotateTowardsTarget(Transform target)
    {
        if (target == null) return;

        Vector3 direction = (target.position - playerTransform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            playerTransform.rotation = Quaternion.LookRotation(direction);
        }
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

    private void FireMeatball(Transform target)
    {
        if (meatballPrefab == null || weaponData == null)
        {
            Debug.LogError("meatballess gun");
            return;
        }

        int currentTier = weaponData.tier;

        //Finding combined damage
        //float combinedDamage = weaponData.baseWeaponDamage + (playerStats != null ? playerStats.baseDamage : 5f); //if no base damage found set to 5
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData.baseWeaponDamage + extraDamage;

        //Shoot direction, defaults to in front of player if no enemies in range
        Vector3 fireDirection;
        if (target != null)
        {
            fireDirection = (target.position - firePoint.position).normalized;
            fireDirection.y = 0f;
        }
        else
        {
            fireDirection = playerTransform.forward;
        }



        switch (currentTier)
        {
            case 1:
                //TIER 1: normal straight shot at target
                SpawnMeatball(fireDirection, combinedDamage);
                break;

            case 2:
                //TIER 2: 3 shot spread, sorta like a trident, at a target.
                //(good for crowds, but useless really against single target)
                Vector3 leftDir = Quaternion.Euler(0f, -45f, 0f) * fireDirection;
                Vector3 centerDir = fireDirection;
                Vector3 rightDir = Quaternion.Euler(0f, 45f, 0f) * fireDirection;

                SpawnMeatball(leftDir, combinedDamage);
                SpawnMeatball(centerDir, combinedDamage);
                SpawnMeatball(rightDir, combinedDamage);
                break;

            case 3:
                //TIER 3: Homing projectiles, doing this in another script and enabling it so that it doesnt need to track every bullet,
                //the bullets track themselvess
                Vector3 t3LeftDir = Quaternion.Euler(0f, -45f, 0f) * fireDirection;
                Vector3 t3CenterDir = fireDirection;
                Vector3 t3RightDir = Quaternion.Euler(0f, 45f, 0f) * fireDirection;

                GameObject leftMeatball = SpawnMeatball(t3LeftDir, combinedDamage);
                GameObject centerMeatball = SpawnMeatball(t3CenterDir, combinedDamage);
                GameObject rightMeatball = SpawnMeatball(t3RightDir, combinedDamage);

                // Enable homing on all 3 spawned meatballs
                EnableHomingIfPresent(leftMeatball);
                EnableHomingIfPresent(centerMeatball);
                EnableHomingIfPresent(rightMeatball);
                break;
        }


    }



    private GameObject SpawnMeatball(Vector3 direction, float damage)
    {
        GameObject meatballObj = Instantiate(meatballPrefab, firePoint.position, Quaternion.LookRotation(direction));

        //MeatballProjectile projectile = meatballObj.GetComponent<MeatballProjectile>();
        if (meatballObj.TryGetComponent<MeatballProjectile>(out var projectile))
        {
            projectile.Initialize(direction, damage, playerTransform);
        }

        return meatballObj;
    }

        //Hitbox/Enemy detection + damage are all decided in meatballprojectile script, go there!
        
    

    public void SetWeaponData(WeaponData newData)
    {
        weaponData = newData;
    }



    private void EnableHomingIfPresent(GameObject meatballObj)
    {
        if (meatballObj != null && meatballObj.TryGetComponent<MeatballHoming>(out var homing))
        {
            homing.enabled = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (weaponData != null && firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, weaponData.baseAttackRange);
        }
    }

}

