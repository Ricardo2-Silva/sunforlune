using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AssistantAttackClass
{
    public AttackData data;
    private float lastUsedTime = -Mathf.Infinity;

    [SerializeField, HideInInspector]
    private AttackInstance instanceBacking;

    public AttackInstance Instance => instanceBacking ??= new AttackInstance();
    public AssistantAttackClass(AttackData data)
    {
        this.data = data;
    }

    public bool IsChanneled => data.isChanneledAttack;
    public bool IsCasting => Instance.isCasting;
    public Coroutine CurrentRoutine { get; set; }
    public GameObject ActiveParticleObject => Instance.activeParticleObject;
    public float LastUsedTime => lastUsedTime;
    public void StartCasting() => Instance.isCasting = true;

    public void StopCasting() => Instance.StopChanneling();

    public bool IsOffCooldown() => Time.time >= lastUsedTime + data.cooldown;

    public void TriggerCooldown() => lastUsedTime = Time.time;
    public void CleanupEffects() => Instance.CleanupEffects();

}
