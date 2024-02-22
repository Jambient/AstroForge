using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region Structures

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
#endregion

public enum BuildingOption
{
    Select,
    Build,
    Delete
}

public class BuildingSystem : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject visualisationSprite;
    [SerializeField] private GameObject currentRender;
    [SerializeField] private TextMeshProUGUI buildPrice;
    [SerializeField] private GameObject shipDataContainer;
    [SerializeField] private GameObject canvas;

    [SerializeField] private GameObject RedZonePrefab;

    private Collider2D gridCollider;
    private GridManager gridManager;
    private Piece activePiece;
    private Dictionary<GridPosition, GridCell> gridData = new Dictionary<GridPosition, GridCell>();
    bool isValid = false;
    private Color validPlacementColor = new Color(1, 1, 1, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);
    private ShipData shipData;
    private BuildingOption currentBuildOption = BuildingOption.Build;

    private GridPosition[] directions = { new GridPosition(0, -1), new GridPosition(0, 1), new GridPosition(-1, 0), new GridPosition(1, 0) };

    // UI references
    private GameObject noShipsMessage;
    private Transform newShipModal;
    private TMP_InputField shipNameInput;
    private Transform buildingOptionButtons;
    private TextMeshProUGUI buildingOptionSelectedText;
    private RectTransform hoverInfo;

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
        Debug.Log(newPiece);
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
            float cellSize = 0.5f * grid.transform.localScale.x;
            GridCell data = kvp.Value;
            Piece pieceData = PieceManager.instance.GetPieceFromIndex(data.pieceIndex);
            Vector2 gridTopLeft = (Vector2)grid.transform.position - new Vector2(grid.GetComponent<Renderer>().bounds.size.x / 2, grid.GetComponent<Renderer>().bounds.size.y / 2);
            Vector2 spritePosition = gridTopLeft + cellSize * kvp.Key.ToVector2() + new Vector2(cellSize/2, cellSize/2);
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
                    redZone.transform.position = spritePosition + cellSize * new Vector2(rotatedPosition.x, -rotatedPosition.y);
                    redZone.transform.parent = currentRender.transform;
                    redZone.transform.localScale = Vector3.one;
                }
            }

            totalCost += pieceData.Cost;
        }

        // update build price
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            buildPrice.text = $"BUILD PRICE: <color=#01C8B1>{GlobalsManager.gameData.credits} GC";
        }
    }

    public void LoadShip(int shipID)
    {
        if (SaveManager.instance.LoadShipData(shipID, out shipData))
        {
            GlobalsManager.currentShipID = shipID;

            gridData = SaveManager.instance.ConvertGridFromSerializable(shipData.gridData);
            RenderGridData();

            canvas.transform.Find("ShipLoading").gameObject.SetActive(false);
            canvas.transform.Find("ShipData").GetComponentInChildren<TMP_InputField>().text = shipData.name;
        }
    }

    public void OpenNewShipModal()
    {
        newShipModal.gameObject.SetActive(true);
        shipNameInput.text = "";
        shipNameInput.Select();
    }

    public void ReturnToShipLoadMenu()
    {
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);
        DisplaySavedShips();
        canvas.transform.Find("ShipLoading").gameObject.SetActive(true);
    }

    public void SwitchBuildingOption(int optionIndex)
    {
        currentBuildOption = (BuildingOption)optionIndex;
        string buildingOptionName = currentBuildOption.ToString();

        foreach (RectTransform button in buildingOptionButtons)
        {
            Image buttonBackground = button.GetComponent<Image>();
            Image buttonIcon = button.Find("Icon").GetComponent<Image>();

            if (button.gameObject.name == buildingOptionName)
            {
                button.DOSizeDelta(new Vector2(80, 80), 0.3f);
                buttonBackground.DOColor(new Color(1f / 255f, 200f / 255f, 177f / 255f), 0.3f);
                buttonIcon.DOColor(new Color(1, 1, 1, 1), 0.3f);
            } else
            {
                button.DOSizeDelta(new Vector2(70, 70), 0.3f);
                buttonBackground.DOColor(new Color(108f / 255f, 108f / 255f, 108f / 255f), 0.3f);
                buttonIcon.DOColor(new Color(1, 1, 1, 0.3f), 0.3f);
            }
        }

        buildingOptionSelectedText.text = $"{buildingOptionName} Mode";
    }

    private int FloodFillCountCells(GridPosition currentPosition, List<GridPosition> visited = null)
    {
        visited ??= new List<GridPosition>();
        visited.Add(currentPosition);

        int cellCount = 1;
        foreach (GridPosition direction in directions)
        {
            GridPosition newGridPos = currentPosition + direction;
            if (newGridPos.x >= 0 && newGridPos.y >= 0 && newGridPos.x < gridManager.gridSize.x && newGridPos.y < gridManager.gridSize.y)
            {
                if (gridData.ContainsKey(newGridPos) && !visited.Contains(newGridPos))
                {
                    cellCount += FloodFillCountCells(newGridPos, visited);
                }
            }
        }
        return cellCount;
    }

    public void LaunchShip()
    {
        if (gridData.Count != FloodFillCountCells(gridData.First().Key))
        {
            Debug.Log("not all pieces are connected");
            return;
        }

        // save ship
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);

        GlobalsManager.inBuildMode = false;
        SceneManager.LoadScene("ShipTestingZone");
    }

    private void DisplaySavedShips()
    {
        foreach (Transform child in shipDataContainer.transform.parent)
        {
            if (child != shipDataContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        int[] shipIDs = SaveManager.instance.GetAllShipIDs();

        if (shipIDs.Length > 0) {
            noShipsMessage.SetActive(false);

            foreach (int shipID in shipIDs)
            {
                ShipData tempShipData;
                if (SaveManager.instance.LoadShipData(shipID, out tempShipData))
                {
                    GameObject containerClone = Instantiate(shipDataContainer, shipDataContainer.transform.parent);
                    containerClone.transform.Find("ShipNameText").GetComponent<TextMeshProUGUI>().text = tempShipData.name;
                    containerClone.transform.Find("LastEditedText").GetComponent<TextMeshProUGUI>().text = $"Last Edited: {tempShipData.lastEdited.ToString("t")} {tempShipData.lastEdited.ToString("d")}";
                    containerClone.transform.GetComponent<Button>().onClick.AddListener(() => {
                        LoadShip(shipID);
                    });
                    containerClone.transform.Find("DeleteButton").GetComponent<Button>().onClick.AddListener(() => {
                        SaveManager.instance.DeleteShipData(shipID);
                        DisplaySavedShips();
                    });
                    containerClone.SetActive(true);
                }
            }
        } else
        {
            noShipsMessage.SetActive(true);
        }
    }
    #endregion

    #region Unity Messages
    private void Start()
    {
        GlobalsManager.inBuildMode = true;

        gridCollider = grid.GetComponent<Collider2D>();
        gridManager = grid.GetComponent<GridManager>();

        SetActivePiece(debugPieces[currentDebugPieceIndex]);

        // get ui references
        newShipModal = canvas.transform.Find("NewShipModal");

        Transform shipLoadingScreen = canvas.transform.Find("ShipLoading");
        noShipsMessage = shipLoadingScreen.Find("NoShipsMessage").gameObject;

        Transform modalContent = newShipModal.Find("ModalContent");
        shipNameInput = modalContent.GetComponentInChildren<TMP_InputField>();

        Transform modesContainer = canvas.transform.Find("Modes");
        buildingOptionButtons = modesContainer.Find("Options");
        buildingOptionSelectedText = modesContainer.Find("SelectedOption").GetComponentInChildren<TextMeshProUGUI>();

        hoverInfo = (RectTransform)canvas.transform.Find("HoverInfo");

        buildPrice.gameObject.SetActive(GlobalsManager.currentGameMode == GameMode.Restricted);

        DisplaySavedShips();

        // cancelled input
        shipNameInput.onEndEdit.AddListener(delegate {
            newShipModal.gameObject.SetActive(false);
        });

        // submitted input
        shipNameInput.onSubmit.AddListener(delegate {
            newShipModal.gameObject.SetActive(false);

            shipData.name = shipNameInput.text;
            shipData.gridData = SaveManager.instance.ConvertGridToSerializable(new Dictionary<GridPosition, GridCell>());
            int[] shipIDs = SaveManager.instance.GetAllShipIDs();
            int newShipID = shipIDs.Length > 0 ? shipIDs.Max() + 1 : 0;

            SaveManager.instance.SaveShipData(newShipID, shipData);
            DisplaySavedShips();
        });
    }

    private void Update()
    {
        if (PauseManager.instance.isGamePaused) {  return; }

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 gridPosition = Vector2.zero;

        if (gridCollider.OverlapPoint(mousePosition))
        {
            float cellSize = 0.5f * grid.transform.localScale.x;
            visualisationSprite.transform.position = new Vector3(Mathf.Floor((mousePosition.x + cellSize/2) / cellSize) * cellSize, Mathf.Floor((mousePosition.y + cellSize/2) / cellSize) * cellSize, 0);

            Vector2 gridTopLeft = (Vector2)grid.transform.position - new Vector2(grid.GetComponent<Renderer>().bounds.size.x / 2, -(grid.GetComponent<Renderer>().bounds.size.y / 2));
            Vector2 adjustedMousePosition = mousePosition - gridTopLeft;
            adjustedMousePosition.y *= -1;
            gridPosition = new Vector2(Mathf.Floor(adjustedMousePosition.x / cellSize), Mathf.Floor(adjustedMousePosition.y / cellSize));

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
            visualisationSprite.SetActive(true);
        }
        else
        {
            visualisationSprite.SetActive(false);
            isValid = false;
        }

        switch (currentBuildOption)
        {
            case BuildingOption.Select:
                visualisationSprite.SetActive(false);
                if (gridData.ContainsKey(new GridPosition(gridPosition)))
                {
                    hoverInfo.gameObject.SetActive(true);
                    Piece hoveredPieceData = PieceManager.instance.GetPieceFromIndex(gridData[new GridPosition(gridPosition)].pieceIndex);

                    hoverInfo.anchoredPosition = Input.mousePosition;
                    hoverInfo.GetComponentInChildren<TextMeshProUGUI>().text = hoveredPieceData.Name;
                } else
                {
                    hoverInfo.gameObject.SetActive(false);
                }

                break;
            case BuildingOption.Build:
                hoverInfo.gameObject.SetActive(false);

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
                    GlobalsManager.gameData.credits -= activePiece.Cost;
                    gridData.Add(new GridPosition(gridPosition), new GridCell(PieceManager.instance.GetIndexFromPiece(activePiece), visualisationSprite.transform.rotation.eulerAngles.z));
                    RenderGridData();
                }
                if (Input.GetMouseButtonDown(1))
                {
                    if (gridData.ContainsKey(new GridPosition(gridPosition)))
                    {
                        GlobalsManager.gameData.credits += activePiece.Cost;
                        gridData.Remove(new GridPosition(gridPosition));
                        RenderGridData();
                    }
                }

                break;
            case BuildingOption.Delete:
                visualisationSprite.SetActive(false);
                hoverInfo.gameObject.SetActive(false);

                if (Input.GetMouseButtonDown(0))
                {
                    if (gridData.ContainsKey(new GridPosition(gridPosition)))
                    {
                        GlobalsManager.gameData.credits += activePiece.Cost;
                        gridData.Remove(new GridPosition(gridPosition));
                        RenderGridData();
                    }
                }

                break;
        }
    }
    #endregion
}
