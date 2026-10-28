
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

    [Header("Tier Modifiers")]
    [SerializeField] private float tier2ScaleMultiplier = 1.3f;
    [SerializeField] private float tier3ScaleMultiplier = 1.5f;
    [SerializeField] private float tier3KnockbackForce = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip swingSound;
    [Range(0f, 1f)][SerializeField] private float swingVolume = 1.0f;
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)][SerializeField] private float hitVolume = 1.0f;


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
        int currentTier = weaponData.tier;

        float currentAttackRange = weaponData.baseAttackRange;
        float vfxScale = 1;

        if (currentTier == 2)
        {
            currentAttackRange *= 1.25f; //more reach for tier 2
            vfxScale = tier2ScaleMultiplier;
        }
        else if (currentTier >= 3)
        {
            currentAttackRange *= 1.4f; //even more reach, knockback logic further down + in knockback function
            vfxScale = tier3ScaleMultiplier;
        }


        //Play vfx/animation
        if (weaponData.attackVFX != null)
        {
            GameObject vfxInstance = Instantiate(weaponData.attackVFX, attackPoint.position, attackPoint.rotation);
            VisualEffect vfx;
            vfxInstance.transform.SetParent(transform); //whether vfx attatches to player
            vfxInstance.transform.localScale = Vector3.one * vfxScale; //scales the vfx


            if (vfxInstance.TryGetComponent(out vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }

            Destroy(vfxInstance, 1.5f);
        }

        //Play audio
        if (swingSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(swingSound, swingVolume);
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
                ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                Debug.Log($"[MELEE ATTACK] Hit {meleeEnemy.name} for {combinedDamage} total damage!");
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
                ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);
                Debug.Log($"[MELEE ATTACK] Hit {rangedEnemy.name} for {combinedDamage} total damage!");
                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }
            }

            // if it LOOKS like a rat, do damage
            RatEnemyController ratEnemy =
                hit.GetComponentInParent<RatEnemyController>();

            if (ratEnemy != null)
            {
                ratEnemy.TakeDamage(combinedDamage);
                ApplyKnockbackIfNeeded(hit.gameObject, hitDirection, currentTier);

                Debug.Log($"[MELEE ATTACK] Hit Rat for {combinedDamage} damage!");

                if (hitSound != null && SoundManager.Instance != null)
                {
                    AudioSource.PlayClipAtPoint(
                        hitSound, transform.position, hitVolume);
                }
            }

            // ADD MORE ENEMIES HERE

        }
    }


    private void ApplyKnockbackIfNeeded(GameObject enemyObj, Vector3 hitDirection, int currentTier)
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
            Gizmos.color = Color.red;
            float gizmoRange = weaponData.baseAttackRange;
            if (weaponData.tier == 2) gizmoRange *= 1.25f;
            else if (weaponData.tier >= 3) gizmoRange *= 1.4f;

            Gizmos.DrawWireSphere(attackPoint.position, gizmoRange);
        }
    }

}

