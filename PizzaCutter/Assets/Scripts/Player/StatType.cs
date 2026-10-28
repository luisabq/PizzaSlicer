using UnityEngine;


//This is supposed to make finding variables easier across scripts or something
public enum StatType
{
    MoveSpeed,
    AttackSpeed,
    Damage,
    MaxHealth,
    IFrames,
    PickupRadius
}

[System.Serializable]
public struct StatUpgradeOption
{
    //this is supposed to help with deciding the amount of points gained in the stat option when levelling
    public StatType statType;
    public int pointsGained; 
}
