using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    public static PieceManager instance { get; private set; }

    public List<Piece> pieces = new List<Piece>();
    public float maxMass;
    public float maxHealth;

    public Piece GetPieceFromIndex(int index)
    {
        if (index >= 0 && index < pieces.Count)
        {
            return pieces[index];
        }
        else
        {
            Debug.LogWarning($"Piece index {index} not valid.");
            return null;
        }
    }

    public int GetIndexFromPiece(Piece data)
    {
        return pieces.FindIndex(piece => piece.DisplayName == data.DisplayName);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }

        foreach (Piece piece in pieces)
        {
            maxMass = Mathf.Max(maxMass, piece.Mass);
            maxHealth = Mathf.Max(maxHealth, piece.Health);
        }
    }
}
