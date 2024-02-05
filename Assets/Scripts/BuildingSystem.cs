using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
    public static GridPosition operator +(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x + b.x, a.y + b.y);
    }

    public static GridPosition operator -(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x - b.x, a.y - b.y);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        GridPosition other = (GridPosition)obj;
        return x == other.x && y == other.y;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            return hash;
        }
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
    private Dictionary<GridPosition, GridCell> gridData = new Dictionary<GridPosition, GridCell>();
    bool isValid = false;
    private Color validPlacementColor = new Color(1, 1, 1, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);
    private ShipData shipData;

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

    public bool ValidateSquares(List<GridPosition> squares)
    {
        List<GridPosition> invalidPositions = new List<GridPosition>();

        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
        {
            invalidPositions.AddRange(PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex).Prefab.GetComponent<PieceBase>().restrictedPositions.Select(restriction => kvp.Key + new GridPosition(RotateAroundOrigin(restriction.relativePosition.ToVector2(), (360 - kvp.Value.rotation) * Mathf.Deg2Rad))).ToList());
        }

        // Check every square is inside the grid.
        foreach (GridPosition gridPos in squares)
        {
            if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= gridManager.gridSize.x || gridPos.y >= gridManager.gridSize.y)
            {
                return false;
            }
        }

        // Check every square does not already have a piece on it.
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

    private GameObject BuildShip(Dictionary<GridPosition, GridCell> data)
    {
        GameObject ship = new GameObject();
        ship.name = "Ship";

        Vector2 topLeftPosition = new Vector2(Mathf.Infinity, Mathf.Infinity);
        foreach (KeyValuePair<GridPosition, GridCell> kvp in data)
        {
            topLeftPosition.x = Mathf.Min(topLeftPosition.x, kvp.Key.x);
            topLeftPosition.y = Mathf.Min(topLeftPosition.y, kvp.Key.y);
        }

        Debug.Log($"Top left position: {topLeftPosition}");
        foreach (KeyValuePair<GridPosition, GridCell> kvp in data)
        {
            Vector2 newPosition = kvp.Key.ToVector2() - topLeftPosition;
            Vector2 renderPosition = 0.5f * newPosition;
            renderPosition.y *= -1;

            GameObject shipPiece = Instantiate(PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex).Prefab, ship.transform);
            shipPiece.transform.position = renderPosition;
            shipPiece.transform.rotation = Quaternion.Euler(0, 0, kvp.Value.rotation);
        }

        return ship;
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

        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
        {
            GridCell data = kvp.Value;
            Piece pieceData = PieceManager.instance.GetPieceFromIndex(data.pieceIndex);
            Vector2 gridTopLeft = (Vector2)grid.transform.position - new Vector2(grid.GetComponent<Renderer>().bounds.size.x / 2, grid.GetComponent<Renderer>().bounds.size.y / 2);
            Vector2 spritePosition = gridTopLeft + 0.5f * kvp.Key.ToVector2() + new Vector2(0.25f, 0.25f);
            spritePosition.y *= -1;

            GameObject renderPiece = Instantiate(pieceData.Prefab, currentRender.transform);
            renderPiece.transform.position = spritePosition;
            renderPiece.transform.rotation = Quaternion.Euler(0, 0, data.rotation);

            PieceBase prefabData = pieceData.Prefab.GetComponent<PieceBase>();
            foreach (RestrictedPosition restrictionPos in prefabData.restrictedPositions)
            {
                if (restrictionPos.restrictionType == RestrictionType.RedZone)
                {
                    GameObject redZone = Instantiate(RedZonePrefab);
                    Vector2 rotatedPosition = RotateAroundOrigin(restrictionPos.relativePosition.ToVector2(), (360 - data.rotation) * Mathf.Deg2Rad);
                    redZone.transform.position = spritePosition + 0.5f * new Vector2(rotatedPosition.x, -rotatedPosition.y);
                    redZone.transform.parent = currentRender.transform;
                }
            }

            totalCost += pieceData.Cost;
        }

        // update build price
        buildPrice.text = $"BUILD PRICE: <color=#01C8B1>{totalCost} GC";

        //BuildShip(gridData);
    }

    public void TestShip()
    {
        // save ship
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);

        SceneManager.LoadScene("ShipTestingZone");
    }
    #endregion

    #region Unity Messages
    private void Start()
    {
        gridCollider = grid.GetComponent<Collider2D>();
        gridManager = grid.GetComponent<GridManager>();

        SetActivePiece(debugPieces[currentDebugPieceIndex]);

        if (SaveManager.instance.LoadShipData(GlobalsManager.currentShipID, out shipData))
        {
            //BuildShip(SaveManager.instance.ConvertGridFromSerializable(shipData.gridData));
            gridData = SaveManager.instance.ConvertGridFromSerializable(shipData.gridData);
            RenderGridData();
        }
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
            List<GridPosition> squarePositions = activePiece.Prefab.GetComponent<PieceBase>().restrictedPositions.Select(restriction => new GridPosition(gridPosition + RotateAroundOrigin(restriction.relativePosition.ToVector2(), (360 - visualisationSprite.transform.rotation.eulerAngles.z) * Mathf.Deg2Rad))).ToList();
            isValid = ValidateSquares(squarePositions);

            if (isValid && activePiece.OnlyAllowOne)
            {
                foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
                {
                    if (activePiece.Name == PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex).Name)
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
            gridData.Add(new GridPosition(gridPosition), new GridCell(PieceManager.instance.GetIndexFromPiece(activePiece), visualisationSprite.transform.rotation.eulerAngles.z));
            RenderGridData();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (gridData.ContainsKey(new GridPosition(gridPosition)))
            {
                gridData.Remove(new GridPosition(gridPosition));
                RenderGridData();
            }
        }
    }
    #endregion
}
