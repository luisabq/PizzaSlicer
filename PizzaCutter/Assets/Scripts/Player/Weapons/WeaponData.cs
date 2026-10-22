using UnityEngine;
public enum WeaponType
{
    Melee,
    MeatballGun,
    Garlic
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Type")]
    public string weaponName;
    public WeaponType weaponType;
    public int tier = 1; //1 is base btw

    [Header("Base Stats")]
    public float baseWeaponDamage = 10f;
    public float baseWeaponAttackSpeed = 1f; //Attacks per second
    public float baseAttackRange = 2.5f;


    [Header("Next Tier")] //needs a reference for tier 2, then tier 2 needs a reference for 3
    public WeaponData nextTierUpgrade;


    [Header("Visuals + Sounds")]
    public GameObject attackVFX;

    
}
