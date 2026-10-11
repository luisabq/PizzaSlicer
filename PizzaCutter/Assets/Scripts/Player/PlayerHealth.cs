using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Invincibility Frames")]
    [SerializeField] private float iFrameDuration = 3.0f;
    [SerializeField] private float flashInterval = 0.5f;
    [SerializeField] private Renderer[] playerRenderers;

    private PlayerStats playerStats;
    private float currentHealth;
    private bool isInvincible = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => playerStats != null ? playerStats.MaxHealth : 30f;
    public bool IsInvincible => isInvincible;

    
    // changed this so that capsule stops flashing and instead does player model
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }
   
    private void Start()
    {
       currentHealth = MaxHealth;
    }









    public void Heal(float healAmount)
    {
        float maxHP = MaxHealth;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHP);
        Debug.Log($"[PLAYER] Restored health to {currentHealth}/{maxHP}!");
    }

    //not used rn but maybe could be between levels, if we're bringing stats across them
    public void HealToMax()
    {
        currentHealth = MaxHealth;
        Debug.Log($"[PLAYER] fully restored health to {currentHealth} omnomnom"); 
    }





    public void TakeDamage(float damageAmount)
    {
        //Ignores damage if dead/in invincibility frames
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= damageAmount;
        Debug.Log($"[PLAYER] Took {damageAmount} damage! Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(IFrameRoutine());
        }
    }

    private IEnumerator IFrameRoutine()
    {
        isInvincible = true;

        float timer = 0f;
        float duration = playerStats != null ? playerStats.IFrameDuration : iFrameDuration;
        while (timer < duration)
        {
            SetRenderersEnabled(false);
            yield return new WaitForSeconds(flashInterval);
            SetRenderersEnabled(true);
            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        SetRenderersEnabled(true);
        isInvincible = false;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (var rend in playerRenderers)
        {
            if (rend != null)
            {
                rend.enabled = enabled;
            }
        }
    }


    //use this for death management like pausing the game or switching levels
    private void Die()
    {
        Debug.Log("You dieded :D");
        Time.timeScale = 0f;
    }
}
