using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    #region Variables
    public static PieceManager instance { get; private set; }

    [Header("Public Variables")]
    public List<Piece> pieces = new List<Piece>();
    public float maxMass;
    public float maxHealth;
    #endregion

    #region Public Methods
    /// <summary>
    /// Returns piece data from the pieces list
    /// </summary>
    /// <param name="index">The index of the piece</param>
    /// <returns>Piece data</returns>
    public Piece GetPieceFromIndex(int index)
    {
        // check that index is valid
        if (index >= 0 && index < pieces.Count)
        {
            return pieces[index];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the index of a given piece in the pieces list
    /// </summary>
    /// <param name="data">The piece data</param>
    /// <returns>The index of the piece</returns>
    public int GetIndexFromPiece(Piece data)
    {
        return pieces.FindIndex(piece => piece.DisplayName == data.DisplayName);
    }

    /// <summary>
    /// Gets the restricted positions list from the given piece
    /// </summary>
    /// <param name="data">The piece data</param>
    /// <returns>The list of restricted positions</returns>
    public List<RestrictedPosition> GetRestrictedPositionsFromPiece(Piece data)
    {
        return data.Prefab.GetComponent<PieceBase>().restrictedPositions;
    }
    #endregion

    #region MonoBehaviour Messages
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
    #endregion
}
