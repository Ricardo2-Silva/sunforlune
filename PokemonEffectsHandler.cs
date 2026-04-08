using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonEffectsHandler : MonoBehaviour
{
    public GameObject exclamarPrefab;
    public AudioClip fleeSound;
    public void ShowAlertEffect(GameObject exclamarPrefab)
    {
        if (exclamarPrefab == null) return;
        GameObject excl = Instantiate(exclamarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
        Destroy(excl, 1.5f);
    }
    public void PlayFleeSound(AudioClip fleeSound)
    {
        if (fleeSound == null) return;
        AudioSource.PlayClipAtPoint(fleeSound, transform.position);
    }
    public void StartFade()
    {
        // Implemente fade-out visual
    }
}
