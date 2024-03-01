using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum FighterState
{
    Chasing,
    Attacking
}

public class FighterEnemy : MonoBehaviour, IDamagable
{
    #region Variables
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform leftSideFiring;
    [SerializeField] private Transform rightSideFiring;

    private float currentHealth = 200f;
    private float maxHealth = 200f;
    private int creditsForKill = 100;
    private float moveSpeed = 2f;
    private float rotationSpeed = 0.5f;
    private float attackDelay = 1.5f;
    private float lastAttack;
    private Renderer baseRenderer;
    private FighterState state;
    #endregion

    #region Public Methods
    public void OnDamage(float damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);

        float destructionLevel = (1 - (currentHealth / maxHealth)) * 0.7f;
        baseRenderer.material.SetFloat("_DestructionLevel", destructionLevel);

        if (currentHealth == 0)
        {
            RoundManager.instance.creditsEarned += creditsForKill;
            Destroy(gameObject);
        }
    }
    #endregion

    #region MonoBehaviour Messages
    private void Awake()
    {
        baseRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        Vector3 playerDirection = ShipController.ship.position - transform.position;

        if (playerDirection.magnitude > 5)
        {
            state = FighterState.Chasing;
        } else
        {
            state = FighterState.Attacking;
        }
        
        // rotate enemy gradually towards player
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, playerDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        switch (state)
        {
            case FighterState.Chasing:
                transform.position += transform.up * moveSpeed * Time.deltaTime;
                break;
            case FighterState.Attacking:
                if (Time.time - lastAttack >= attackDelay)
                {
                    lastAttack = Time.time;

                    Quaternion projectileRotation = Quaternion.FromToRotation(projectilePrefab.transform.right, playerDirection);

                    Projectile leftProjectile = Instantiate(projectilePrefab, leftSideFiring.position, projectileRotation).GetComponent<Projectile>();
                    leftProjectile.ignorePiece = gameObject;
                    leftProjectile.originTransform = transform.parent;

                    Projectile rightProjectile = Instantiate(projectilePrefab, rightSideFiring.position, projectileRotation).GetComponent<Projectile>();
                    rightProjectile.ignorePiece = gameObject;
                    rightProjectile.originTransform = transform.parent;
                }
                break;
        }
    }
    #endregion
}
