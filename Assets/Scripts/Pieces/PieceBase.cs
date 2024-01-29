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
    public Vector2 relativePosition;
    public RestrictionType restrictionType;
}

public class PieceBase : MonoBehaviour
{
    public List<RestrictedPosition> restrictedPositions = new List<RestrictedPosition>();
}