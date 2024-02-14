using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RestrictionType
{
    Piece,
    RedZone
}

[System.Serializable]
public struct RestrictedPosition
{
    public GridPosition relativePosition;
    public RestrictionType restrictionType;
}

public class PieceBase : MonoBehaviour
{
    public List<RestrictedPosition> restrictedPositions = new List<RestrictedPosition>();
    public Piece pieceData;

    private float health;
    private Renderer pieceRenderer;

    public void DamagePiece(float damageAmount)
    {
        health = Mathf.Max(health - damageAmount, 0);

        if (pieceRenderer != null)
        {
            float destructionLevel = 1 - (health / pieceData.Health);
            pieceRenderer.material.SetFloat("_DestructionLevel", destructionLevel);
        }

        if (health == 0)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        pieceRenderer = GetComponent<Renderer>();
        pieceRenderer.material.SetFloat("_NoiseSeed", Random.value * 600);

        health = pieceData.Health;
    }
}