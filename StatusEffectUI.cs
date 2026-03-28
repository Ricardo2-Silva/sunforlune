using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectUI : MonoBehaviour
{
    public GameObject effectIconPrefab;
    public Transform iconParent;

    private Dictionary<StatusEffectType, StatusEffectIcon> icons = new();

    public void AddEffect(StatusEffectInstance effect)
    {
        var type = effect.effectData.effectType;
        if (icons.ContainsKey(type)) return;
        var go = Instantiate(effectIconPrefab, iconParent);
        var icon = go.GetComponent<StatusEffectIcon>();
        icon.SetIcon(effect.effectData.icon);
        icon.SetDuration(effect.remainingDuration);
        icon.SetStacks(effect.currentStacks);
        icons[type] = icon;
    }

    public void UpdateEffect(StatusEffectInstance effect)
    {
        var type = effect.effectData.effectType;
        if (icons.TryGetValue(type, out var icon))
        {
            icon.SetDuration(effect.remainingDuration);
            icon.SetStacks(effect.currentStacks);
        }
    }

    public void RemoveEffect(StatusEffectType type)
    {
        if (icons.TryGetValue(type, out var icon))
        {
            Destroy(icon.gameObject);
            icons.Remove(type);
        }
    }
}
