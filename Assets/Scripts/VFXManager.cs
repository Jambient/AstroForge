using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager instance { get; private set; }

    #region Public Methods
    public void SpawnParticle(string particleName, Vector2 position)
    {
        StartCoroutine(SpawnParticleCoroutine(particleName, position));
    }
    #endregion

    #region Private Methods
    private IEnumerator SpawnParticleCoroutine(string particleName, Vector2 position)
    {
        // get the particle from the resources folder
        GameObject particle = Instantiate(Resources.Load<GameObject>($"Particles/{particleName}"));
        particle.transform.position = position;

        ParticleSystem particleSystem = particle.GetComponent<ParticleSystem>();
        particleSystem.Play();

        // wait until the particle is guaranteed to have finished
        yield return new WaitForSeconds(particleSystem.main.duration + particleSystem.main.startLifetime.constant);

        Destroy(particle);
    }
    #endregion

    #region MonoBehaviour Messages
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }
    #endregion
}
