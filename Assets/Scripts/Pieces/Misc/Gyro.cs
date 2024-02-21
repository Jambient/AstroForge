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
            shipRb.AddTorque(1500 * Time.deltaTime);
        } else if (Input.GetKey(KeyCode.D))
        {
            shipRb.AddTorque(-1500 * Time.deltaTime);
        }
    }
}
