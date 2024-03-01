using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[System.Serializable]
public struct GridCell
{
    public int pieceIndex;
    public float rotation;

    public GridCell(int pieceIndex, float rotation)
    {
        this.pieceIndex = pieceIndex;
        this.rotation = rotation;
    }
}

[System.Serializable]
public class GridPosition
{
    public int x, y;

    public GridPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public GridPosition(Vector2 vector2)
    {
        this.x = (int)Mathf.Round(vector2.x);
        this.y = (int)Mathf.Round(vector2.y);
    }
    public Vector2 ToVector2() => new Vector2(x, y);

    public static GridPosition operator +(GridPosition a, GridPosition b) => new GridPosition(a.x + b.x, a.y + b.y);
    public static GridPosition operator -(GridPosition a, GridPosition b) => new GridPosition(a.x - b.x, a.y - b.y);

    public override bool Equals(object other) => other is GridPosition p && (p.x, p.y).Equals((x, y));
    public override int GetHashCode() => (x, y).GetHashCode();
}

public class GridManager : MonoBehaviour
{
    #region Variables
    public Vector2 gridSize
    {
        get { return _gridSize; }
        set
        {
            _gridSize = value;
            UpdateGridRenderSize();
        }
    }
    public Dictionary<GridPosition, GridCell> gridData { get; private set; }
    public GridPosition mouseGridPosition { get; private set; }
    public Piece activePiece { get; private set; }

    [Header("Public Variables")]
    public bool showPieceVisualisation = true;
    [SerializeField] private Vector2 _gridSize;

    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform gridBorder;

    [Header("References")]
    [SerializeField] private GameObject visualisationSprite;
    [SerializeField] private GameObject currentRender;
    [SerializeField] private GameObject redZonePrefab;

    private Collider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Color validPlacementColor = new Color(1, 1, 1, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the active piece
    /// </summary>
    /// <param name="newPiece">The new active piece</param>
    public void SetActivePiece(Piece newPiece)
    {
        activePiece = newPiece;
        visualisationSprite.GetComponent<SpriteRenderer>().sprite = newPiece.PreviewImage;
        visualisationSprite.transform.rotation = Quaternion.identity;

        // destroy previous red zones
        foreach (Transform child in visualisationSprite.transform)
        {
            Destroy(child.gameObject);
        }

        // add red zone visualistions onto visualisation sprite
        List<RestrictedPosition> restrictedPositions = PieceManager.instance.GetRestrictedPositionsFromPiece(activePiece);
        int sizeX = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.x : 0);
        int sizeY = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.y : 0);
        Vector2 sizeOffset = new Vector2(sizeX * 0.25f, sizeY * 0.25f);

        foreach (RestrictedPosition restrictionPos in PieceManager.instance.GetRestrictedPositionsFromPiece(activePiece))
        {
            if (restrictionPos.restrictionType == RestrictionType.RedZone)
            {
                Vector2 zonePosition = restrictionPos.relativePosition.ToVector2();
                Vector2 offsetPosition = new Vector2(zonePosition.x * -0.5f + sizeOffset.x, zonePosition.y * -0.5f + sizeOffset.y);

                GameObject redZone = Instantiate(redZonePrefab);
                redZone.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.4f);
                redZone.transform.parent = visualisationSprite.transform;
                redZone.transform.localPosition = offsetPosition;
                redZone.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Loads and renders the given grid data
    /// </summary>
    /// <param name="data">Data to load</param>
    public void LoadGridFromData(SerializableGrid data)
    {
        gridData = SaveManager.instance.ConvertGridFromSerializable(data);
        RenderGridData();
    }

    /// <summary>
    /// Get the cell data at the given position only if there is a piece there
    /// </summary>
    /// <param name="position">The position in the grid</param>
    /// <param name="cellData">The cell data. Set to default if the cell doesnt exist</param>
    /// <returns>True if cell data was found, False otherwise</returns>
    public bool GetCellDataAtPositionIfExists(GridPosition position, out GridCell cellData)
    {
        if (gridData.ContainsKey(position))
        {
            cellData = gridData[position];
            return true;
        } else
        {
            cellData = default;
            return false;
        }
    }

    /// <summary>
    /// Deletes the cell data at the given position
    /// </summary>
    /// <param name="position">The position in the grid</param>
    /// <param name="deletedCellData">The cell data that was deleted</param>
    /// <returns>True if a cell was deleted, False otherwise</returns>
    public bool ClearCellDataAtPositionIfExists(GridPosition position, out GridCell deletedCellData)
    {
        bool doesCellContainPiece = gridData.ContainsKey(position);
        deletedCellData = default;

        if (doesCellContainPiece)
        {
            deletedCellData = gridData[position];
            gridData.Remove(position);
            RenderGridData();
        }

        return doesCellContainPiece;
    }

    /// <summary>
    /// Checks if the mouse is on the grid
    /// </summary>
    /// <returns>True if the mouse is inside the grid, False otherwise</returns>
    public bool IsMouseOnGrid()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return boxCollider.OverlapPoint(mousePosition);
    }

