using DG.Tweening;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingUIManager : MonoBehaviour
{
    #region Variables
    [Header("Public Variables")]
    public Piece currentHoveredPiece;

    [Header("Class References")]
    [SerializeField] private BuildingSystem buildingSystem;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI availableCurrencyText;
    [SerializeField] private TextMeshProUGUI launchErrorText;
    [SerializeField] private TextMeshProUGUI shipNameText;
    [SerializeField] private TextMeshProUGUI selectedBuildingModeText;

    [SerializeField] private Transform piecesTabs;
    [SerializeField] private Transform piecesScrollViewport;
    [SerializeField] private TextMeshProUGUI piecesTabName;
    [SerializeField] private TextMeshProUGUI piecesTabKnown;
    [SerializeField] private GameObject pieceContainer;

    [SerializeField] private GameObject shipLoadingScreen;
    [SerializeField] private GameObject shipDataContainer;
    [SerializeField] private GameObject noShipsMessage;
    [SerializeField] private Transform newShipModal;
    [SerializeField] private TMP_InputField shipNameInput;

    [SerializeField] private Transform buildingOptionButtons;
    [SerializeField] private RectTransform hoverInfo;
    #endregion

    #region Public Methods
    /// <summary>
    /// Updates and opens the load ship menu.
    /// </summary>
    public void ReturnToShipLoadMenu()
    {
        buildingSystem.SaveCurrentShipData();
        DisplaySavedShips();
        shipLoadingScreen.SetActive(true);
    }

    /// <summary>
    /// Closes the load ship menu and updates and opens the building screen.
    /// </summary>
    public void ShowBuildingScreen()
    {
        shipLoadingScreen.SetActive(false);
        shipNameText.text = buildingSystem.shipData.name;
    }

    /// <summary>
    /// Opens the create new ship modal.
    /// </summary>
    public void OpenNewShipModal()
    {
        newShipModal.gameObject.SetActive(true);
        shipNameInput.text = "";
        shipNameInput.Select();
    }

    /// <summary>
    /// Switches to the given tab on the piece selector.
    /// </summary>
    /// <param name="tabIndex">The index of the PieceCategory to switch to</param>
    public void SwitchToTab(int tabIndex)
    {
        PieceCategory category = (PieceCategory)tabIndex;
        string categoryName = category.ToString();

        // show the correct content container based on the current category
        foreach (Transform contentContainer in piecesScrollViewport)
        {
            contentContainer.gameObject.SetActive(contentContainer.name == categoryName);
        }

        // highlight the clicked tab
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

        // update tab description
        piecesTabName.text = categoryName;
        piecesTabKnown.text = $"{piecesScrollViewport.Find(categoryName).childCount} known";
    }

    /// <summary>
    /// Switches the current build mode to the given one.
    /// </summary>
    /// <param name="optionIndex">The index of the BuildingMode to switch to</param>
    public void SwitchBuildingOption(int optionIndex)
    {
        buildingSystem.currentBuildMode = (BuildingMode)optionIndex;
        string buildingOptionName = buildingSystem.currentBuildMode.ToString();

        // highlight the clicked button
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

        selectedBuildingModeText.text = $"{buildingOptionName} Mode";
    }

    /// <summary>
    /// Sets and shows the launch ship error message
    /// </summary>
    /// <param name="message">The message to display</param>
    public void ShowLaunchErrorMessage(string message)
    {
        launchErrorText.gameObject.SetActive(true);
        launchErrorText.text = message;
    }
    #endregion

    #region Private Methods
    private void GeneratePiecesUI()
    {
        foreach (Piece piece in PieceManager.instance.pieces)
        {
            // clone the piece container and parent it to the correct category content container.
            GameObject newPieceContainer = Instantiate(pieceContainer, piecesScrollViewport.GetChild((int)piece.Category));
            RectTransform previewImage = (RectTransform)newPieceContainer.transform.Find("PreviewImage");
            previewImage.GetComponent<Image>().sprite = piece.PreviewImage;

            // scale the preview image to correctly account for its size
            List<RestrictedPosition> restrictedPositions = PieceManager.instance.GetRestrictedPositionsFromPiece(piece);
            int sizeX = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.x : 0) + 1;
            int sizeY = restrictedPositions.Max((data) => data.restrictionType == RestrictionType.Piece ? data.relativePosition.y : 0) + 1;
            float maxSize = Mathf.Max(sizeX, sizeY);
            previewImage.sizeDelta = new Vector2(110 * (sizeX / maxSize), 110 * (sizeY / maxSize));

            // set the rest of the text boxes on the piece container
            newPieceContainer.transform.Find("PieceName").GetComponent<TextMeshProUGUI>().text = piece.DisplayName;
            newPieceContainer.transform.Find("CostBar").Find("Cost").GetComponent<TextMeshProUGUI>().text = $"{piece.Cost} GC";
            newPieceContainer.SetActive(true);

            // bind the various events on the container
            newPieceContainer.GetComponent<Button>().onClick.AddListener(() => { buildingSystem.OnSelectPiece(piece); });
            EventTrigger trigger = newPieceContainer.GetComponent<EventTrigger>();

            EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
            pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
            pointerEnterEvent.callback.AddListener((data) => { currentHoveredPiece = piece; });

            EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
            pointerExitEvent.eventID = EventTriggerType.PointerExit;
            pointerExitEvent.callback.AddListener((data) => { currentHoveredPiece = null; });

            trigger.triggers.Add(pointerEnterEvent);
            trigger.triggers.Add(pointerExitEvent);
        }
        Destroy(pieceContainer);
    }

    /// <summary>
    /// Updates the Load Ship Menu to show the users saved ships
    /// </summary>
    private void DisplaySavedShips()
    {
        // destroy the previous containers
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
                    // clone the ship data container and fill in the details
                    GameObject containerClone = Instantiate(shipDataContainer, shipDataContainer.transform.parent);
                    containerClone.transform.Find("ShipNameText").GetComponent<TextMeshProUGUI>().text = tempShipData.name;
                    containerClone.transform.Find("LastEditedText").GetComponent<TextMeshProUGUI>().text = $"Last Edited: {tempShipData.lastEdited.ToString("t")} {tempShipData.lastEdited.ToString("d")}";

                    // bind the various onclick events
                    containerClone.transform.GetComponent<Button>().onClick.AddListener(() => {
                        buildingSystem.LoadShip(shipID);
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
    #endregion

    #region MonoBehaviour Messages
    private void Start()
    {
        DisplaySavedShips();
        GeneratePiecesUI();
        SwitchToTab(0);

        availableCurrencyText.gameObject.SetActive(GlobalsManager.currentGameMode == GameMode.Restricted);

        // cancelled input
        shipNameInput.onEndEdit.AddListener(delegate {
            newShipModal.gameObject.SetActive(false);
        });

        // submitted input
        shipNameInput.onSubmit.AddListener(delegate {
            newShipModal.gameObject.SetActive(false);

            buildingSystem.shipData.name = shipNameInput.text;
            buildingSystem.shipData.gridData = SaveManager.instance.ConvertGridToSerializable(new Dictionary<GridPosition, GridCell>());
            int[] shipIDs = SaveManager.instance.GetAllShipIDs();
            int newShipID = shipIDs.Length > 0 ? shipIDs.Max() + 1 : 0;

            SaveManager.instance.SaveShipData(newShipID, buildingSystem.shipData);
            DisplaySavedShips();
        });
    }

    private void Update()
    {
        if (PauseManager.instance.isGamePaused) { return; }

        // update available currency
        if (GlobalsManager.currentGameMode == GameMode.Restricted)
        {
            availableCurrencyText.text = $"AVAILABLE CURRENCY: <color=#01C8B1>{GlobalsManager.gameData.credits} GC";
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
        }
        else
        {
            hoverInfo.gameObject.SetActive(false);
        }
    }
    #endregion
}
