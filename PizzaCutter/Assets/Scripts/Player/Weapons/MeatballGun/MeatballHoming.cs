using UnityEngine;

public class MeatballHoming : MonoBehaviour
{
    [SerializeField] private float homingSpeed = 5f;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        GameObject nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null && rb != null)
        {
            //enemy direction
            Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;

            //steering to the enemy 
            Vector3 newVelocity = Vector3.RotateTowards(rb.linearVelocity, direction * rb.linearVelocity.magnitude, homingSpeed * Time.fixedDeltaTime, 0.0f);
            rb.linearVelocity = newVelocity;
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                closest = enemy;
            }
        }

        return closest;
    }
   
}
