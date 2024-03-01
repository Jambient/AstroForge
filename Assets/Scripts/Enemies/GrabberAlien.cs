using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum AlienState
{
    Chasing,
    Attacking
}

public class GrabberAlien : MonoBehaviour, IDamagable
{
    #region Variables
    private Rigidbody2D rb;
    private FixedJoint2D joint;
    private float rotationSpeed = 5f;
    private float movementSpeed = 15f;
    private float currentHealth = 120f;
    private float maxHealth = 120f;
    private int creditsForKill = 50;
    private IDamagable connectedPiece;

    private float attackDelay = 1.5f;
    private float lastAttack;
    private AlienState state;
    #endregion

    #region Public Methods
    public void OnDamage(float damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);

        float destructionLevel = (1 - (currentHealth / maxHealth)) * 0.7f;

        if (currentHealth == 0)
        {
            RoundManager.instance.creditsEarned += creditsForKill;
            Destroy(gameObject);
        }
    }
    #endregion

    #region Private Methods
    private void AttachTo(Rigidbody2D body)
    {
        joint.enabled = true;
        joint.connectedBody = body;
        rb.mass = 0;
    }

    private void DeAttach()
    {
        joint.enabled = false;
        rb.mass = 80;
    }
    #endregion

    #region MonoBehaviour Messages
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<FixedJoint2D>();
    }

    void Update()
    {
        // Check if the ray hit a collider with the "Piece" tag
        RaycastHit2D hit = Physics2D.Raycast(transform.position + transform.up * 0.55f, transform.up, 0.2f);
        if (hit.collider != null && hit.collider.CompareTag("Piece") && state == AlienState.Chasing)
        {
            state = AlienState.Attacking;
            lastAttack = Time.time;
            connectedPiece = hit.collider.gameObject.GetComponent<IDamagable>();

            AttachTo(ShipController.ship.GetComponent<Rigidbody2D>());
        }

        switch (state)
        {
            case AlienState.Chasing:
                Vector3 playerDirection = ShipController.ship.position - transform.position;
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, playerDirection);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);


                break;
            case AlienState.Attacking:
                if (Time.time - lastAttack >= attackDelay)
                {
                    lastAttack = Time.time;
                    connectedPiece.OnDamage(15);
                }

                break;
        }

        // check if the enemy has destroyed the piece it is attacking
        if (connectedPiece == null)
        {
            state = AlienState.Chasing;
            DeAttach();
        }
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case AlienState.Chasing:
                Vector2 movementDirection = transform.up;
                movementDirection.Normalize();
                rb.AddForce(movementDirection * movementSpeed);

                break;
            case AlienState.Attacking:
                Rigidbody2D shipRb = ShipController.ship.GetComponent<Rigidbody2D>();
                if (Mathf.Abs(shipRb.angularVelocity) >= 45)
                {
                    DeAttach();
                }

                break;
        }
    }
    #endregion
}
