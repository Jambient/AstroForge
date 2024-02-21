using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterBase : PieceBase
{
    private ParticleSystem thrustParticle;

    protected override void Start()
    {
        base.Start();
        thrustParticle = GetComponentInChildren<ParticleSystem>();
    }

    private void FixedUpdate()
    {
        if (GlobalsManager.inBuildMode) { return; }

        if (Input.GetKey(KeyCode.W))
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
            thrustParticle.Stop();
        }
    }
}
