using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

struct GridData
{
    public Piece pieceData;
    public Vector2 position;
    public float rotation;

    public GridData(Piece pieceData, Vector2 position, float rotation)
    {
        this.pieceData = pieceData;
        this.position = position;
        this.rotation = rotation;
    }
}

public class BuildingSystem : MonoBehaviour
{
    #region Variables

    private Piece activePiece;
    private List<GridData> gridData = new List<GridData>();

    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject visualisationSprite;
    [SerializeField] private GameObject currentRender;

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

    private void RenderGridData()
    {
        // updated method should check which pieces do not need to be rendered/changed
        // then it should only add objects for pieces which need to be rendered.

        // temporary method (remove everything and re render)
        foreach (Transform piece in currentRender.transform)
        {
            Destroy(piece.gameObject);
        }

        foreach (GridData data in gridData)
        {
            GameObject renderPiece = Instantiate(visualisationSprite);

            SpriteRenderer renderer = renderPiece.GetComponent<SpriteRenderer>();
            renderer.sprite = data.pieceData.Icon;
            renderer.color = Color.white;
            renderPiece.transform.position = data.position;
            renderPiece.transform.rotation = Quaternion.Euler(0, 0, data.rotation);

            renderPiece.transform.parent = currentRender.transform;
        }
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
            gridData.Add(new GridData(activePiece, new Vector2(Mathf.Floor((mousePosition.x + 0.25f) / 0.5f) * 0.5f, Mathf.Floor((mousePosition.y + 0.25f) / 0.5f) * 0.5f), visualisationSprite.transform.rotation.eulerAngles.z));
            RenderGridData();
        }
    }
    #endregion
}
