using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemy : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform target;

    private float shotDelay = 1;
    private float timeSinceLastShot = 0;

    // Update is called once per frame
    void Update()
    {
        timeSinceLastShot -= Time.deltaTime;

        if (timeSinceLastShot <= 0) {
            timeSinceLastShot = shotDelay;

            Quaternion projectileRotation = Quaternion.FromToRotation(projectilePrefab.transform.right, target.transform.position - transform.position);

            Instantiate(projectilePrefab, transform.position, projectileRotation);
        }
    }
}
