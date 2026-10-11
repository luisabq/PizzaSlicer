using UnityEngine;
using UnityEngine.VFX;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform attackPoint; //Spot where VFX spawns
    [SerializeField] private LayerMask enemyLayer;

    [Header("Auto target stuff")]
    [SerializeField] private bool autoRotateToTarget = true;
    [SerializeField] private float searchRadius = 8f; // The players target scan range


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

        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.Log("no attackpoint set for vfx");
        }
    }


    void Start()
    {
        CalculateNextAttackTime();
    }




    void Update()
    {
        //autoswing
        if (Time.time >= nextAttackTime)
        {
            //Rotates player to nearest enemy right before swinging
            if (autoRotateToTarget)
            {
                RotateTowardsNearestEnemy();
            }

            PerformAttack();
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

        void RotateTowardsNearestEnemy()
     {
        Transform target = GetNearestEnemy();
        if (target == null) return;

        Vector3 direction = (target.position - playerTransform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerTransform.rotation = targetRotation;
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

     void PerformAttack()
     {
        //Finding combined damage
        //float combinedDamage = weaponData.baseWeaponDamage + (playerStats != null ? playerStats.baseDamage : 5f); //if no base damage found set to 5
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData.baseWeaponDamage + extraDamage;



        //Play vfx/animation
        if (weaponData.attackVFX != null)
        {
            GameObject vfxInstance = Instantiate(weaponData.attackVFX, attackPoint.position, attackPoint.rotation);
            VisualEffect vfx;
            vfxInstance.transform.SetParent(transform); //whether vfx attatches to player

           
            if (vfxInstance.TryGetComponent(out vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }

            Destroy(vfxInstance, 1.5f); 
        }

        //Hitbox/Enemy detection 
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, weaponData.baseAttackRange, enemyLayer);

        foreach (var hit in hits)
        {
            // Try to get EnemyController directly on the object hit
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(combinedDamage);
                Debug.Log($"[MELEE ATTACK] Hit {enemy.name} for {combinedDamage} total damage!");
            }
        }
     }

     void OnDrawGizmosSelected()
     {
        if (weaponData != null && attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, weaponData.baseAttackRange);
        }
     }

}
