using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicDualFireWeapon : PieceBase
{
    [SerializeField] private GameObject projectilePrefab;

    private Transform barrels;
    private float lastShot;
    private int projectileSide = 1;

    protected override void Start()
    {
        base.Start();
        barrels = transform.Find("Barrels");
    }

    void Update()
    {
        if (GlobalsManager.inBuildMode) { return; }

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

        if (Input.GetMouseButton(0) && lastShot <= 0)
        {
            lastShot = ((Weapon)pieceData).FireRate;
            projectileSide *= -1;

            Quaternion projectileRotation = Quaternion.FromToRotation(projectilePrefab.transform.right, transform.localToWorldMatrix * directionVector);
            Projectile projectile = Instantiate(projectilePrefab, transform.position + barrels.up * 0.2f + barrels.right * projectileSide * 0.08f, projectileRotation).GetComponent<Projectile>();
            projectile.ignorePiece = gameObject;
        }

        lastShot -= Time.deltaTime;
    }
}
