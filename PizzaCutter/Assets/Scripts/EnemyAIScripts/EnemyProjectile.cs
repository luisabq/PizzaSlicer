
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody rb;
    private float damage;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    public void Initialize(
        Vector3 direction,
        float damageAmount,
        Transform owner)
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

        Destroy(gameObject, lifetime);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized)
            return;

        // this makes it so it does damage to player
        PlayerHealth playerHealth =
            collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(Mathf.RoundToInt(damage));
        }

        // once projectile hits something it goes *poof* even things that aren't the player
        Destroy(gameObject);
    }
}

