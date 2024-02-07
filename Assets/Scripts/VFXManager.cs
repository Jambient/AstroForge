using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public IEnumerator SpawnParticle(string particleName, Vector2 position)
    {
        GameObject particle = Instantiate(Resources.Load<GameObject>($"Particles/{particleName}"));
        particle.transform.position = position;

        while (particle.GetComponent<ParticleSystem>().isPlaying)
        {
            yield return null;
        }

        //Destroy(particle);
    }
}
