
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
        if (meatballPrefab == null)
        {
            Debug.LogError("meatballess gun");
            return;
        }

        //Finding combined damage
        //float combinedDamage = weaponData.baseWeaponDamage + (playerStats != null ? playerStats.baseDamage : 5f); //if no base damage found set to 5
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData.baseWeaponDamage + extraDamage;

        //Shoot direction, defaults to in front of player if no enemies in range
        Vector3 fireDirection;
        if (target != null)
        {
            fireDirection = (target.position - firePoint.position).normalized;
        }
        else
        {
            fireDirection = playerTransform.forward;
        }

        // Spawn Meatball
        GameObject meatballObj = Instantiate(meatballPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));

        if (meatballObj.TryGetComponent<MeatballProjectile>(out var projectile))
        {
            projectile.Initialize(fireDirection, combinedDamage, playerTransform);
        }

        //Hitbox/Enemy detection + damage are all decided in meatballprojectile script, go there!
        
    }

    public void SetWeaponData(WeaponData newData)
    {
        weaponData = newData;
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

