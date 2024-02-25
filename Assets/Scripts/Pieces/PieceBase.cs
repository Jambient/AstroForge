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

public interface IDamagable
{
    public void OnDamage(float damage);
}

public class PieceBase : MonoBehaviour, IDamagable
{
    public List<RestrictedPosition> restrictedPositions = new List<RestrictedPosition>();
    public Piece pieceData;

    public float health { get; private set; }
    private Renderer pieceRenderer;

    protected ShipController shipController;
    protected Rigidbody2D shipRb;

    public void OnDamage(float damage)
    {
        health = Mathf.Max(health - damage, 0);

        if (pieceRenderer != null)
        {
            float destructionLevel = 1 - (health / pieceData.Health);
            pieceRenderer.material.SetFloat("_DestructionLevel", destructionLevel*0.65f);
        }

        if (health == 0)
        {
            shipController.CalculateShipData();
            Destroy(gameObject);
        }
    }

    protected virtual void InGameStart() { }
    protected virtual void InGameUpdate() { }
    protected virtual void InGameFixedUpdate() { }

    protected virtual void Start()
    {
        pieceRenderer = GetComponent<Renderer>();
        pieceRenderer.material.SetFloat("_NoiseSeed", Random.value * 600);

        health = pieceData.Health;

        if (!GlobalsManager.inBuildMode)
        {
            shipController = transform.parent.GetComponent<ShipController>();
            shipRb = transform.parent.GetComponent<Rigidbody2D>();
            InGameStart();
        }
    }

    protected virtual void Update()
    {
        if (!GlobalsManager.inBuildMode)
        {
            InGameUpdate();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!GlobalsManager.inBuildMode)
        {
            InGameFixedUpdate();
        }
    }
}