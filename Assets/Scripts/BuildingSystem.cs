using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

//struct GridCell
//{
//    public Piece pieceData;
//    public Vector2 position;
//    public float rotation;

//    public GridCell(Piece pieceData, Vector2 position, float rotation)
//    {
//        this.pieceData = pieceData;
//        this.position = position;
//        this.rotation = rotation;
//    }
//}
struct GridCell
{
    public Piece pieceData;
    public float rotation;

    public GridCell(Piece pieceData, float rotation)
    {
        this.pieceData = pieceData;
        this.rotation = rotation;
    }
}

public class BuildingSystem : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject visualisationSprite;
    [SerializeField] private GameObject currentRender;
    [SerializeField] private TextMeshProUGUI buildPrice;

    [SerializeField] private GameObject RedZonePrefab;

    private Collider2D gridCollider;
    private GridManager gridManager;
    private Piece activePiece;
    private Dictionary<Vector2, GridCell> gridData = new Dictionary<Vector2, GridCell>();
    bool isValid = false;
    private Color validPlacementColor = new Color(1, 1, 1, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);

    // temporary
    [SerializeField] private Piece[] debugPieces;
    private int currentDebugPieceIndex;

    #endregion

    #region Maths Functions
    private Vector2 RotateAroundOrigin(Vector2 vector, float angle)
    {
        return new Vector2(vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle), vector.y * Mathf.Cos(angle) + vector.x * Mathf.Sin(angle));
    }
    #endregion

    #region Class Functions
    public void SetActivePiece(Piece newPiece)
    {
        activePiece = newPiece;
        visualisationSprite.GetComponent<SpriteRenderer>().sprite = newPiece.Prefab.GetComponent<SpriteRenderer>().sprite;

        // reset certain properties
        visualisationSprite.transform.rotation = Quaternion.identity;
    }

    public bool ValidateSquares(List<Vector2> squares)
    {
        List<Vector2> invalidPositions = new List<Vector2>();

        foreach (KeyValuePair<Vector2, GridCell> kvp in gridData)
        {
            invalidPositions.AddRange(kvp.Value.pieceData.Prefab.GetComponent<PieceBase>().restrictedPositions.Select(restriction => kvp.Key + RotateAroundOrigin(restriction.relativePosition, (360 - kvp.Value.rotation) * Mathf.Deg2Rad)).ToList());
        }

        //Debug.Log("----------- GRID POS");
        // Check every square is inside the grid.
        foreach (Vector2 gridPos in squares)
        {
            //Debug.Log(gridPos);
            if ((int)gridPos.x < 0 || (int)gridPos.y < 0 || (int)gridPos.x >= gridManager.gridSize.x || (int)gridPos.y >= gridManager.gridSize.y)
            {
                return false;
            }
        }

        //Debug.Log("---------- INVALID POS");
        // Check every square does not already have a piece on it.
        foreach (Vector2 invalidPos in invalidPositions)
        {
            //Debug.Log(invalidPos);
            foreach (Vector2 gridPos in squares)
            {
                if (Mathf.Round(invalidPos.x) == Mathf.Round(gridPos.x) && Mathf.Round(invalidPos.y) == Mathf.Round(gridPos.y))
                {
                    return false;
                }
            }
        }

        return true;
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

        int totalCost = 0;

        foreach (KeyValuePair<Vector2, GridCell> kvp in gridData)
        {
            GridCell data = kvp.Value;
            Vector2 gridTopLeft = (Vector2)grid.transform.position - new Vector2(grid.GetComponent<Renderer>().bounds.size.x / 2, grid.GetComponent<Renderer>().bounds.size.y / 2);
            Vector2 spritePosition = gridTopLeft + 0.5f * kvp.Key + new Vector2(0.25f, 0.25f);
            spritePosition.y *= -1;

            GameObject renderPiece = Instantiate(data.pieceData.Prefab);
            renderPiece.transform.position = spritePosition;
            renderPiece.transform.rotation = Quaternion.Euler(0, 0, data.rotation);
            renderPiece.transform.parent = currentRender.transform;

            PieceBase prefabData = data.pieceData.Prefab.GetComponent<PieceBase>();
            foreach (RestrictedPosition restrictionPos in prefabData.restrictedPositions)
            {
                if (restrictionPos.restrictionType == RestrictionType.RedZone)
                {
                    GameObject redZone = Instantiate(RedZonePrefab);
                    Vector2 rotatedPosition = RotateAroundOrigin(restrictionPos.relativePosition, (360 - data.rotation) * Mathf.Deg2Rad);
                    redZone.transform.position = spritePosition + 0.5f * new Vector2(rotatedPosition.x, -rotatedPosition.y);
                    redZone.transform.parent = currentRender.transform;
                }
            }

            totalCost += data.pieceData.Cost;
        }

        // update build price
        buildPrice.text = $"BUILD PRICE: <color=#01C8B1>{totalCost} GC";
    }
    #endregion

    #region Unity Messages
    private void Start()
    {
        gridCollider = grid.GetComponent<Collider2D>();
        gridManager = grid.GetComponent<GridManager>();

        SetActivePiece(debugPieces[currentDebugPieceIndex]);
    }

    private void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 gridPosition = Vector2.zero;

        if (gridCollider.OverlapPoint(mousePosition))
        {
            visualisationSprite.SetActive(true);
            visualisationSprite.transform.position = new Vector3(Mathf.Floor((mousePosition.x + 0.25f) / 0.5f) * 0.5f, Mathf.Floor((mousePosition.y + 0.25f) / 0.5f) * 0.5f, 0);

            Vector2 gridTopLeft = (Vector2)grid.transform.position - new Vector2(grid.GetComponent<Renderer>().bounds.size.x / 2, -(grid.GetComponent<Renderer>().bounds.size.y / 2));
            Vector2 adjustedMousePosition = mousePosition - gridTopLeft;
            adjustedMousePosition.y *= -1;
            gridPosition = new Vector2(Mathf.Floor(adjustedMousePosition.x / 0.5f), Mathf.Floor(adjustedMousePosition.y / 0.5f));

            // validate the pieces squares
            List<Vector2> squarePositions = activePiece.Prefab.GetComponent<PieceBase>().restrictedPositions.Select(restriction => gridPosition + RotateAroundOrigin(restriction.relativePosition, (360 - visualisationSprite.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad)).ToList();
            isValid = ValidateSquares(squarePositions);

            if (isValid && activePiece.OnlyAllowOne)
            {
                foreach (KeyValuePair<Vector2, GridCell> kvp in gridData)
                {
                    if (activePiece.Name == kvp.Value.pieceData.Name)
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            visualisationSprite.GetComponent<SpriteRenderer>().color = isValid ? validPlacementColor : invalidPlacementColor;
        }
        else
        {
            visualisationSprite.SetActive(false);
            isValid = false;
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

        if (Input.GetMouseButtonDown(0) && isValid)
        {
            gridData.Add(gridPosition, new GridCell(activePiece, visualisationSprite.transform.rotation.eulerAngles.z));
            RenderGridData();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (gridData.ContainsKey(gridPosition))
            {
                gridData.Remove(gridPosition);
                RenderGridData();
            }
        }
    }
    #endregion
}
