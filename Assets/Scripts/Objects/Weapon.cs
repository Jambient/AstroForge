using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : Piece
{
    public GameObject ProjectilePrefab;
    public float FireRate;
    public float EnergyUsage;
    public float ContinuousShotsTillCooldown;
    public float CooldownTime;
}