using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager instance { get; private set; }

    public void SpawnParticle(string particleName, Vector2 position)
    {
        StartCoroutine(SpawnParticleCoroutine(particleName, position));
    }
    private IEnumerator SpawnParticleCoroutine(string particleName, Vector2 position)
    {
        GameObject particle = Instantiate(Resources.Load<GameObject>($"Particles/{particleName}"));
        particle.transform.position = position;

        ParticleSystem particleSystem = particle.GetComponent<ParticleSystem>();
        particleSystem.Play();

        yield return new WaitForSeconds(particleSystem.main.duration + particleSystem.main.startLifetime.constant);

        Destroy(particle);
    }

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
}
