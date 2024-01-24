using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct GridData
{
    public Piece pieceData;
    public Vector2 position;
}

public class BuildingSystem : MonoBehaviour
{
    #region Variables

    private Piece activePiece;
    private GridData[] gridData;

    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject visualisationSprite;

    private Collider2D gridCollider;

    // temporary
    [SerializeField] private Piece[] debugPieces;
    private int currentDebugPieceIndex;

    #endregion

    #region Class Functions
    public void SetActivePiece(Piece newPiece)
    {
        activePiece = newPiece;
        visualisationSprite.GetComponent<SpriteRenderer>().sprite = newPiece.Icon;

        // reset certain properties
        visualisationSprite.transform.rotation = Quaternion.identity;
    }
    #endregion

    #region Unity Messages
    private void Start()
    {
        gridCollider = grid.GetComponent<Collider2D>();
        SetActivePiece(debugPieces[currentDebugPieceIndex]);
    }

    private void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (gridCollider.OverlapPoint(mousePosition))
        {
            visualisationSprite.SetActive(true);
            visualisationSprite.transform.position = new Vector3(Mathf.Floor((mousePosition.x + 0.25f) / 0.5f) * 0.5f, Mathf.Floor((mousePosition.y + 0.25f) / 0.5f) * 0.5f, 0);
        } else
        {
            visualisationSprite.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentDebugPieceIndex = ++currentDebugPieceIndex % debugPieces.Length;
            SetActivePiece(debugPieces[currentDebugPieceIndex]);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            visualisationSprite.transform.Rotate(Vector3.forward, -90);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("mouse button pressed");
        }
    }
    #endregion
}
