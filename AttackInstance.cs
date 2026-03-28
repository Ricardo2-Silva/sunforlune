using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class holds instance-specific state for an attack
[System.Serializable]
public class AttackInstance
{
    // Runtime state
    public bool isCasting { get; set; }
    public ParticleSystem activeParticleSystem { get; set; }
    public GameObject activeParticleObject { get; set; }//padronizar o nome, pois não serão apenas usado particulas
    public TrailRenderer activeTrail { get; set; }
    public Coroutine activeChannelingCoroutine { get; set; }

    public AttackInstance()
    {
        isCasting = false;
        activeParticleSystem = null;
        activeParticleObject = null;
        activeTrail = null;
        activeChannelingCoroutine = null;
    }

    // Helper method to stop particle effects when channeling is canceled
    public void StopChanneling()
    {
        isCasting = false;

        if (activeParticleSystem != null)
        {
            activeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (activeParticleObject != null)
        {
            // Don't destroy immediately to allow particles to fade out
            Object.Destroy(activeParticleObject, 2f);
            activeParticleObject = null;
        }

        activeParticleSystem = null;
        activeChannelingCoroutine = null;
    }

    public void CleanupEffects()
    {
        if (activeParticleObject != null)
        {
            Object.Destroy(activeParticleObject);
            activeParticleObject = null;
        }

        activeParticleSystem = null;
        isCasting = false;
    }
}
