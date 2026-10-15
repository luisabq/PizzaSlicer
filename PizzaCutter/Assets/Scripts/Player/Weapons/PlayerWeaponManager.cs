using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Weapon Objects on Player")]
    [SerializeField] private MeleeWeapon meleeWeaponScript;
    [SerializeField] private MeatballGunWeapon meatballGunScript;
    //ADD MORE WEAPONS HERE


    [Header("Current Weapon Data")]
    public WeaponData currentMeleeData;
    public WeaponData currentMeatballData;
    //ADD MORE WEAPONS HERE

    private void Start()
    {
        //starts with melee only
        if (meleeWeaponScript != null)
        {
            meleeWeaponScript.gameObject.SetActive(true);
        }

        if (meatballGunScript != null)
        {
            meatballGunScript.gameObject.SetActive(false);
        }


        //ADD MORE WEAPONS HERE
    }

    //quick checker function to see what weapons the player has
    public bool IsWeaponUnlocked(WeaponType type)
    {
        if (type == WeaponType.Melee) return currentMeleeData != null;
        if (type == WeaponType.MeatballGun) return currentMeatballData != null;
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


        //ADD MORE WEAPONS HERE





    }

}
