using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform destination; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TeleportPlayer(other.gameObject);
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;

        if (cc != null)
        {
            cc.enabled = true;
            Physics.SyncTransforms(); 
        }
    }
}
