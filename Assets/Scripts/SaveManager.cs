using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class SerializableGrid
{
    public List<GridPosition> keys;
    public List<GridCell> values;
}

[System.Serializable]
public struct ShipData
{
    public string name;
    public DateTime lastEdited;
    public SerializableGrid gridData;
}

[System.Serializable]
public struct GameData
{
    public int currentRound;
    public int credits;
    public int researchPoints;
}

public enum GameMode
{
    Restricted,
    Sandbox
}
public class SaveManager : MonoBehaviour
{
    #region Variables
    public static SaveManager instance { get; private set; }
    #endregion

    #region Public Methods
    /// <summary>
    /// Converts a dictionary containing grid data to the serializable grid class
    /// </summary>
    /// <param name="gridData">The grid data to convert</param>
    /// <returns>The serializable grid version of the grid data</returns>
    public SerializableGrid ConvertGridToSerializable(Dictionary<GridPosition, GridCell> gridData)
    {
        SerializableGrid serializableDict = new SerializableGrid();
        serializableDict.keys = new List<GridPosition>(gridData.Keys);
        serializableDict.values = new List<GridCell>(gridData.Values);

        return serializableDict;
    }

    /// <summary>
    /// Converts a serializable grid back into a dictionary.
    /// </summary>
    /// <param name="serializableGrid">The serializable grid</param>
    /// <returns>The dictionary version of the grid data</returns>
    public Dictionary<GridPosition, GridCell> ConvertGridFromSerializable(SerializableGrid serializableGrid)
    {
        Dictionary<GridPosition, GridCell> gridData = new Dictionary<GridPosition, GridCell>();
        for (int i = 0; i < serializableGrid.keys.Count; i++)
        {
            gridData.Add(serializableGrid.keys[i], serializableGrid.values[i]);
        }

        return gridData;
    }

    /// <summary>
    /// Gets all the ship IDs relating to the current game mode.
    /// </summary>
    /// <returns>All Ship IDs found in the current game mode folder</returns>
    public int[] GetAllShipIDs()
    {
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentGameMode}/";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        return Directory.GetFiles(folderPath).Select(path => int.Parse(Path.GetFileNameWithoutExtension(path))).ToArray();
    }

    /// <summary>
    /// Saves the given ship data to the file relating to the given ship ID
    /// </summary>
    /// <param name="shipID">The ship ID to save to</param>
    /// <param name="shipData">The ship data to save</param>
    public void SaveShipData(int shipID, ShipData shipData)
    {
        shipData.lastEdited = DateTime.Now;

        BinaryFormatter formatter = new BinaryFormatter();
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentGameMode}/";
        string filePath = $"{folderPath}{shipID}.dat";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            formatter.Serialize(stream, shipData);
        }
    }

    /// <summary>
    /// Loads ship data for the given shipID
    /// </summary>
    /// <param name="shipID">The ID of the ship to load</param>
    /// <param name="shipData">Stores the ship data</param>
    /// <returns>True if the data exists, False otherwise</returns>
    public bool LoadShipData(int shipID, out ShipData shipData)
    {
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentGameMode}/";
        string filePath = $"{folderPath}{shipID}.dat";
        shipData = new ShipData();

        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                shipData = (ShipData)formatter.Deserialize(stream);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the current game data
    /// </summary>
    /// <param name="gameData">The game data</param>
    public void SaveGameData(GameData gameData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string filePath = $"{Application.persistentDataPath}/GameSave.dat";

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            formatter.Serialize(stream, gameData);
        }
    }

    /// <summary>
    /// Loads the saved game data
    /// </summary>
    /// <param name="gameData">A copy of the game data</param>
    /// <returns>True if the game data was succesfully loaded, False otherwise</returns>
    public bool LoadGameData(out GameData gameData)
    {
        string filePath = $"{Application.persistentDataPath}/GameSave.dat";
        gameData = new GameData();

        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                gameData = (GameData)formatter.Deserialize(stream);
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the data for the ship with the given ID
    /// </summary>
    /// <param name="shipID">The ID of the ship</param>
    /// <returns></returns>
    public bool DeleteShipData(int shipID)
    {
        string path = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentGameMode}/{shipID}.dat";

        if (File.Exists(path))
        {
            File.Delete(path);
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region MonoBehaviour Messages
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        } else
        {
            instance = this;
        }
    }
    #endregion
}
