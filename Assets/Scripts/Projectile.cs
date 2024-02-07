using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage;
    public float speed;

    private float currentLifetime = 0;
    private float maxLifetime = 10;

    private void Update()
    {
        currentLifetime += Time.deltaTime;
        transform.position += transform.right * speed * Time.deltaTime;

        if (currentLifetime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.CompareTag("Piece"))
        {
            PieceBase pieceBase = collision.gameObject.GetComponent<PieceBase>();
            pieceBase.DamagePiece(damage);

            StartCoroutine(VFXManager.Instance.SpawnParticle("Spark", transform.position + transform.right * 0.3f));

            Destroy(gameObject);
        }
    }
}
