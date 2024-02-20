using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public struct ShipData
{
    public string name;
    public SerializableGrid gridData;
}

[System.Serializable]
public class SerializableGrid
{
    public List<GridPosition> keys;
    public List<GridCell> values;
}

public enum BuildMode
{
    Restricted,
    Sandbox
}
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance { get; private set; }

    // Function to convert the dictionary to a serializable format
    public SerializableGrid ConvertGridToSerializable(Dictionary<GridPosition, GridCell> gridData)
    {
        SerializableGrid serializableDict = new SerializableGrid();
        serializableDict.keys = new List<GridPosition>(gridData.Keys);
        serializableDict.values = new List<GridCell>(gridData.Values);

        return serializableDict;
    }

    // Function to convert the serializable data back to a dictionary
    public Dictionary<GridPosition, GridCell> ConvertGridFromSerializable(SerializableGrid serializableGrid)
    {
        Dictionary<GridPosition, GridCell> gridData = new Dictionary<GridPosition, GridCell>();
        for (int i = 0; i < serializableGrid.keys.Count; i++)
        {
            gridData.Add(serializableGrid.keys[i], serializableGrid.values[i]);
        }

        return gridData;
    }

    public int[] GetAllShipIDs()
    {
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentBuildMode}/";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        return Directory.GetFiles(folderPath).Select(path => int.Parse(Path.GetFileNameWithoutExtension(path))).ToArray();
    }

    public void SaveShipData(int shipID, ShipData shipData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentBuildMode}/";
        string filePath = $"{folderPath}{shipID}.dat";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            formatter.Serialize(stream, shipData);
        }

        Debug.Log("Data saved to: " + filePath);
    }

    public bool LoadShipData(int shipID, out ShipData shipData)
    {
        string folderPath = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentBuildMode}/";
        string filePath = $"{folderPath}{shipID}.dat";
        shipData = new ShipData();

        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                shipData = (ShipData)formatter.Deserialize(stream);
            }

            Debug.Log("Data loaded from: " + filePath);
            return true;
        }
        else
        {
            Debug.LogError("Save file not found at: " + filePath);
            return false;
        }
    }

    public bool DeleteShipData(int shipID)
    {
        string path = Application.persistentDataPath + $"/Ships/{GlobalsManager.currentBuildMode}/{shipID}.dat";

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Data deleted at: " + path);
            return true;
        }
        else
        {
            Debug.LogWarning("No data file found at: " + path);
            return false;
        }
    }

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
}
