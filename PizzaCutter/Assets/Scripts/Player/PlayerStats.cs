using UnityEngine;

public class PlayerStats : MonoBehaviour
{

    [Header("Base Stat Points")]
    public int moveSpeedPoints = 0;
    public int attackSpeedPoints = 0;
    public int damagePoints = 0;
    public int maxHealthPoints = 0;
    public int iFramePoints = 0;
    public int pickupRadiusPoints = 0;

    //base values in relation to each stat's logic
    [Header("Base Stats")]
   // public float baseDamage = 15f; 
   // public float attackSpeedMultiplier = 0.6f;
   // public float maxHealth = 30f;

    
    [SerializeField] public float baseMoveSpeed = 4f;
    [SerializeField] public float baseAttackSpeed = 0.4f; 
    [SerializeField] public float baseDamage = 15f; //used with weapon damage
    [SerializeField] public float baseMaxHealth = 30f;
    [SerializeField] public float baseIFrameDuration = 1f;
    [SerializeField] public float basePickupRadius = 2f;


    //Basically stat point converision rate, how much a single stat point will increase the stat
    //WILL ABSOLUTELY NEED SOME ADJUSTMENT, THIS IS THE KEY FOR BALANCING THE GAME!!
    [Header("Gains Per Stat Point")]
    [SerializeField] private float moveSpeedPerPoint = 0.5f;
    [SerializeField] private float attackSpeedPerPoint = 0.1f;
    [SerializeField] private float damagePerPoint = 3f;
    [SerializeField] private float maxHealthPerPoint = 10f;
    [SerializeField] private float iFramePerPoint = 0.2f;
    [SerializeField] private float pickupRadiusPerPoint = 0.5f;



    [Header("Level System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public float neededXPPerLevelScale = 1.25f;
    //public float healthIncreasePerLevel = 20f;
    //public float damageIncreasePerLevel = 5f;
    public float healPercentFromLevel = 0.3f; 

    private PlayerHealth playerHealth;


    //btw => lets you set the value and stuff without needing to do 'get' and 'return' stuff,
    //and is updated everytime its called/read instead of being run once
    public float MoveSpeed => baseMoveSpeed + (moveSpeedPoints * moveSpeedPerPoint);
    public float AttackSpeed => baseAttackSpeed + (attackSpeedPoints * attackSpeedPerPoint);
    public float Damage => baseDamage + (damagePoints * damagePerPoint);
    public float MaxHealth => baseMaxHealth + (maxHealthPoints * maxHealthPerPoint);
    public float IFrameDuration => baseIFrameDuration + (iFramePoints * iFramePerPoint);
    public float PickupRadius => basePickupRadius + (pickupRadiusPoints * pickupRadiusPerPoint);







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

    
    public void LevelUp()
    {
        currentLevel++;

        //game pausing stuff for stat selection
        Time.timeScale = 0f;

        if (LevelUpUI.Instance != null)
        {
            LevelUpUI.Instance.ShowLevelUpChoices();
        }
    }

    /*  OLD CODE - that i dont wanna delete because im like what if we use it again even tho we def wont but like what if
      baseDamage += damageIncreasePerLevel;
         maxHealth += healthIncreasePerLevel;
         if (playerHealth != null)
         {
             playerHealth.Heal(Mathf.Round((float)(healPercentFromLevel * maxHealth)));
         }

         Debug.Log($"[LEVEL UP!] Reached Level {currentLevel}! Max HP: {maxHealth}, Damage: {baseDamage}"); */


    //basically a buncha if and else statements, if movespeed called, then do that stuff and stop
    //else check if next stat was called
    public void ApplyStatUpgrade(StatType stat, int points)
    {
        switch (stat)
        {
            case StatType.MoveSpeed: moveSpeedPoints += points; break;
            case StatType.AttackSpeed: attackSpeedPoints += points; break;
            case StatType.Damage: damagePoints += points; break;
            case StatType.MaxHealth:
                maxHealthPoints += points;
                if (playerHealth != null)
                {
                    playerHealth.Heal(Mathf.Round((float)(healPercentFromLevel * MaxHealth)));
                }
                break;
            case StatType.IFrames: iFramePoints += points; break;
            case StatType.PickupRadius: pickupRadiusPoints += points; break;
        }
        Time.timeScale = 1f; 
    }

    //gives random stat increase amount
    public StatUpgradeOption RollRandomUpgrade()
    {
        //randomly decides which of the stats to offer
        StatType randomStat = (StatType)Random.Range(0, System.Enum.GetValues(typeof(StatType)).Length);


        //randomly decides from 1 to 3 points for that stat, weighted. +3 has a lower chance, etc..
        float roll = Random.value; 
        int points = 1;

        if (roll <= 0.10f)       
        {
            points = 3;
        }
        else if (roll <= 0.40f)  
        {
            points = 2;
        }
        
        return new StatUpgradeOption { statType = randomStat, pointsGained = points };
    }











}





