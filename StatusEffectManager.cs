using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private Dictionary<StatusEffectType, StatusEffectInstance> activeEffects = new();
    public SaudePokemon saudePokemon;
    public StatusEffectUI statusEffectUI;

    private void Awake()
    {
        //saudePokemon = GetComponent<SaudePokemon>();
        //statusEffectUI = FindObjectOfType<StatusEffectUI>();
    }

    public void ApplyEffect(StatusEffect effect)
    {
        if (activeEffects.TryGetValue(effect.effectType, out var existingEffect))
        {
            switch (effect.stackType)
            {
                case EffectStackType.None:
                    existingEffect = new StatusEffectInstance(effect, saudePokemon);
                    break;
                case EffectStackType.Stack:
                case EffectStackType.Refresh:
                    existingEffect.AddStack();
                    //statusEffectUI.UpdateEffect(existingEffect);
                    break;
                case EffectStackType.Independent:
                    existingEffect = new StatusEffectInstance(effect, saudePokemon);
                    break;
            }
        }
        else
        {
            var newEffect = new StatusEffectInstance(effect, saudePokemon);
            activeEffects.Add(effect.effectType, newEffect);
            newEffect.ApplyVisualEffects(transform);
            if (statusEffectUI != null)
                statusEffectUI.AddEffect(newEffect);
        }
    }

    public void RemoveEffect(StatusEffectType effectType)
    {
        if (activeEffects.TryGetValue(effectType, out var effect))
        {
            effect.CleanupEffects();
            activeEffects.Remove(effectType);
            if (statusEffectUI != null)
                statusEffectUI.RemoveEffect(effectType);
        }
    }

    private void Update()
    {
        List<StatusEffectType> expired = new();
        foreach (var kvp in activeEffects)
        {
            var effect = kvp.Value;
            effect.UpdateEffect(Time.deltaTime);
            if (statusEffectUI != null)
                statusEffectUI.UpdateEffect(effect);
                Debug.Log("Atualizando UI");
            if (effect.remainingDuration <= 0 && effect.effectData.duration > 0)
                expired.Add(kvp.Key);
        }
        foreach (var type in expired)
            RemoveEffect(type);
    }

    public bool HasEffect(StatusEffectType effectType) => activeEffects.ContainsKey(effectType);

    public float GetEffectValue(StatusEffectType effectType)
    {
        if (activeEffects.TryGetValue(effectType, out var effect))
            return effect.GetCurrentValue();
        return 0f;
    }
    /// <summary>
    /// Retorna uma lista de todos os efeitos ativos.
    /// </summary>
    public List<StatusEffectInstance> GetActiveEffects()
    {
        return new List<StatusEffectInstance>(activeEffects.Values);
    }
}