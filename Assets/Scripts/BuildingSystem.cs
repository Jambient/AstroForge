using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BuildingMode
{
    Select,
    Build,
    Delete
}

public class BuildingSystem : MonoBehaviour
{
    #region Variables
    [Header("Public Variables")]
    public ShipData shipData;
    public BuildingMode currentBuildMode = BuildingMode.Build;

    [Header("Class References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private BuildingUIManager uiManager;

    [Header("Sound Effect References")]
    [SerializeField] private AudioSource placeSoundEffect;
    [SerializeField] private AudioSource deleteSoundEffect;

    private GridPosition[] directions = { new GridPosition(0, -1), new GridPosition(0, 1), new GridPosition(-1, 0), new GridPosition(1, 0) };
    #endregion

    #region Public Methods
    /// <summary>
    /// Saves the currently open ship data.
    /// </summary>
    public void SaveCurrentShipData()
    {
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridManager.gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);
    }

    public void OnSelectPiece(Piece piece)
    {
        gridManager.SetActivePiece(piece);
    }

    /// <summary>
    /// Validates the users ship and if it is valid then the ship is loaded into the next scene
    /// </summary>
    public void LaunchShip()
    {
        // check that the ship has a core.
        bool hasCore = false;
        foreach (KeyValuePair<GridPosition, GridCell> kvp in gridManager.gridData)
        {
            Piece pieceData = PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex);
            if (pieceData.DisplayName == "Ship Core")
            {
                hasCore = true;
            }

        }
        if (!hasCore)
        {
            uiManager.ShowLaunchErrorMessage("The ship requires a core before launching.");
            return;
        }

        // check that all of the ship's pieces are connected.
        if (gridManager.gridData.Count != FloodFillCountCells(gridManager.gridData.First().Key))
        {
            uiManager.ShowLaunchErrorMessage("All pieces must be connected before launching.");
            return;
        }
        
        // save ship
        SerializableGrid serializableGrid = SaveManager.instance.ConvertGridToSerializable(gridManager.gridData);
        shipData.gridData = serializableGrid;
        SaveManager.instance.SaveShipData(GlobalsManager.currentShipID, shipData);

        // save game data
        SaveManager.instance.SaveGameData(GlobalsManager.gameData);

        // load the user into the correct scene based on game mode.
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

    /// <summary>
    /// Loads the ship with the specified ID if it exists.
    /// </summary>
    /// <param name="shipID">The ID of the ship</param>
    public void LoadShip(int shipID)
    {
        if (SaveManager.instance.LoadShipData(shipID, out shipData))
        {
            GlobalsManager.currentShipID = shipID;
            gridManager.LoadGridFromData(shipData.gridData);
            uiManager.ShowBuildingScreen();
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Recursive flood fill that counts the number of cells with pieces in connected to the starting position.
    /// </summary>
    /// <param name="currentPosition">The current position to be checked (the starting position on the first call)</param>
    /// <param name="visited">List of positions that have already been visited</param>
    /// <returns>The number of connected cells</returns>
    private int FloodFillCountCells(GridPosition currentPosition, List<GridPosition> visited = null)
    {
        visited ??= new List<GridPosition>();
        visited.Add(currentPosition);

        int cellCount = 1;
        foreach (GridPosition direction in directions)
        {
            GridPosition newGridPos = currentPosition + direction;

            // check that new grid position is inside the grid.
            if (newGridPos.x >= 0 && newGridPos.y >= 0 && newGridPos.x < gridManager.gridSize.x && newGridPos.y < gridManager.gridSize.y)
            {
                // check that a piece exists at the position and that the position hasnt already been visited.
                if (gridManager.gridData.ContainsKey(newGridPos) && !visited.Contains(newGridPos))
                {
                    cellCount += FloodFillCountCells(newGridPos, visited);
                }
            }
        }

        return cellCount;
    }
    #endregion

    #region MonoBehaviour Messages
    private void Start()
    {
        GlobalsManager.inBuildMode = true;
        gridManager.SetActivePiece(PieceManager.instance.pieces[0]);

        if (GlobalsManager.currentShipID >= 0)
        {
            Debug.Log("wow!");
            LoadShip(GlobalsManager.currentShipID);
        }
    }

    private void Update()
    {
        if (PauseManager.instance.isGamePaused) { return; }

        switch (currentBuildMode)
        {
            case BuildingMode.Select:
                gridManager.showPieceVisualisation = false;

                if (gridManager.GetCellDataAtPositionIfExists(gridManager.mouseGridPosition, out GridCell cellData))
                {
                    uiManager.currentHoveredPiece = PieceManager.instance.GetPieceFromIndex(cellData.pieceIndex);
                } else if (gridManager.IsMouseOnGrid())
                {
                    uiManager.currentHoveredPiece = null;
                }

                break;
            case BuildingMode.Build:
                gridManager.showPieceVisualisation = true;
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gridManager.RotatePlacement(-90);
                }

                if (Input.GetMouseButtonDown(0) && (GlobalsManager.currentGameMode == GameMode.Sandbox || GlobalsManager.gameData.credits >= gridManager.activePiece.Cost) && gridManager.PlaceActivePieceIfValid())
                {
                    GlobalsManager.gameData.credits -= gridManager.activePiece.Cost;
                    placeSoundEffect.Play();
                } else if (Input.GetMouseButtonDown(1))
                {
                    if (gridManager.ClearCellDataAtPositionIfExists(gridManager.mouseGridPosition, out GridCell deletedCellData))
                    {
                        GlobalsManager.gameData.credits += PieceManager.instance.GetPieceFromIndex(deletedCellData.pieceIndex).Cost;
                        deleteSoundEffect.Play();
                    }
                }

                break;
            case BuildingMode.Delete:
                gridManager.showPieceVisualisation = false;

                if (Input.GetMouseButton(0))
                {
                    if (gridManager.ClearCellDataAtPositionIfExists(gridManager.mouseGridPosition, out GridCell deletedCellData))
                    {
                        GlobalsManager.gameData.credits += PieceManager.instance.GetPieceFromIndex(deletedCellData.pieceIndex).Cost;
                        deleteSoundEffect.Play();
                    }
                }

                break;
        }
    }
    #endregion
}