using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Piece", order = 1)]
public class Piece : ScriptableObject
{
    public string Name;
    public Sprite Icon;
    public Vector2 GridSize;
    public int Health;
    public int Cost;
    public float Weight;
}