using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Thruster", order = 1)]
public class Thruster : Piece
{
    public float Thrust;
    public float EnergyUsage;
}