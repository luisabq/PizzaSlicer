using UnityEngine;
using TMPro; 

public class DeliveryCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText; 
    private static int totalDeliveryCount = 0; 

    private void Start()
    {
        UpdateTextUI(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            totalDeliveryCount++; 
            UpdateTextUI(); 
        }
    }

    private void UpdateTextUI()
    {
        if (counterText != null)
        {
            counterText.text = "Deliveries: " + totalDeliveryCount;
        }
    }
}