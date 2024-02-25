using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
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

public enum BuildingOption
{
    Select,
    Build,
    Delete
}
#endregion

public class BuildingSystem : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject visualisationSprite;
    [SerializeField] private GameObject currentRender;
    [SerializeField] private TextMeshProUGUI buildPrice;
    [SerializeField] private TextMeshProUGUI launchErrorMessage;
    [SerializeField] private GameObject shipDataContainer;
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject redZonePrefab;
    [SerializeField] private AudioSource placeSoundEffect;
    [SerializeField] private AudioSource deleteSoundEffect;

    private Collider2D gridCollider;
    private GridManager gridManager;
    private Piece activePiece;
    private Dictionary<GridPosition, GridCell> gridData = new Dictionary<GridPosition, GridCell>();
    bool isValid = false;
    private Color validPlacementColor = new Color(1, 1, 1, 0.5f);
    private Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);
    private ShipData shipData;
    private BuildingOption currentBuildOption = BuildingOption.Build;
    private Piece currentHoveredPiece;

    private GridPosition[] directions = { new GridPosition(0, -1), new GridPosition(0, 1), new GridPosition(-1, 0), new GridPosition(1, 0) };

    // UI references
    private GameObject noShipsMessage;
    private Transform newShipModal;
    private TMP_InputField shipNameInput;
    private Transform buildingOptionButtons;
    private TextMeshProUGUI buildingOptionSelectedText;
    private RectTransform hoverInfo;

    private Transform piecesTabs;
    private TextMeshProUGUI piecesTabName;
    private TextMeshProUGUI piecesTabKnown;
    private Transform piecesScrollViewport;
    private GameObject pieceContainer;
    #endregion

    #region Maths Functions
    private Vector2 RotateAroundOrigin(Vector2 vector, float angle)
    {
        return new Vector2(vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle), vector.y * Mathf.Cos(angle) + vector.x * Mathf.Sin(angle));
    }
    #endregion

    #region Public UI Functions
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

    public void SwitchToTab(int tabId)
    {
        PieceCategory category = (PieceCategory)tabId;
        string categoryName = category.ToString();

        foreach (Transform contentContainer in piecesScrollViewport)
        {
            contentContainer.gameObject.SetActive(contentContainer.name == categoryName);
        }
        foreach (Transform tab in piecesTabs)
        {
            if (tab.name == categoryName)
            {
                tab.GetComponent<Image>().color = new Color(1f / 255f, 200f / 255f, 177f / 255f);
                tab.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, 1);
            }
            else
            {
                tab.GetComponent<Image>().color = new Color(42f / 255f, 42f / 255f, 42f / 255f);
                tab.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
            }
        }
        piecesTabName.text = categoryName;
        piecesTabKnown.text = $"{piecesScrollViewport.Find(categoryName).childCount} known";
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
            }
            else
            {
                button.DOSizeDelta(new Vector2(70, 70), 0.3f);
                buttonBackground.DOColor(new Color(108f / 255f, 108f / 255f, 108f / 255f), 0.3f);
                buttonIcon.DOColor(new Color(1, 1, 1, 0.3f), 0.3f);
            }
        }

        buildingOptionSelectedText.text = $"{buildingOptionName} Mode";
    }

    public void LaunchShip()
    {
        bool hasCore = false;
        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridData)
        {
            Piece pieceData = PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex);
            if (pieceData.DisplayName == "Ship Core")
            {
                hasCore = true;
            }

        }
        if (!hasCore)
        {
            launchErrorMessage.gameObject.SetActive(true);
            launchErrorMessage.text = "The ship requires a core before launching.";
            return;
        }

        if (gridData.Count != FloodFillCountCells(gridData.First().Key))
        {
            launchErrorMessage.gameObject.SetActive(true);
            launchErrorMessage.text = "All pieces must be connected before launching.";
            return;
        }

        // save ship
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);

        GlobalsManager.inBuildMode = false;
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            SceneManager.LoadScene("InGame");
        }
        else
        {
            SceneManager.LoadScene("ShipTestingZone");
        }
    }
    #endregion

    #region Private Functions
    private void SetActivePiece(Piece newPiece)
    {
        activePiece = newPiece;
        Debug.Log(newPiece);
        visualisationSprite.GetComponent<SpriteRenderer>().sprite = newPiece.PreviewImage;

        // reset certain properties
        visualisationSprite.transform.rotation = Quaternion.identity;
    }

    private bool ValidateSquares(List<GridPosition> squares)
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
                    GameObject redZone = Instantiate(redZonePrefab);
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

    private void GeneratePiecesUI()
    {
        foreach (Piece piece in PieceManager.instance.pieces)
        {
            GameObject newPieceContainer = Instantiate(pieceContainer, piecesScrollViewport.GetChild((int)piece.Category));
            newPieceContainer.transform.Find("PreviewImage").GetComponent<Image>().sprite = piece.PreviewImage;
            newPieceContainer.transform.Find("PieceName").GetComponent<TextMeshProUGUI>().text = piece.DisplayName;
            newPieceContainer.transform.Find("CostBar").Find("Cost").GetComponent<TextMeshProUGUI>().text = $"{piece.Cost} GC";
            newPieceContainer.SetActive(true);

            newPieceContainer.GetComponent<Button>().onClick.AddListener(() => { SetActivePiece(piece); });
            EventTrigger trigger = newPieceContainer.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) => { Debug.Log("Pointer entered"); currentHoveredPiece = piece; });

            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener((data) => { Debug.Log("Pointer exited"); currentHoveredPiece = null; });

            trigger.triggers.Add(pointerEnterEvent);
            trigger.triggers.Add(pointerExitEvent);
        }
        Destroy(pieceContainer);
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

        if (shipIDs.Length > 0)
        {
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
        }
        else
        {
            noShipsMessage.SetActive(true);
        }
    }

    private void LoadShip(int shipID)
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
    #endregion

    #region Unity Messages
    private void Start()
    {
        GlobalsManager.inBuildMode = true;

        gridCollider = grid.GetComponent<Collider2D>();
        gridManager = grid.GetComponent<GridManager>();

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

        Transform pieceSelection = canvas.transform.Find("Pieces");
        Transform piecesTabDescription = pieceSelection.Find("TabDescription");

        piecesTabs = pieceSelection.Find("Tabs").Find("Options");
        piecesScrollViewport = pieceSelection.Find("ScrollRect").Find("Viewport");
        piecesTabName = piecesTabDescription.Find("TabName").GetComponent<TextMeshProUGUI>();
        piecesTabKnown = piecesTabDescription.Find("KnownCount").GetComponent<TextMeshProUGUI>();
        pieceContainer = piecesScrollViewport.GetChild(0).GetChild(0).gameObject;

        buildPrice.gameObject.SetActive(GlobalsManager.currentGameMode == GameMode.Restricted);

        DisplaySavedShips();
        GeneratePiecesUI();
        SwitchToTab(0);
        SetActivePiece(PieceManager.instance.pieces[0]);

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
                    if (activePiece.DisplayName == PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex).DisplayName)
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
                    currentHoveredPiece = PieceManager.instance.GetPieceFromIndex(gridData[new GridPosition(gridPosition)].pieceIndex);
                } else
                {
                    if (gridPosition.magnitude > 0)
                    {
                        currentHoveredPiece = null;
                        Debug.Log("hiding piece");
                    }
                }

                break;
            case BuildingOption.Build:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    visualisationSprite.transform.Rotate(Vector3.forward, -90);
                }

                if (Input.GetMouseButtonDown(0) && isValid && (GlobalsManager.currentGameMode == GameMode.Sandbox || GlobalsManager.gameData.credits >= activePiece.Cost))
                {
                    GlobalsManager.gameData.credits -= activePiece.Cost;
                    gridData.Add(new GridPosition(gridPosition), new GridCell(PieceManager.instance.GetIndexFromPiece(activePiece), visualisationSprite.transform.rotation.eulerAngles.z));
                    RenderGridData();
                    placeSoundEffect.Play();
                }
                if (Input.GetMouseButtonDown(1))
                {
                    if (gridData.ContainsKey(new GridPosition(gridPosition)))
                    {
                        GlobalsManager.gameData.credits += activePiece.Cost;
                        gridData.Remove(new GridPosition(gridPosition));
                        RenderGridData();
                        deleteSoundEffect.Play();
                    }
                }

                break;
            case BuildingOption.Delete:
                visualisationSprite.SetActive(false);

                if (Input.GetMouseButtonDown(0))
                {
                    if (gridData.ContainsKey(new GridPosition(gridPosition)))
                    {
                        GlobalsManager.gameData.credits += activePiece.Cost;
                        gridData.Remove(new GridPosition(gridPosition));
                        RenderGridData();
                        deleteSoundEffect.Play();
                    }
                }

                break;
        }

        if (currentHoveredPiece != null)
        {
            hoverInfo.anchoredPosition = (Vector2)Input.mousePosition + new Vector2(5, 5);
            hoverInfo.pivot = new Vector2(hoverInfo.anchoredPosition.x + hoverInfo.sizeDelta.x > Screen.width ? 1 : 0, 0);

            TextMeshProUGUI pieceDisplayNameTitle = hoverInfo.Find("Title").GetComponentInChildren<TextMeshProUGUI>();
            if (pieceDisplayNameTitle.text != currentHoveredPiece.DisplayName)
            {
                // update hover info
                pieceDisplayNameTitle.text = currentHoveredPiece.DisplayName;
                hoverInfo.Find("Description").GetComponent<TextMeshProUGUI>().text = currentHoveredPiece.Description;
                hoverInfo.Find("CostStat").Find("Amount").GetComponentInChildren<TextMeshProUGUI>().text = $"{currentHoveredPiece.Cost} GC";

                Transform massValueBar = hoverInfo.Find("MassStat").Find("ValueBar");
                RectTransform massInnerBar = (RectTransform)massValueBar.Find("BarContainer").Find("InnerBar");
                massValueBar.GetComponentInChildren<TextMeshProUGUI>().text = currentHoveredPiece.Mass.ToString();
                massInnerBar.DOAnchorMax(new Vector2(currentHoveredPiece.Mass / PieceManager.instance.maxMass, 1), 0.3f);

                Transform healthValueBar = hoverInfo.Find("HealthStat").Find("ValueBar");
                RectTransform healthInnerBar = (RectTransform)healthValueBar.Find("BarContainer").Find("InnerBar");
                healthValueBar.GetComponentInChildren<TextMeshProUGUI>().text = currentHoveredPiece.Health.ToString();
                healthInnerBar.DOAnchorMax(new Vector2(currentHoveredPiece.Health / PieceManager.instance.maxHealth, 1), 0.3f);
            }

            hoverInfo.gameObject.SetActive(true);
        } else
        {
            hoverInfo.gameObject.SetActive(false);
        }
    }
    #endregion
}