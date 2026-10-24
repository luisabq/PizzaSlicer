
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeatballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitVFXPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [Range(0f, 1f)][SerializeField] private float shootVolume = 1.0f;
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)][SerializeField] private float hitVolume = 1.0f;


    private Rigidbody rb;
    private float damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    public void Initialize(Vector3 direction, float damageAmount, Transform owner)
    {
        damage = damageAmount;

        // no collision from the object that spawned this bullet
        Collider[] ownerColliders =
            owner.GetComponentsInChildren<Collider>();

        Collider projectileCollider = GetComponent<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            if (ownerCollider != null && projectileCollider != null)
            {
                Physics.IgnoreCollision(
                    projectileCollider,
                    ownerCollider
                );
            }
        }

        direction.Normalize();

        rb.useGravity = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearVelocity = direction * speed;

        initialized = true;

        if (shootSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(shootSound, shootVolume);
        }



        Destroy(gameObject, lifetime);
    }


      private void OnCollisionEnter(Collision collision)
      {
          if (!initialized)
              return;


          EnemyController meleeEnemy = collision.gameObject.GetComponentInParent<EnemyController>();
          if (meleeEnemy != null)
          {
              meleeEnemy.TakeDamage(damage);
              Debug.Log($"[MEATBALL] Hit enemy {meleeEnemy.name} for {damage} damage!");
              Impact();
              return;
          }

          RangedEnemyController rangedEnemy = collision.gameObject.GetComponentInParent<RangedEnemyController>();
          if (rangedEnemy != null)
          {
              rangedEnemy.TakeDamage(damage);
              Debug.Log($"[MEATBALL] Hit enemy {rangedEnemy.name} for {damage} damage!");
              Impact();
              return;
          }

        RatEnemyController ratEnemy = collision.gameObject.GetComponentInParent<RatEnemyController>();
         if (ratEnemy != null)
         {
             ratEnemy.TakeDamage(damage);
             Debug.Log($"[MEATBALL] Hit Rat {ratEnemy.name} for {damage} damage!");
             Impact();
             return;
         }
        
         //For hitting anything else
        Impact();
      }

     


    /*private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Meatball") || other.CompareTag("VFX")) return;


        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"[MEATBALL] Hit enemy {enemy.name} for {damage} damage!");
            Impact();
            return;
        }

        RangedEnemyController rangedEnemy = other.GetComponentInParent<RangedEnemyController>();
        if (rangedEnemy != null)
        {
            rangedEnemy.TakeDamage(damage);
            Debug.Log($"[MEATBALL] Hit enemy {rangedEnemy.name} for {damage} damage!");
            Impact();
            return;
        }
        Impact();

    }
    */

     //ADD MORE ENEMIES HERE
       
    
    
    private void Impact()
    {
        if (hitVFXPrefab != null)
        {
            if (hitSound != null && SoundManager.Instance != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                //SoundManager.Instance.PlaySound(hitSound, hitVolume);
            }
            GameObject vfx = Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }

        Destroy(gameObject);
    }  





}

