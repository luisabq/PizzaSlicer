using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Type")]
    public string weaponName = "Pizza Slicer"; 
    public int currentLevel = 1;

    [Header("Base Stats")]
    public float baseWeaponDamage = 10f;
    public float baseWeaponAttackSpeed = 1f; //Attacks per second
    public float baseAttackRange = 2.5f;

    [Header("Upgrades")]



    [Header("Visuals + Sounds")]
    public GameObject attackVFX;






}
