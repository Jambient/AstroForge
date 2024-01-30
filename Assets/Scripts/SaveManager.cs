using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class SerializableGrid
{
    public List<Vector2> keys;
    public List<GridCell> values;
}

public class SaveManager : MonoBehaviour
{
    // Function to convert the dictionary to a serializable format
    private SerializableGrid ConvertGridToSerializable(Dictionary<Vector2, GridCell> gridData)
    {
        SerializableGrid serializableDict = new SerializableGrid();
        serializableDict.keys = new List<Vector2>(gridData.Keys);
        serializableDict.values = new List<GridCell>(gridData.Values);

        return serializableDict;
    }

    // Function to convert the serializable data back to a dictionary
    private Dictionary<Vector2, GridCell> ConvertGridFromSerializable(SerializableGrid serializableGrid)
    {
        Dictionary<Vector2, GridCell> gridData = new Dictionary<Vector2, GridCell>();
        for (int i = 0; i < serializableGrid.keys.Count; i++)
        {
            gridData.Add(serializableGrid.keys[i], serializableGrid.values[i]);
        }

        return gridData;
    }

    public void SaveGridData(string filePath, Dictionary<Vector2, GridCell> gridData)
    {
        SerializableGrid serializableGrid = ConvertGridToSerializable(gridData);

        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + $"Ship/{filePath}.dat";

        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, serializableGrid);
        }

        Debug.Log("Data saved to: " + path);
    }

    public bool LoadGridData(string filePath, out Dictionary<Vector2, GridCell> gridData)
    {
        string path = Application.persistentDataPath + $"Ship/{filePath}.dat";
        gridData = new Dictionary<Vector2, GridCell>();

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                SerializableGrid serializableDict = (SerializableGrid)formatter.Deserialize(stream);
                gridData = ConvertGridFromSerializable(serializableDict);
            }

            Debug.Log("Data loaded from: " + path);
            return true;
        }
        else
        {
            Debug.LogError("Save file not found at: " + path);
            return false;
        }
    }
}
