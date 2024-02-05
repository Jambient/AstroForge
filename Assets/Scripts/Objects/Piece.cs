using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Piece", order = 1)]
public class Piece : ScriptableObject
{
    public string Name;
    public GameObject Prefab;
    public int Health;
    public int Cost;
    public float Mass;
    public bool OnlyAllowOne;
}