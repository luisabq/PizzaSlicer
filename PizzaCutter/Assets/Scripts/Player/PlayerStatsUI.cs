using UnityEngine;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI EXPText;



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
            Debug.Log("SET YOUR HEALTH TEXT WHY I GOTTA DO EVERYTHING FOR YOU");
            healthText = GetComponent<TextMeshProUGUI>();
        }
        //too lazy to make quirky null message for everythbing else just make sure all the texts are set
        
    }





    
    void Update()
    {
        //Health
        if (playerHealth == null || healthText == null) return;

        float hpCurrent = Mathf.Max(0f, playerHealth.CurrentHealth);
        float hpMax = playerStats.maxHealth;


        //makes sure to shows whole numbers
        healthText.text = $"Health: {Mathf.CeilToInt(hpCurrent)} / {Mathf.CeilToInt(hpMax)}";

        //Death Text
        if(hpCurrent <= 0) healthText.text = $"you've been promoted to customer";


        //Level
        float currentLevel = playerStats.currentLevel;
        levelText.text = $"Player Level: {currentLevel}";

        //EXP
        float expCurrent = playerStats.currentXP;
        float expNeeded = playerStats.xpToNextLevel;
        EXPText.text = $"EXP: {Mathf.CeilToInt(expCurrent)} / {Mathf.CeilToInt(expNeeded)}";
    }

}
