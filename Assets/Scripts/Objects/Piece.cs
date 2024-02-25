using UnityEngine;

public enum PieceCategory
{
    Blocks,
    Power,
    Weapons,
    Thrusters,
}

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Piece", order = 1)]
public class Piece : ScriptableObject
{
    public string DisplayName;
    public string Description;
    public Sprite PreviewImage;
    public PieceCategory Category;
    public GameObject Prefab;
    public float Health;
    public int Cost;
    public float Mass;
    public bool OnlyAllowOne;
}