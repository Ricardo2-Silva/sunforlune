using UnityEngine;

public class StatusEffectInstance
{
    public StatusEffect effectData { get; private set; }
    public int currentStacks { get; private set; }
    public float remainingDuration { get; private set; }
    public float nextTickTime { get; private set; }
    private GameObject activeVFX;
    private ParticleSystem activeParticles;
    private SaudePokemon targetSaude;

    public StatusEffectInstance(StatusEffect data, SaudePokemon alvo)
    {
        effectData = data;
        currentStacks = 1;
        remainingDuration = data.duration;
        nextTickTime = Time.time + data.tickRate;
        targetSaude = alvo;
    }

    public void AddStack()
    {
        if (effectData.stackType == EffectStackType.Stack && currentStacks < effectData.maxStacks)
            currentStacks++;
        if (effectData.stackType == EffectStackType.Refresh)
            remainingDuration = effectData.duration;
    }

    public float GetCurrentValue()
    {
        return effectData.effectValue * (1 + (currentStacks - 1) * effectData.stackMultiplier);
    }

    public void UpdateEffect(float deltaTime)
    {
        if (effectData.duration > 0)
            remainingDuration -= deltaTime;

        if (Time.time >= nextTickTime)
        {
            ProcessTick();
            nextTickTime = Time.time + effectData.tickRate;
        }
    }

    private void ProcessTick()
    {
        if (targetSaude == null) return;

        switch (effectData.effectType)
        {
            case StatusEffectType.Poison:
            case StatusEffectType.Burn:
                targetSaude.ReceberDano(GetCurrentValue());
                break;
            case StatusEffectType.Heal:
                targetSaude.Curar(Mathf.RoundToInt(GetCurrentValue()));
                break;
                // Adicione aqui buffs/debuffs e outros efeitos conforme necessário
        }
    }

    public void ApplyVisualEffects(Transform target)
    {
        if (effectData.effectPrefab != null)
            activeVFX = GameObject.Instantiate(effectData.effectPrefab, target);
        if (effectData.particles != null)
        {
            activeParticles = GameObject.Instantiate(effectData.particles, target);
            activeParticles.Play();
        }
    }

    public void CleanupEffects()
    {
        if (activeVFX != null) GameObject.Destroy(activeVFX);
        if (activeParticles != null)
        {
            activeParticles.Stop();
            GameObject.Destroy(activeParticles.gameObject, 2f);
        }
    }
}