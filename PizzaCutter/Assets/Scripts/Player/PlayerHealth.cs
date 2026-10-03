using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Invincibility Frames")]
    [SerializeField] private float iFrameDuration = 3.0f;
    [SerializeField] private float flashInterval = 0.5f;

    private PlayerStats playerStats;
    private Renderer[] playerRenderers;
    private float currentHealth;
    private bool isInvincible = false;

    public float CurrentHealth => currentHealth;
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        currentHealth = playerStats != null ? playerStats.maxHealth : 100f;
    }









    public void Heal(float healAmount)
    {
        currentHealth = currentHealth + healAmount;
        if (currentHealth >= playerStats.maxHealth) currentHealth = playerStats.maxHealth;
        Debug.Log($"[PLAYER] Fully restored health to {currentHealth}!");
        return; 
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
        while (timer < iFrameDuration)
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
