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

        Camera cam = Camera.main;

        Vector3 min = renderer.bounds.min;
        Vector3 max = renderer.bounds.max;

        Vector3 screenMin = cam.WorldToScreenPoint(min);
        Vector3 screenMax = cam.WorldToScreenPoint(max);

        float screenWidth = screenMax.x - screenMin.x;
        float screenHeight = screenMax.y - screenMin.y;

        transform.localScale = new Vector3(650 / screenWidth, 650 / screenHeight, 1);
    }
}
