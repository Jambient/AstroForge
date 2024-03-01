using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterBase : PieceBase
{
    private ParticleSystem thrustParticle;
    private PowerRequest powerRequest;

    protected override void InGameStart()
    {
        thrustParticle = GetComponentInChildren<ParticleSystem>();
        powerRequest = shipController.RequestPower(20);
    }

    protected override void InGameFixedUpdate()
    {
        if (Input.GetKey(KeyCode.W) && powerRequest.AttemptToUsePower())
        {
            //Vector2 directionToThruster = -((Vector2)transform.localPosition - shipController.centerOfMass);
            //Vector2 worldDirection = transform.TransformDirection(directionToThruster);
            //shipRb.AddForceAtPosition(worldDirection.normalized * ((Thruster)pieceData).Thrust * Time.deltaTime, (Vector2)transform.localPosition);

            //float torque = Vector3.Cross(directionToThruster, Vector3.up).z * ((Thruster)pieceData).Thrust * Time.deltaTime;
            //shipRb.AddTorque(torque);

            shipRb.AddForce(transform.up * ((Thruster)pieceData).Thrust * Time.deltaTime);

            if (!thrustParticle.isPlaying)
            {
                thrustParticle.Play();
            }
        } else
        {
            powerRequest.ReleasePower();
            if (thrustParticle.isPlaying)
            {
                thrustParticle.Stop();
            }
        }
    }

    private void OnDestroy()
    {
        if (powerRequest != null)
        {
            shipController.StopUsingPower(powerRequest);
        }
    }
}
