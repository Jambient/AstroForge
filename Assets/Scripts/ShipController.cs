using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct ThrusterData
{
    public float thrust;
    public Vector2 position;

    public ThrusterData(float thrust, Vector2 position)
    {
        this.thrust = thrust;
        this.position = position;
    }
}

public class ShipController : MonoBehaviour
{
    public Vector2 centerOfMass { get; private set; }
    private ShipData shipData;
    private float shipMass;
    private Rigidbody2D rb;
    private List<ThrusterData> thrusters = new List<ThrusterData>();

    [SerializeField] private GameObject testingCircle;

    private void BuildShip(Dictionary<GridPosition, GridCell> data)
    {
        Vector2 topLeftPosition = new Vector2(Mathf.Infinity, Mathf.Infinity);
        Vector2 topRightPosition = new Vector2(-Mathf.Infinity, -Mathf.Infinity);
        foreach (KeyValuePair<GridPosition, GridCell> kvp in data)
        {
            topLeftPosition.x = Mathf.Min(topLeftPosition.x, kvp.Key.x);
            topLeftPosition.y = Mathf.Min(topLeftPosition.y, kvp.Key.y);
            topRightPosition.x = Mathf.Max(topRightPosition.x, kvp.Key.x);
            topRightPosition.y = Mathf.Max(topRightPosition.y, kvp.Key.y);
        }
        Vector2 centerPosition = (topLeftPosition + topRightPosition) / 2;

        foreach (KeyValuePair<GridPosition, GridCell> kvp in data)
        {
            Vector2 newPosition = kvp.Key.ToVector2() - centerPosition;
            Vector2 renderPosition = 0.5f * newPosition;
            renderPosition.y *= -1;

            var pieceData = PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex);

            // add the piece prefab to the current ship build
            GameObject shipPiece = Instantiate(pieceData.Prefab, transform);
            shipPiece.transform.position = renderPosition;
            shipPiece.transform.rotation = Quaternion.Euler(0, 0, kvp.Value.rotation);
        }
    }

    public void CalculateShipData()
    {
        centerOfMass = Vector2.zero;
        thrusters.Clear();
        shipMass = 0;

        foreach (Transform pieceTransform in transform)
        {
            Vector2 piecePosition = pieceTransform.localPosition;
            var pieceData = pieceTransform.GetComponent<PieceBase>().pieceData;

            // pre calculate some data
            Vector2 massPosition = piecePosition * pieceData.Mass;
            centerOfMass += massPosition;

            shipMass += pieceData.Mass;

            if (pieceData is Thruster)
            {
                thrusters.Add(new ThrusterData(((Thruster)pieceData).Thrust, piecePosition));
            }
        }

        centerOfMass /= shipMass;
        testingCircle.transform.position = centerOfMass;
    }

    private void Start()
    {
        if (SaveManager.instance.LoadShipData(GlobalsManager.currentShipID, out shipData))
        {
            BuildShip(SaveManager.instance.ConvertGridFromSerializable(shipData.gridData));
            CalculateShipData();
        }

        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity *= 0.95f;
            rb.angularVelocity *= 0.95f;
        }

        // move camera
        Vector3 newPosition = Vector3.Lerp(Camera.main.transform.position, gameObject.transform.position, 0.3f);
        newPosition.z = -10;

        Camera.main.transform.position = newPosition;
    }

    
}
