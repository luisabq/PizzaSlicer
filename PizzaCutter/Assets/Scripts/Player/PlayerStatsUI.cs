using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI EXPText;

    [Header("Progress Bars")]
    [SerializeField] private Image healthBarFill; //the fill image that shrinks/grows, NOT the background
    [SerializeField] private Image xpBarFill;
    [SerializeField] private float barFillSpeed = 1.5f; //how much of the bar it can slide per second (1 = full bar in 1 sec), 0 = snap instantly

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

        SetupFillImage(healthBarFill);
        SetupFillImage(xpBarFill);

        //starts the bars at the right spot instead of sliding in from empty
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = GetHealthPercent();
        }

        if (xpBarFill != null)
        {
            xpBarFill.fillAmount = GetXPPercent();
        }
        
    }
    
    void Update()
    {
        //shows HP values
        if (playerHealth == null || healthText == null) return;

        float hpCurrent = Mathf.Max(0f, playerHealth.CurrentHealth);
        float hpMax = playerStats.MaxHealth;


        //makes sure to shows whole numbers
        healthText.text = $"Health: {Mathf.CeilToInt(hpCurrent)} / {Mathf.CeilToInt(hpMax)}";

        //when you die
        if(hpCurrent <= 0) healthText.text = $"you've been promoted to customer";


        //the number for level
        float currentLevel = playerStats.currentLevel;
        levelText.text = $"Player Level: {currentLevel}";

        //how much exp 
        float expCurrent = playerStats.currentXP;
        float expNeeded = playerStats.xpToNextLevel;
        EXPText.text = $"EXP: {Mathf.CeilToInt(expCurrent)} / {Mathf.CeilToInt(expNeeded)}";

        //info bars
        UpdateHealthBar();
        UpdateXPBar();
    }

    //bar status of when taking damage and when healed
    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        healthBarFill.fillAmount = MoveFill(healthBarFill.fillAmount, GetHealthPercent());
    }

    //exp bar filling up as you gain exp
    private void UpdateXPBar()
    {
        if (xpBarFill == null) return;

        float target = GetXPPercent();

        //on level up XP drops back down, so snap instead of sliding backwards
        if (target < xpBarFill.fillAmount)
        {
            xpBarFill.fillAmount = target;
            return;
        }

        xpBarFill.fillAmount = MoveFill(xpBarFill.fillAmount, target);
    }

    // returns health as 0 to 1 for the health bar
    private float GetHealthPercent()
    {
        if (playerHealth == null || playerStats == null || playerStats.MaxHealth <= 0f) return 0f;

        return Mathf.Clamp01(playerHealth.CurrentHealth / playerStats.MaxHealth);
    }

    // returns exp progress toward the next level as 0 to 1 for the exp bar
    private float GetXPPercent()
    {
        if (playerStats == null || playerStats.xpToNextLevel <= 0) return 0f;

        return Mathf.Clamp01((float)playerStats.currentXP / playerStats.xpToNextLevel);
    }

    // moves a fill value toward its target, uses unscaled time so bars still finish while the game is paused for level ups
    private float MoveFill(float current, float target)
    {
        if (barFillSpeed <= 0f) return target;

        return Mathf.MoveTowards(current, target, barFillSpeed * Time.unscaledDeltaTime);
    }

    // forces a bar image into Filled / Horizontal / Left mode so it actually shows progress
    private void SetupFillImage(Image fillImage)
    {
        if (fillImage == null) return;

        if (fillImage.sprite == null)
        {
            Debug.LogWarning($"[PlayerStatsUI] {fillImage.name} has no Source Image, Filled mode needs a sprite to work.");
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

}
