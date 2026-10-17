using UnityEngine;

public class HealthPickup : MonoBehaviour
{


    [Header("Heal value")]
    [SerializeField] private int healAmount = 10;

    [Header("Magnet & Pickup Settings")]
    //[SerializeField] private float magnetRadius = 5f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float pickupDistance = 0.5f;

    [Header("Collect Animation")]
    [SerializeField] private float shrinkSpeed = 10f;

    private Transform playerTransform;
    private bool isBeingCollected = false;
    private PlayerStats playerStats;
    private PlayerHealth playerHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlayerStats>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();

            if (playerStats == null)
            {
                playerStats = playerObj.GetComponentInParent<PlayerStats>();
            }

            if (playerStats == null)
            {
                playerStats = playerObj.GetComponentInChildren<PlayerStats>();
            }

            if (playerHealth == null)
            {
                playerHealth = playerObj.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            }
        }

        // Fallback if the tagged GameObject doesn't hold these components.
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        // Prefer the PlayerStats object as the pickup target, since it provides PickupRadius.
        if (playerStats != null)
        {
            playerTransform = playerStats.transform;
        }
        else if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null || playerStats == null)
        {
            return;
        }


        if (isBeingCollected)
        {
            //Shrinks and destroys whhen collected
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, shrinkSpeed * Time.deltaTime);

            if (transform.localScale.x <= 0.05f)
            {
                Destroy(gameObject);
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= playerStats.PickupRadius)
        {
            //Gravitation
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
            if (distanceToPlayer <= pickupDistance) Collect();
        }

    }


    private void Collect()
    {
        if (isBeingCollected) return;

        if (playerHealth != null)
        {
            playerHealth.Heal(healAmount);
        }

        isBeingCollected = true;

        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
    }

    //theres no way this is optimal x2
    public void SetHealValue(int amount)
    {
        healAmount = amount;
    }
}
