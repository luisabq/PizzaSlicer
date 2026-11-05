using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class DashUI : MonoBehaviour
{
    [SerializeField] private DoughRollerWeapon doughRollerWeapon;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject[] chargeIcons;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TMP_Text barText;
    [SerializeField] private string readyText = "Double Tap!";
    [SerializeField] private string rechargingText = "Recharging...";


    private void Awake()
    {
        if (hudContainer == null)
        {
            hudContainer = gameObject;
            Debug.Log("Hud doesnt exist fool");
        }

        if (doughRollerWeapon == null)
        {
            Debug.Log("no dough");
            doughRollerWeapon = FindAnyObjectByType<DoughRollerWeapon>();
        }
    }



    private void Update()
   {

    if (doughRollerWeapon == null)
    {
        if (hudContainer.activeSelf) hudContainer.SetActive(false);
        return;
    }

    bool isWeaponUnlockedAndActive = doughRollerWeapon.gameObject.activeInHierarchy && doughRollerWeapon.enabled;

    if (hudContainer.activeSelf != isWeaponUnlockedAndActive)
    {
        hudContainer.SetActive(isWeaponUnlockedAndActive);
    }

    if (!isWeaponUnlockedAndActive) return;

    UpdateChargeIcons(doughRollerWeapon.currentCharges, doughRollerWeapon.maxCharges);
    UpdateCooldownBar(doughRollerWeapon.currentCharges, doughRollerWeapon.maxCharges, doughRollerWeapon.rechargeTimer, doughRollerWeapon.baseCooldown);
   }




    private void UpdateChargeIcons(int currentCharges, int maxCharges)
    {
        for (int i = 0; i < chargeIcons.Length; i++)
        {
            if (chargeIcons[i] != null)
            {
                bool shouldBeActive = i < maxCharges && i < currentCharges;
                chargeIcons[i].SetActive(shouldBeActive);
            }
        }
    }

    private void UpdateCooldownBar(int currentCharges, int maxCharges, float rechargeTimer, float baseCooldown)
    {
        if (cooldownSlider == null) return;

        if (currentCharges >= maxCharges)
        {
            cooldownSlider.value = 1f;
            if (barText != null) barText.text = readyText;
        }
        else
        {
            float progress = Mathf.Clamp01(1f - (rechargeTimer / baseCooldown));
            cooldownSlider.value = progress;
            if (barText != null) barText.text = rechargingText;
        }
    }
}




