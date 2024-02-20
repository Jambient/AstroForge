using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gyro : PieceBase
{

    private void FixedUpdate()
    {
        if (GlobalsManager.inBuildMode) { return; }

        if (Input.GetKey(KeyCode.A))
        {
            Debug.Log("rotating left");
            shipRb.AddTorque(3);
        } else if (Input.GetKey(KeyCode.D))
        {
            Debug.Log("rotating right");
            shipRb.AddTorque(-3);
        }
    }
}
