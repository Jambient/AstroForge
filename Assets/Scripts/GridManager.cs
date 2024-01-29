using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public Vector2 gridSize = Vector2.one;

    private void Start()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.size = gridSize / 2;
    }
}
