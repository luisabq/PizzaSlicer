
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Unity.Cinemachine;

public class DoughRollerWeapon : MonoBehaviour
{
    [Header("Weapon Config")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform attackPoint; //Spot where VFX spawns
    [SerializeField] private LayerMask enemyLayer;

    [Header("Dash Settings")]
    [SerializeField] private float baseDashDistance = 5f; //further tiers add onto these
    [SerializeField] private float baseDashDuration = 0.2f; //how long it takes to get to the dash distance + invincibility
    [SerializeField] public float baseCooldown = 6f; //how long till you can dash again, would be used if that cooldown gets adjusted with attack speed
    [SerializeField] private float doubleTapMaxDelay = 0.3f; //threshold before considering 2 taps to be a double tap

    [Header("Tier Modifiers")]
    [SerializeField] private float tier2DashDistance = 7f;
    [SerializeField] private float tier3DashDistance = 9f;
    //[SerializeField] private float tier3KnockbackForce = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip dashSound;
    [Range(0f, 1f)][SerializeField] private float dashVolume = 1.0f;
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)][SerializeField] private float hitVolume = 1.0f;
    [SerializeField] private AudioClip timerSound;
    [Range(0f, 1f)][SerializeField] private float timerVolume = 1.0f;


    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private GameObject dashVFX;
    [SerializeField] private GameObject dashPoofVFX;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator doughAnimator;

    [SerializeField] private GameObject doughRollerObject;
    [SerializeField] private float activeDuration = 0.5f;


    [Header("Dash Hitstop")]
    [SerializeField] private float dashHitStopDuration = 0.05f; 
    [SerializeField] private float pitchStepMultiplier = 0.15f;
    [SerializeField] private CinemachineImpulseSource hitImpulseSource;


    [Header("dont touch this pls")]
    public int maxCharges = 1;
    public int currentCharges;
    public float rechargeTimer;
    private float lastTapTime;
    private bool isDashing = false;

    private PlayerStats playerStats;
    private Transform playerTransform;
    private float nextAttackTime;
    private CharacterController characterController;
    private PlayerHealth playerHealth; //for invuln





    private void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        characterController = GetComponentInParent<CharacterController>();
        

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }


        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("dough roller cant find playerhealth script");
        }

        if (doughRollerObject != null)
        {
            doughRollerObject.SetActive(false);
        }

        if (hitImpulseSource == null)
        {
            hitImpulseSource = GetComponent<CinemachineImpulseSource>();
        }

    }


    void Start()
    {
        UpdateTierStats();
        currentCharges = maxCharges;
    }




    void Update()
    {
        dashVFX.SetActive(isDashing);

        //Dash refill logic
        if (currentCharges < maxCharges)
        {
            //start timer if there are dash charges to refill
            rechargeTimer -= Time.deltaTime;
            if (rechargeTimer <= 0f)
            {
                currentCharges++;
                AudioSource.PlayClipAtPoint(timerSound, transform.position, timerVolume);
                // If we still need more charges, loop the timer

                //may change and make cooldown based on attack speed, if so run this
                //if (currentCharges < maxCharges)
                // {

                //     rechargeTimer = GetAdjustedCooldown();
                // }

                if (currentCharges < maxCharges)
                {
                    rechargeTimer = baseCooldown;
                }

            }
        }

        //will constantly check for double tapping
        DetectDoubleTapInput();
    }

    private void UpdateTierStats()
    {
        int tier = weaponData != null ? weaponData.tier : 1;

        if (tier == 1)
        {
            maxCharges = 1;
            baseDashDistance = 5f;
        }
        else if (tier == 2)
        {
            maxCharges = 2;
            baseDashDistance = tier2DashDistance;
        }
        else if (tier >= 3)
        {
            maxCharges = 3;
            baseDashDistance = tier3DashDistance;
        }

    }


    private void DetectDoubleTapInput()
    {
        //bool tapped = false;
        Vector2 tapScreenPosition = Vector2.zero;

        //KEYBOARD
        //bruh
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame ||
            keyboard.leftShiftKey.wasPressedThisFrame ||
            keyboard.qKey.wasPressedThisFrame ||
            keyboard.eKey.wasPressedThisFrame)
            {
                if (currentCharges > 0)
                {
                    //unlike other dash methods, gotta make this one dash forward based on the player's forward direction
                    Transform playerTf = playerTransform.transform;
                    Vector3 forwardDashDir = playerTf.forward;
                    forwardDashDir.y = 0f;

                    if (forwardDashDir != Vector3.zero)
                    {
                        Debug.Log("(fake) Double tap detected (you faker)");
                        StartCoroutine(ExecuteDashRoutine(forwardDashDir.normalized));
                        return;
                    }
                }
            }
        }

        //MOBILE and hopefully mouse too
        if (Touch.activeTouches.Count > 0)
        {
            Touch activeTouch = Touch.activeTouches[0];

            // Check if the touch began this frame and isn't blocked by UI
            if (activeTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                 tapScreenPosition = activeTouch.screenPosition;
                



                //checking time since last tap to calculate whether a tap is now a double tap
                float timeSinceLastTap = Time.time - lastTapTime;
                //another check for if you are allowed to be dashing rn
                if (timeSinceLastTap <= doubleTapMaxDelay && currentCharges > 0)
                {
                    Debug.Log("Double tap detected");
                    AttemptDash(tapScreenPosition);
                    lastTapTime = 0f; //resets tap timer
                }
                else
                {
                    lastTapTime = Time.time;
                }
            }
        }
    } 
    

    //Function for nonkeyboard methods of dashing, basically calculating dash direction via screen tap position
    private void AttemptDash(Vector3 screenPos)
    {
        //only start if youre not already dashing
        if (isDashing) return;

        //converts tap position to world direction/vector relative to the player
        //i also dont entirely understand how it works nor half these functions but it works so lol
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, playerTransform.transform.position);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 worldTarget = ray.GetPoint(rayDistance);
            Vector3 playerPos = playerTransform.transform.position;
            Vector3 dashDirection = (worldTarget - playerPos).normalized;
            dashDirection.y = 0f;

            if (dashDirection != Vector3.zero)
            {
                StartCoroutine(ExecuteDashRoutine(dashDirection));
            }
        }
    }


    //The movement of the dash
    private IEnumerator ExecuteDashRoutine(Vector3 direction)
    {
        isDashing = true;

        if (currentCharges == maxCharges)
        {
            rechargeTimer = baseCooldown;
        }
        currentCharges--;

        //COME BACK HERE FOR DASH CHARGE JANK
       // if (currentCharges == maxCharges - 1)
        //{

           
          //  rechargeTimer = GetAdjustedCooldown();
        //}


        //rotate player to dash direction
        if (playerTransform != null)
        {
            playerTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        animator.SetTrigger("DashOn");
        doughAnimator.SetTrigger("DashOn");

        if (doughRollerObject != null)
        {
            //StopAllCoroutines();
            StartCoroutine(ToggleRollerRoutine());
        }


        //dash poof
        if (dashPoofVFX != null)
        {
            Vector3 spawnPos = playerTransform.position;
            Quaternion spawnRot = playerTransform.rotation;

            GameObject dashVfxInstance = Instantiate(dashPoofVFX, spawnPos, spawnRot);
            Destroy(dashVfxInstance, 2.0f);
        }



        //Invincibility stuff
        if (playerHealth != null)
        {
            //may need to mess with stuff regarding invuln length of the dash, if we need to do that youd have to go back to playerhealth tho
            playerHealth.StartCoroutine(playerHealth.IFrameRoutine());
        }

        if (dashSound != null && SoundManager.Instance != null)
        {
            AudioSource.PlayClipAtPoint(dashSound, transform.position, dashVolume);
        }

        float elapsedTime = 0f;
        float currentDistance = baseDashDistance;
        Vector3 startPos = playerTransform.transform.position;
        Vector3 targetPos = startPos + (direction * currentDistance);

        //tracking enemies in this specific dash
        HashSet<GameObject> hitEnemiesInThisDash = new HashSet<GameObject>();
        int hitCountInThisDash = 0;


        //Dash movement, moves you from point A to point B over time (dash duration)
        while (elapsedTime < baseDashDuration)
        {
            float t = elapsedTime / baseDashDuration;
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);

            if (characterController != null)
            {
                characterController.Move(newPos - (playerTransform.transform.position));
            }
            else
            {
                //if theres no playertransform can use wherever this script is attatched ig
                transform.root.position = newPos;
            }
            
            bool hitSomeone = PerformDashCollisionCheck(hitEnemiesInThisDash, ref hitCountInThisDash);

            //checking for enemies you hit when dashing to deal damage
            //PerformDashCollisionCheck(hitEnemiesInThisDash);

            if (hitSomeone && hitImpulseSource != null)
            {
                hitImpulseSource.GenerateImpulse();
            }

            if (hitSomeone)
            {
                //hitstop
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(dashHitStopDuration);
                Time.timeScale = 1f;
               
            }



            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //end of dash

        
        isDashing = false;
    }

    //Checks what enemies were collided with the dash, and deals deals damage
    //hashet is for making sure a given enemy is only hit once per dash
    //ref int for the rising pitch sound
    private bool PerformDashCollisionCheck(HashSet<GameObject> hitTracker, ref int hitCount)
    {
        float extraDamage = playerStats != null ? playerStats.Damage : 0f;
        float combinedDamage = weaponData != null ? weaponData.baseWeaponDamage + extraDamage : 10f;
        
        //may need to adjust this for attack range
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, weaponData != null ? weaponData.baseAttackRange : 1.5f, enemyLayer);
        bool triggeredNewHit = false;

        foreach (var hit in hits)
        {

            //making sure each enemy is only hit once per dash instead of a gazillion times per frame
            GameObject enemyRoot = hit.transform.root.gameObject;
            if (hitTracker.Contains(enemyRoot)) continue;


            hitTracker.Add(enemyRoot);
            triggeredNewHit = true;
            hitCount++; 

            //pitch rising calculation
            float currentPitch = 1.0f + ((hitCount - 1) * pitchStepMultiplier);

            EnemyController meleeEnemy = hit.GetComponentInParent<EnemyController>();
            if (meleeEnemy != null)
            {
                hitTracker.Add(enemyRoot);
                triggeredNewHit = true;
                meleeEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(hit.transform);
                Debug.Log("[Dash] hit " + meleeEnemy + " for " + combinedDamage + " damage!");
             //   if (hitSound != null && SoundManager.Instance != null)
              //  {
              //      AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
              //  }
            }

            RangedEnemyController rangedEnemy = hit.GetComponentInParent<RangedEnemyController>();
            if (rangedEnemy != null)
            {
                hitTracker.Add(enemyRoot);
                triggeredNewHit = true;
                rangedEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(hit.transform);
                Debug.Log("[Dash] hit " + rangedEnemy + " for " + combinedDamage + " damage!");
               // if (hitSound != null && SoundManager.Instance != null)
               // {
               //     AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
               // }
            }

            RatEnemyController ratEnemy = hit.GetComponentInParent<RatEnemyController>();
            if (ratEnemy != null)
            {
                hitTracker.Add(enemyRoot);
                triggeredNewHit = true;
                ratEnemy.TakeDamage(combinedDamage);
                SpawnHitVFX(hit.transform);
                Debug.Log("[Dash] hit " + ratEnemy + " for " + combinedDamage + " damage!");
               // if (hitSound != null && SoundManager.Instance != null)
               // {
               //     AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
               // }
            }
            //ADD MORE ENEMIES HERE AAAAAAAAA


            if (hitSound != null)
            {
                PlayPitchedHitSound(hit.transform.position, currentPitch);
            }

        }

        return triggeredNewHit;

    }


    private IEnumerator ToggleRollerRoutine()
    {
        doughRollerObject.SetActive(true);
        yield return new WaitForSeconds(activeDuration);
        doughRollerObject.SetActive(false);
    }

    //spawns audio source here
    private void PlayPitchedHitSound(Vector3 position, float pitch)
    {
        GameObject audioObj = new GameObject("DashHitAudio");
        audioObj.transform.position = position;
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = hitSound;
        audioSource.volume = hitVolume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = 0.5f;
        audioSource.Play();

        Destroy(audioObj, hitSound.length / Mathf.Max(pitch, 0.1f) + 0.1f);
    }


    private void SpawnHitVFX(Transform enemyTransform)
    {
        if (hitVFXPrefab != null && enemyTransform != null)
        {
            Vector3 spawnPos = enemyTransform.position + Vector3.up * 1f;
            GameObject vfxInstance = Instantiate(hitVFXPrefab, spawnPos, Quaternion.identity);
            if (vfxInstance.TryGetComponent(out VisualEffect vfx))
            {
                vfx.Reinit();
                vfx.Play();
            }
            Destroy(vfxInstance, 1.5f);
        }
    }






    //posssssibly a cooldown adjustment based on attackspeed, but im leaving it out for now
    /* 
     private float GetAdjustedCooldown()
    {
        // Adjust cooldown with player attack speed stats if desired
        float speedMultiplier = playerStats != null ? playerStats.AttackSpeed : 1f;
        return baseCooldown / Mathf.Max(speedMultiplier, 0.01f);
    }
    */



    /* more knockback copium
    private void ApplyKnockbackIfNeeded(GameObject enemyObj, Vector3 hitDirection, int currentTier)
    {
        if (currentTier >= 3)
        {
            EnemyController meleeEnemy = enemyObj.GetComponentInParent<EnemyController>();
            if (meleeEnemy != null)
            {
                meleeEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
                return;
            }

            RangedEnemyController rangedEnemy = enemyObj.GetComponentInParent<RangedEnemyController>();
            if (rangedEnemy != null)
            {
                rangedEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
            }

            RatEnemyController ratEnemy = enemyObj.GetComponentInParent<RatEnemyController>();

            if (ratEnemy != null)
            {
                ratEnemy.ApplyKnockback(hitDirection, tier3KnockbackForce);
            }

            //ADD MORE ENEMIES HERE

        }
    }

   */





    public void SetWeaponData(WeaponData newData)
    {
        weaponData = newData;
        UpdateTierStats();
    }

    
    

}

