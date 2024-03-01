using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicDualFireWeapon : PieceBase
{
    private Transform barrels;
    private float lastShot;
    private int projectileSide = 1;
    private PowerRequest powerRequest;
    private Weapon weaponData;
    private float overHeatIncrease;
    private float currentOverHeatAmount;
    private float cooldownTimeLeft;

    private IEnumerator DelayPowerRelease(float delay)
    {
        yield return new WaitForSeconds(delay);
        powerRequest.ReleasePower();
    }

    protected override void InGameStart()
    {
        barrels = transform.Find("Barrels");
        weaponData = (Weapon)pieceData;
        powerRequest = shipController.RequestPower(weaponData.EnergyUsage);
        overHeatIncrease = 1 / weaponData.ContinuousShotsTillCooldown;
    }

    protected override void InGameUpdate()
    {
        if (barrels is null || powerRequest is null) { return; }

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 localMousePosition = transform.parent.InverseTransformPoint(mousePosition);
        Vector2 localDirection = localMousePosition - (Vector2)transform.localPosition;

        Quaternion rotation = Quaternion.Euler(0, 0, transform.localRotation.eulerAngles.z);
        Vector2 rotatedDirection = rotation * localDirection;

        float dotProduct = Vector2.Dot(transform.up, mousePosition - (Vector2)transform.position);
        float angle = Mathf.Atan2(rotatedDirection.y, rotatedDirection.x) * Mathf.Rad2Deg;
        float angleSign = Mathf.Sign(angle);

        angle -= 90f * angleSign;
        
        if (dotProduct < 0)
        {
            angle = angle > 0 ? -90 : 90;
        }

        angle = Mathf.Clamp(angle, -60f, 60f);

        Vector2 directionVector = new Vector2(Mathf.Cos((angle + 90f) * Mathf.Deg2Rad), Mathf.Sin((angle + 90f) * Mathf.Deg2Rad));
        barrels.SetLocalPositionAndRotation(new Vector2(0, -0.05f) + directionVector * 0.05f, Quaternion.Euler(0, 0, angle));

        if (Input.GetMouseButton(0) && lastShot <= 0 && powerRequest.AttemptToUsePower() && cooldownTimeLeft <= 0)
        {
            lastShot = weaponData.FireRate;
            currentOverHeatAmount += overHeatIncrease;
            projectileSide *= -1;

            Quaternion projectileRotation = Quaternion.FromToRotation(weaponData.ProjectilePrefab.transform.right, transform.localToWorldMatrix * directionVector);
            Projectile projectile = Instantiate(weaponData.ProjectilePrefab, transform.position + barrels.up * 0.2f + barrels.right * projectileSide * 0.08f, projectileRotation).GetComponent<Projectile>();
            projectile.ignorePiece = gameObject;
            projectile.originTransform = ShipController.ship;

            StartCoroutine(DelayPowerRelease(lastShot * 0.8f));

            if (currentOverHeatAmount >= 1)
            {
                currentOverHeatAmount = 0;
                cooldownTimeLeft = weaponData.CooldownTime;
            }
        } else if (lastShot <= 0)
        {
            currentOverHeatAmount = Mathf.Max(currentOverHeatAmount - Time.deltaTime, 0);
        }

        lastShot -= Time.deltaTime;
        cooldownTimeLeft -= Time.deltaTime;
    }
}
