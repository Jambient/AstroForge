using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : Piece
{
    public float Damage;
    public float FireRate;
    public float Range;
    public float EnergyUsage;
}