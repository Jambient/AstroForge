using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerRequest
{
    public float amount;
    public bool isUsingPower = false;

    public bool AttemptToUsePower()
    {
        isUsingPower = true && ShipController.isPowerEnabled;
        return isUsingPower;
    }

    public void ReleasePower()
    {
        isUsingPower = false;
    }
}

public class ShipController : MonoBehaviour
{
    public static Transform ship { get; private set; }
    public static bool isPowerEnabled = true;
    public static Transform core;

    [SerializeField] private HUDManager hudManager;

    public Vector2 centerOfMass { get; private set; }
    private ShipData shipData;
    private float shipMass;
    private Rigidbody2D rb;
    private float totalAvailablePower;
    private List<PowerRequest> requestedPowerList = new List<PowerRequest>();

    public PowerRequest RequestPowerUsage(float powerAmount)
    {
        PowerRequest newPowerRequest = new PowerRequest();
        newPowerRequest.amount = powerAmount;

        requestedPowerList.Add(newPowerRequest);

        return newPowerRequest;
    }
    public void StopUsingPower(PowerRequest powerRequest)
    {
        requestedPowerList.Remove(powerRequest);
    }

    public void CalculateShipData()
    {
        centerOfMass = Vector2.zero;
        shipMass = 0;
        totalAvailablePower = 0;

        foreach (Transform pieceTransform in transform)
        {
            Vector2 piecePosition = pieceTransform.localPosition;
            var pieceData = pieceTransform.GetComponent<PieceBase>().pieceData;

            // pre calculate some data
            Vector2 massPosition = piecePosition * pieceData.Mass;
            centerOfMass += massPosition;

            shipMass += pieceData.Mass;
            if (pieceData is Power)
            {
                totalAvailablePower += ((Power)pieceData).AvailablePower;
                if (pieceData.DisplayName == "Ship Core")
                {
                    core = pieceTransform;
                }
            }
        }

        rb.mass = shipMass;

        centerOfMass /= shipMass;
    }

    public float GetCoreHealthPercentage()
    {
        if (core)
        {
            PieceBase pieceBase = core.GetComponent<PieceBase>();
            return pieceBase.health / pieceBase.pieceData.Health;
        } else
        {
            return 0;
        }
    }

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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ship = transform;

        if (SaveManager.instance.LoadShipData(GlobalsManager.currentShipID, out shipData))
        {
            BuildShip(SaveManager.instance.ConvertGridFromSerializable(shipData.gridData));
            CalculateShipData();
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity -= rb.velocity * 0.02f;
            rb.angularVelocity *= 0.95f;
        }

        // move camera
        Vector3 newPosition = Vector3.Lerp(Camera.main.transform.position, gameObject.transform.position, 0.3f);
        newPosition.z = -10;

        Camera.main.transform.position = newPosition;
    }

    private IEnumerator CrashPower()
    {
        isPowerEnabled = false;
        requestedPowerList.ForEach(powerRequest => { powerRequest.ReleasePower(); });
        yield return new WaitForSeconds(3);
        isPowerEnabled = true;
    }

    private void Update()
    {
        float currentPowerUsage = isPowerEnabled ? Mathf.Min(requestedPowerList.Sum(powerRequest => powerRequest.isUsingPower ? powerRequest.amount : 0), totalAvailablePower) : 0;

        hudManager.UpdatePowerUsageStat(currentPowerUsage / totalAvailablePower, UpdateMode.Smooth);
        hudManager.UpdateCoreHealthStat(GetCoreHealthPercentage(), UpdateMode.Smooth);

        if (currentPowerUsage == totalAvailablePower)
        {
            hudManager.UpdatePowerUsageStat(1, UpdateMode.Immediate);
            StartCoroutine(CrashPower());
        }
    }
}
