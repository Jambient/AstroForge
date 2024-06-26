using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DestroyMode
{
    OnInvisible,
    AfterLifetime
}

public class Projectile : MonoBehaviour
{
    #region Variables
    [Header("Public Variables")]
    public float damage;
    public float speed;
    public DestroyMode destroyMode;
    public GameObject ignorePiece;
    public Transform originTransform;

    private bool isDestroying;
    private float elapsedTime;
    private float maxLifetime = 10;
    #endregion

    #region Private Methods
    private IEnumerator DestroyAfterGivenSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
    #endregion

    #region MonoBehaviour Messages
    private void Update()
    {
        transform.position += transform.right * speed * Time.deltaTime;

        if (destroyMode == DestroyMode.AfterLifetime)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnBecameInvisible()
    {
        if (gameObject.activeSelf && destroyMode == DestroyMode.OnInvisible && !isDestroying)
        {
            isDestroying = true;
            StartCoroutine(DestroyAfterGivenSeconds(1));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<IDamagable>() != null && collision.gameObject != ignorePiece)
        {
            if (!collision.transform.IsChildOf(originTransform))
            {
                collision.GetComponent<IDamagable>().OnDamage(damage);
            }

            isDestroying = true;
            VFXManager.instance.SpawnParticle("Spark", transform.position + transform.right * 0.3f);
            Destroy(gameObject);
        }
    }
    #endregion
}
