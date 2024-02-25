using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemy : MonoBehaviour, IDamagable
{
    [SerializeField] private GameObject projectilePrefab;

    private Transform barrel;
    private float shotDelay = 1.5f;
    private float timeSinceLastShot = 0;
    private float currentHealth = 120f;
    private float maxHealth = 120f;
    private int creditsForKill = 50;
    private Renderer baseRenderer;
    private Renderer barrelRenderer;

    private void Start()
    {
        baseRenderer = GetComponent<Renderer>();
        barrel = transform.Find("Barrel");
        barrelRenderer = barrel.GetComponent<Renderer>();
    }

    void Update()
    {
        if (!ShipController.ship) { return; }
        timeSinceLastShot -= Time.deltaTime;

        Vector3 direction = ShipController.ship.position - transform.position;
        if (direction.magnitude < 10)
        {
            barrel.localPosition = direction.normalized * 0.6f;
            barrel.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);

            if (timeSinceLastShot <= 0)
            {
                timeSinceLastShot = shotDelay;

                Quaternion projectileRotation = Quaternion.FromToRotation(projectilePrefab.transform.right, direction);

                Projectile projectile = Instantiate(projectilePrefab, transform.position + direction.normalized * 0.65f, projectileRotation).GetComponent<Projectile>();
                projectile.ignorePiece = gameObject;
                projectile.originTransform = transform.parent;
            }
        }
    }

    public void OnDamage(float damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        
        float destructionLevel = (1 - (currentHealth / maxHealth)) * 0.7f;
        baseRenderer.material.SetFloat("_DestructionLevel", destructionLevel);
        barrelRenderer.material.SetFloat("_DestructionLevel", destructionLevel);

        if (currentHealth == 0)
        {
            RoundManager.instance.creditsEarned += creditsForKill;
            Destroy(gameObject);
        }
    }
}
