using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsDiscovery : MonoBehaviour
{
    private Transform ring;

    private void Start()
    {
        ring = transform.Find("Ring");
    }

    private void Update()
    {
        ring.rotation *= Quaternion.Euler(0, 0, 40 * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.parent == ShipController.ship)
        {
            RoundManager.instance.creditsEarned += Random.Range(20, 100);
            Destroy(gameObject);
        }
    }
}
