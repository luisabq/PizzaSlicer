using UnityEngine;

public class EXPOrb : MonoBehaviour
{


    [Header("XP Value")]
    [SerializeField] private int xpAmount = 10;

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
            playerStats = playerObj.GetComponent<PlayerStats>();

            if (playerStats == null)
            {
                playerStats = playerObj.GetComponentInParent<PlayerStats>();
            }

            if (playerStats == null)
            {
                playerStats = playerObj.GetComponentInChildren<PlayerStats>();
            }
        }

        // Fallback if the tagged GameObject doesn't hold PlayerStats.
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        // Use the object that actually owns PlayerStats as the target.
        if (playerStats != null)
        {
            playerTransform = playerStats.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null || playerStats == null) return;

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

        // Give XP points to player
        if (playerStats != null)
        {
            playerStats.AddXP(xpAmount);
        }

        isBeingCollected = true;

        //supposed to disable colliders in order to not collect multiple times, but might mess things up so look at this first lol
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
    }

    //theres no way this is optimal
    public void SetXPValue(int amount)
    {
        xpAmount = amount;
    }
}
