using UnityEngine;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshProUGUI healthText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                playerStats = player.GetComponent<PlayerStats>();
            }
        }

        if (healthText == null)
        {
            healthText = GetComponent<TextMeshProUGUI>();
        }

        
    }





    
    void Update()
    {
        if (playerHealth == null || healthText == null) return;

        float current = Mathf.Max(0f, playerHealth.CurrentHealth);
        float max = playerStats.maxHealth;

        //makes sure to shows whole numbers
        healthText.text = $"Health: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        if(current <= 0) healthText.text = $"you've been promoted to customer";

    }

}
