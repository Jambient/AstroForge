using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    public static PieceManager instance { get; private set; }

    public List<Piece> pieces = new List<Piece>();

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
        return pieces.FindIndex(piece => piece.Name == data.Name);
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
    }
}
