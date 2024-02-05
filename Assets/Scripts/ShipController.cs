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
    private ShipData shipData;
    private float shipMass;
    private Vector2 centerOfMass = Vector2.zero;
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

            // pre calculate some data
            Vector2 massPosition = renderPosition * pieceData.Mass;
            centerOfMass += massPosition;

            shipMass += pieceData.Mass;

            if (pieceData is Engine)
            {
                thrusters.Add(new ThrusterData(((Engine)pieceData).Thrust, renderPosition));
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
        }

        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            foreach (ThrusterData thruster in thrusters)
            {
                Vector2 directionToThruster = -(thruster.position - centerOfMass);
                Vector2 worldDirection = transform.TransformDirection(directionToThruster);
                rb.AddForceAtPosition(worldDirection.normalized * thruster.thrust * Time.deltaTime, thruster.position);

                float torque = Vector3.Cross(directionToThruster, Vector3.forward).z * thruster.thrust * Time.deltaTime;
                rb.AddTorque(torque);
            }
        }

        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity *= 0.95f;
            
        }
        Debug.Log(rb.velocity);

        // move camera
        Vector3 newPosition = Vector3.Lerp(Camera.main.transform.position, gameObject.transform.position, 0.3f);
        newPosition.z = -10;

        Camera.main.transform.position = newPosition;
    }
}