    /// <summary>
    /// Rotates the current placement
    /// </summary>
    /// <param name="deltaAngle">The angle to rotate by</param>
    public void RotatePlacement(float deltaAngle)
    {
        visualisationSprite.transform.Rotate(Vector3.forward, deltaAngle);
    }

    /// <summary>
    /// Places the current active piece only if the placement is valid
    /// </summary>
    /// <returns>True if the piece was place, False otherwise</returns>
    public bool PlaceActivePieceIfValid()
    {
        bool isValid = IsActivePiecePlacementValid();
        if (isValid)
        {
            gridData.Add(mouseGridPosition, new GridCell(PieceManager.instance.GetIndexFromPiece(activePiece), visualisationSprite.transform.rotation.eulerAngles.z));
            RenderGridData();
        }

        return isValid;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Checks if the current placement is vlaid
    /// </summary>
    /// <returns>True if the placement is valid, False otherwise</returns>
    private bool IsActivePiecePlacementValid()
    {
        // gets the squares that the piece takes up
        List<RestrictedPosition> restrictedPositions = PieceManager.instance.GetRestrictedPositionsFromPiece(activePiece);
        List<GridPosition> squarePositions = restrictedPositions
            .Select(restriction => new GridPosition(mouseGridPosition.ToVector2() + RotateAroundOrigin(restriction.relativePosition.ToVector2(), (360 - visualisationSprite.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad)))
            .ToList();

        bool isValid = ValidateSquares(squarePositions);

        // check for only allow one pieces
        if (isValid && activePiece.OnlyAllowOne)
        {
            foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
            {
                if (activePiece.DisplayName == PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex).DisplayName)
                {
                    isValid = false;
                    break;
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// Updates the visualisation sprite to be at the mouse position.
    /// </summary>
    private void UpdateVisualisationSprite()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (boxCollider.OverlapPoint(mousePosition))
        {
            float cellSize = 0.5f * transform.localScale.x;

            List<RestrictedPosition> restrictedPositions = PieceManager.instance.GetRestrictedPositionsFromPiece(activePiece);
            int sizeX = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.x : 0);
            int sizeY = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.y : 0);

            Vector2 sizeOffset = new Vector2(sizeX * cellSize / 2, sizeY * cellSize / 2);
            sizeOffset = RotateAroundOrigin(sizeOffset, (visualisationSprite.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad);

            visualisationSprite.transform.position = new Vector3(
                Mathf.Floor((mousePosition.x + cellSize / 2) / cellSize) * cellSize - sizeOffset.x, 
                Mathf.Floor((mousePosition.y + cellSize / 2) / cellSize) * cellSize - sizeOffset.y,
                0
            );
            visualisationSprite.GetComponent<SpriteRenderer>().color = IsActivePiecePlacementValid() ? validPlacementColor : invalidPlacementColor;
            visualisationSprite.SetActive(true);
        }
        else
        {
            visualisationSprite.SetActive(false);
        }
    }

    /// <summary>
    /// Validates the given positions on the grid
    /// </summary>
    /// <param name="squares">List of grid positions</param>
    /// <returns>True if the grid positions are valid, False otherwise</returns>
    private bool ValidateSquares(List<GridPosition> squares)
    {
        // Get all of the grid positions that are taken up
        List<GridPosition> invalidPositions = new List<GridPosition>();
        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
        {
            List<RestrictedPosition> restrictedPositions = PieceManager.instance.GetRestrictedPositionsFromPiece(PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex));
            invalidPositions.AddRange(restrictedPositions
                .Select(restriction => kvp.Key + new GridPosition(RotateAroundOrigin(restriction.relativePosition.ToVector2(), (360 - kvp.Value.rotation) * Mathf.Deg2Rad)))
                .ToList());
        }

        // Check every position is inside the grid.
        foreach (GridPosition gridPos in squares)
        {
            if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= gridSize.x || gridPos.y >= gridSize.y)
            {
                return false;
            }
        }

        // Check every position does not already have a piece on it.
        foreach (GridPosition invalidPos in invalidPositions)
        {
            foreach (GridPosition gridPos in squares)
            {
                if (invalidPos.x == gridPos.x && invalidPos.y == gridPos.y)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Renders the current grid data onto the grid
    /// </summary>
    private void RenderGridData()
    {
        foreach (Transform piece in currentRender.transform)
        {
            Destroy(piece.gameObject);
        }

        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
        {
            // calculate the sprite position
            float cellSize = 0.5f * transform.localScale.x;
            GridCell data = kvp.Value;
            Piece pieceData = PieceManager.instance.GetPieceFromIndex(data.pieceIndex);
            Vector2 gridTopLeft = (Vector2)transform.position - new Vector2(spriteRenderer.bounds.size.x / 2, spriteRenderer.bounds.size.y / 2);
            Vector2 spritePosition = gridTopLeft + cellSize * kvp.Key.ToVector2() + new Vector2(cellSize / 2, cellSize / 2);
            spritePosition.y *= -1;

            // instantiate the piece prefab and update properties
            GameObject renderPiece = Instantiate(pieceData.Prefab, currentRender.transform);
            renderPiece.transform.position = spritePosition;
            renderPiece.transform.rotation = Quaternion.Euler(0, 0, data.rotation);

            // place a redzone prefab at the pieces red zones.
            PieceBase prefabData = pieceData.Prefab.GetComponent<PieceBase>();
            foreach (RestrictedPosition restrictionPos in prefabData.restrictedPositions)
            {
                if (restrictionPos.restrictionType == RestrictionType.RedZone)
                {
                    GameObject redZone = Instantiate(redZonePrefab);
                    Vector2 rotatedPosition = RotateAroundOrigin(restrictionPos.relativePosition.ToVector2(), (360 - data.rotation) * Mathf.Deg2Rad);
                    redZone.transform.position = spritePosition + cellSize * new Vector2(rotatedPosition.x, -rotatedPosition.y);
                    redZone.transform.parent = currentRender.transform;
                    redZone.transform.localScale = Vector3.one;
                }
            }
        }
    }

    /// <summary>
    /// Rotates a vector around (0, 0) by the given angle
    /// </summary>
    /// <param name="vector">The vector to rotate</param>
    /// <param name="angle">The angle to rotate the vector by</param>
    /// <returns>The new rotated vector</returns>
    private Vector2 RotateAroundOrigin(Vector2 vector, float angle)
    {
        return new Vector2(vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle), vector.y * Mathf.Cos(angle) + vector.x * Mathf.Sin(angle));
    }

    /// <summary>
    /// Updates the size of the render size of the grid to make sure it fits on the screen
    /// </summary>
    private void UpdateGridRenderSize()
    {
        spriteRenderer.size = _gridSize / 2;

        Camera cam = Camera.main;

        Vector3 min = spriteRenderer.bounds.min;
        Vector3 max = spriteRenderer.bounds.max;
        Vector3 screenMin = cam.WorldToScreenPoint(min);
        Vector3 screenMax = cam.WorldToScreenPoint(max);
        float gridScreenWidth = screenMax.x - screenMin.x;
        float gridScreenHeight = screenMax.y - screenMin.y;
        float newGridSize = (gridBorder.sizeDelta.x * canvas.scaleFactor) - 50;

        transform.localScale = new Vector3(newGridSize / gridScreenWidth, newGridSize / gridScreenHeight, 1);
    }
    #endregion

    #region MonoBehaviour Messages
    private void Awake()
    {
        gridData = new Dictionary<GridPosition, GridCell>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateGridRenderSize();
    }

    private void Update()
    {
        // calculate grid position that mouse is currently at
        float cellSize = 0.5f * transform.localScale.x;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 gridTopLeft = (Vector2)transform.position - new Vector2(spriteRenderer.bounds.size.x / 2, -(spriteRenderer.bounds.size.y / 2));
        Vector2 adjustedMousePosition = mousePosition - gridTopLeft;
        adjustedMousePosition.y *= -1;

        mouseGridPosition = new GridPosition(new Vector2(Mathf.Floor(adjustedMousePosition.x / cellSize), Mathf.Floor(adjustedMousePosition.y / cellSize)));

        // update visulisation sprite
        visualisationSprite.SetActive(showPieceVisualisation);
        if (showPieceVisualisation)
        {
            UpdateVisualisationSprite();
        }
    }
    #endregion
}