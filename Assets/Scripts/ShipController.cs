using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    #region Variables
    public static Transform ship { get; private set; }
    public Vector2 centerOfMass { get; private set; }

    [Header("Public Variables")]
    public static bool isPowerEnabled = true;
    public static Transform core;

    [Header("Script References")]
    [SerializeField] private HUDManager hudManager;

    private ShipData shipData;
    private float shipMass;
    private Rigidbody2D rb;
    private float totalAvailablePower;
    private List<PowerRequest> requestedPowerList = new List<PowerRequest>();
    #endregion

    #region Public Methods
    /// <summary>
    /// Request power to be used at some point
    /// </summary>
    /// <param name="powerAmount">The amount of power needed</param>
    /// <returns>A new PowerRequest</returns>
    public PowerRequest RequestPower(float powerAmount)
    {
        PowerRequest newPowerRequest = new PowerRequest();
        newPowerRequest.amount = powerAmount;

        requestedPowerList.Add(newPowerRequest);

        return newPowerRequest;
    }

    /// <summary>
    /// Releases the requested power
    /// </summary>
    /// <param name="powerRequest">The PowerRequest given previously</param>
    public void StopUsingPower(PowerRequest powerRequest)
    {
        requestedPowerList.Remove(powerRequest);
    }

    /// <summary>
    /// Calculates data about the ship to be used in later calculations
    /// </summary>
    public void CalculateShipData()
    {
        centerOfMass = Vector2.zero;
        shipMass = 0;
        totalAvailablePower = 0;

        foreach (Transform pieceTransform in transform)
        {
            Vector2 piecePosition = pieceTransform.localPosition;
            var pieceData = pieceTransform.GetComponent<PieceBase>().pieceData;

            Vector2 massPosition = piecePosition * pieceData.Mass;
            centerOfMass += massPosition;
            shipMass += pieceData.Mass;

            // if the piece is a Power piece then add its power
            if (pieceData is Power)
            {
                totalAvailablePower += ((Power)pieceData).AvailablePower;

                if (pieceData.DisplayName == "Ship Core")
                {
                    core = pieceTransform;
                }
            }
        }

        // update rigidbody mass and center of mass
        rb.mass = shipMass;
        centerOfMass /= shipMass;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Gets the health of the core as a percentage
    /// </summary>
    /// <returns></returns>
    private float GetCoreHealthPercentage()
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

    /// <summary>
    /// Creates the ship in the world using grid data
    /// </summary>
    /// <param name="data">The grid data of the ship</param>
    private void BuildShip(Dictionary<GridPosition, GridCell> data)
    {
        // calculate the bounding box of the data
        Vector2 topLeftPosition = data.Keys.Aggregate(new Vector2(float.PositiveInfinity, float.PositiveInfinity),
            (current, key) => new Vector2(Mathf.Min(current.x, key.x), Mathf.Min(current.y, key.y)));
        Vector2 topRightPosition = data.Keys.Aggregate(new Vector2(float.NegativeInfinity, float.NegativeInfinity),
            (current, key) => new Vector2(Mathf.Max(current.x, key.x), Mathf.Max(current.y, key.y)));
        Vector2 centerPosition = (topLeftPosition + topRightPosition) / 2;

        // for each piece calculates its actual position and create it
        foreach (KeyValuePair<GridPosition, GridCell> kvp in data)
        {
            var pieceData = PieceManager.instance.GetPieceFromIndex(kvp.Value.pieceIndex);

            // calculate postion relative to the center
            Vector2 newPosition = kvp.Key.ToVector2() - centerPosition;
            Vector2 renderPosition = 0.5f * newPosition;
            renderPosition.y *= -1;

            // add the piece prefab to the current ship build
            GameObject shipPiece = Instantiate(pieceData.Prefab, transform);
            shipPiece.transform.position = renderPosition;
            shipPiece.transform.rotation = Quaternion.Euler(0, 0, kvp.Value.rotation);
        }
    }

    /// <summary>
    /// Turns off the power temporarily
    /// </summary>
    private IEnumerator CrashPower()
    {
        isPowerEnabled = false;
        requestedPowerList.ForEach(powerRequest => { powerRequest.ReleasePower(); });
        yield return new WaitForSeconds(3);
        isPowerEnabled = true;
    }
    #endregion

    #region MonoBehaviour Messages
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ship = transform;
        GlobalsManager.inBuildMode = false;

        if (SaveManager.instance.LoadShipData(GlobalsManager.currentShipID, out shipData))
        {
            BuildShip(SaveManager.instance.ConvertGridFromSerializable(shipData.gridData));
            CalculateShipData();
        }
    }

    private void FixedUpdate()
    {
        // when the user holds space, slow the ship down
        if (Input.GetKey(KeyCode.Space))
        {
            rb.velocity -= rb.velocity * 0.02f;
            rb.angularVelocity *= 0.95f;
        }

        // smoothly update the camera
        Vector3 newPosition = Vector3.Lerp(Camera.main.transform.position, gameObject.transform.position, 0.3f);
        newPosition.z = -10;

        Camera.main.transform.position = newPosition;
    }

    private void Update()
    {
        // calculate the current power usage by summing the active power requests
        float currentPowerUsage = isPowerEnabled ? Mathf.Min(requestedPowerList.Sum(powerRequest => powerRequest.isUsingPower ? powerRequest.amount : 0), totalAvailablePower) : 0;

        // update hud manager with new values
        hudManager.UpdatePowerUsageStat(currentPowerUsage / totalAvailablePower, UpdateMode.Smooth);
        hudManager.UpdateCoreHealthStat(GetCoreHealthPercentage(), UpdateMode.Smooth);

        // check if too much power is being used
        if (currentPowerUsage == totalAvailablePower)
        {
            hudManager.UpdatePowerUsageStat(1, UpdateMode.Immediate);
            StartCoroutine(CrashPower());
        }
    }
    #endregion
}
