using UnityEngine;

public class PlayerStats : MonoBehaviour
{

    [Header("Base Stats")]
    public float baseDamage = 5f; // changes with player upgrades and used with weapon damage to calculate combined damage
    public float attackSpeedMultiplier = 1f;
    public float maxHealth = 100f;

    [Header("Level System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public float neededXPPerLevelScale = 1.25f;
    public float healthIncreasePerLevel = 20f;
    public float damageIncreasePerLevel = 5f;

    private PlayerHealth playerHealth;








    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }


    public void AddXP(int amount)
    {
        currentXP += amount;
        Debug.Log($"[XP] Gained {amount} XP! Total: {currentXP}/{xpToNextLevel}");

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;

            //rn scales the exp needed for the next level by a set amount each level.
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * neededXPPerLevelScale); 
            LevelUp();
        }
    }

    //NEED TO ADJUST AFTER PROTOTYPE TO ALLOW CHOOSING STATS INSTEAD
    //AS OF NOW, INCREASES MAX DAMAGE, HEALTH AND HEALS YOU TO FULL
    public void LevelUp()
    {
        currentLevel++;
        baseDamage += damageIncreasePerLevel;
        maxHealth += healthIncreasePerLevel;
        if (playerHealth != null)
        {
            playerHealth.Heal(maxHealth);
        }

        Debug.Log($"[LEVEL UP!] Reached Level {currentLevel}! Max HP: {maxHealth}, Damage: {baseDamage}");
    }
}





