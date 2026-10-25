using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Invincibility Frames")]
    [SerializeField] private float iFrameDuration = 3.0f;
    [SerializeField] private float flashInterval = 0.5f;
    [SerializeField] private Renderer[] playerRenderers;


    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [Range(0f, 1f)][SerializeField] private float hurtVolume = 1.0f;
    [SerializeField] private AudioClip loseSound;
    [Range(0f, 1f)][SerializeField] private float loseVolume = 1.0f;




    private PlayerStats playerStats;
    private float currentHealth;
    private bool isInvincible = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => playerStats != null ? playerStats.MaxHealth : 30f;
    public bool IsInvincible => isInvincible;

    private int playerLayer;
    private int enemyLayer;



    // changed this so that capsule stops flashing and instead does player model
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        currentHealth = MaxHealth;
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("EnemyLayer");
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
        if (hurtSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(hurtSound, hurtVolume);
        }
        Handheld.Vibrate();
        Debug.Log($"[PLAYER] Took {damageAmount} damage! Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
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
        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true); //can walk through enemies when hit

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
        Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false); //adds collision back
        isInvincible = false;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (playerRenderers == null) return;

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
        if (loseSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(loseSound, loseVolume);
        }



        LevelManager levelManager = FindAnyObjectByType<LevelManager>();

        if (levelManager != null)
        {
            levelManager.ShowGameOverUI();
        }
        else
        {
            Debug.LogWarning("LevelManager not found. Pausing game.");
            Time.timeScale = 0f;
        }
    }

    public void ResetHealthForRestart()
    {
        StopAllCoroutines();

        // Refresh component and layer references in case this player persisted across scenes.
        playerStats = GetComponent<PlayerStats>();
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("EnemyLayer");

        currentHealth = MaxHealth;
        isInvincible = false;

        SetRenderersEnabled(true);

        if (playerLayer >= 0 && enemyLayer >= 0)
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        Debug.Log($"[PLAYER] Health reset to {currentHealth}/{MaxHealth} for a new run.");
    }
}