using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    protected ShipController shipController;
    protected Rigidbody2D shipRb;

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
            shipController.CalculateShipData();
            Destroy(gameObject);
        }
    }

    protected virtual void Start()
    {
        pieceRenderer = GetComponent<Renderer>();
        pieceRenderer.material.SetFloat("_NoiseSeed", Random.value * 600);

        health = pieceData.Health;

        if (!GlobalsManager.inBuildMode)
        {
            shipController = transform.parent.GetComponent<ShipController>();
            shipRb = transform.parent.GetComponent<Rigidbody2D>();
        }
    }
}