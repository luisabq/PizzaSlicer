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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null)
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

        isBeingCollected = true;

        // Give health
        if (playerTransform != null && playerTransform.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            playerHealth.Heal(healAmount);
        }


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
