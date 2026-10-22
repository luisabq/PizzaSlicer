
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapon Objects on Player")]
    [SerializeField] private MeleeWeapon meleeWeaponScript;
    [SerializeField] private MeatballGunWeapon meatballGunScript;
    [SerializeField] private GarlicWeapon garlicScript;
    //ADD MORE WEAPONS HERE




    [Header("Current Weapon Data")]
    [SerializeField] private WeaponData startingMeleeData;
    public WeaponData currentMeleeData;
    public WeaponData currentMeatballData;
    public WeaponData currentGarlicData;
    //ADD MORE WEAPONS HERE

    private void Start()
    {
        //starts with melee only
        if (meleeWeaponScript != null && startingMeleeData != null)
        {
            currentMeleeData = startingMeleeData;
            meleeWeaponScript.gameObject.SetActive(true);
            meleeWeaponScript.SetWeaponData(startingMeleeData);
        }

        if (meatballGunScript != null)
        {
            meatballGunScript.gameObject.SetActive(false);
        }

        if (garlicScript != null)
        {
            garlicScript.gameObject.SetActive(false);
        }


        //ADD MORE WEAPONS HERE
    }

    //quick checker function to see what weapons the player has
    public bool IsWeaponUnlocked(WeaponType type)
    {
        if (type == WeaponType.Melee) return currentMeleeData != null;
        if (type == WeaponType.MeatballGun) return currentMeatballData != null;
        if (type == WeaponType.Garlic) return currentGarlicData != null;

        //ADD MORE WEAPONS HERE
        return false;
    }


    public void ApplyWeaponUpgrade(WeaponData newWeaponData)
    {
        //checks what weapon type was given, then changes current melee weapon to the weapon given, etc..
        if (newWeaponData.weaponType == WeaponType.Melee)
        {
            currentMeleeData = newWeaponData;
            meleeWeaponScript.gameObject.SetActive(true);
            meleeWeaponScript.SetWeaponData(newWeaponData);
        }
        else if (newWeaponData.weaponType == WeaponType.MeatballGun)
        {
            currentMeatballData = newWeaponData;
            meatballGunScript.gameObject.SetActive(true); // Unlocks gun
            meatballGunScript.SetWeaponData(newWeaponData);
        }
        else if (newWeaponData.weaponType == WeaponType.Garlic)
        {
            currentGarlicData = newWeaponData;
            garlicScript.gameObject.SetActive(true); // Unlocks garlic
            garlicScript.SetWeaponData(newWeaponData);
        }


        //ADD MORE WEAPONS HERE





    }

    public void ResetToStartingLoadout()
    {
        //resets weapons to the starting loadout for a new run
        currentMeleeData = startingMeleeData;
        currentMeatballData = null;

        if (meleeWeaponScript != null)
        {
            meleeWeaponScript.gameObject.SetActive(startingMeleeData != null);

            if (startingMeleeData != null)
                meleeWeaponScript.SetWeaponData(startingMeleeData);
        }

        if (meatballGunScript != null)
        {
            meatballGunScript.gameObject.SetActive(false);
        }

        if (garlicScript != null)
        {
            garlicScript.gameObject.SetActive(false);
        }

        //add more weapons here
    }
}
