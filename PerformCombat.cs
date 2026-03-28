using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformCombat : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private Mon combatSettings;
    

    [Header("É o jogador?")]
    public bool isPlayer = false;

    public AssistantAttackClass LastUsedAttack { get; private set; }

    [Header("Global Cooldown")]
    public float globalCooldown = 1.0f;
    private float lastGlobalAttackTime = -Mathf.Infinity;

    public SaudePokemon saudePokemon;

    // Track currently channeling attack for cancellation
    private AssistantAttackClass currentChannelingAttack;
    public bool blockCombatInput = false;
    public MonHurtBox hurtBox;

    private void Awake()
    {

    }

    public static SaudePokemon FindSaudePokemon(Transform root)
    {
        SaudePokemon found = root.GetComponent<SaudePokemon>();
        if (found != null) return found;
        found = root.GetComponentInChildren<SaudePokemon>();
        if (found != null) return found;
        found = root.GetComponentInParent<SaudePokemon>();
        return found;
    }

    void Start()
    {
        if (hurtBox != null)
        {
            //Debug.LogError("HurtBox ausente.");
            hurtBox.OnTookDamage += HandleTookDamage;
            //hurtBox.OnBecameInvulnerable += HandleBecameInvulnerable;
            hurtBox.OnRecoveredFromHurt += HandleRecoveredInvulnerable;
        }
    }

    // Notificação de dano (pode cancelar ataque ou aplicar feedback extra)
    private void HandleTookDamage()
    {
        // Opcional: cancela canalização, ataque ou input
        if (currentChannelingAttack != null)
            CancelAttack(currentChannelingAttack);
        blockCombatInput = true;
    }
    private void HandleRecoveredInvulnerable()
    {
        blockCombatInput = false;
    }

    // ATENÇÃO: agora só executa ataques com PP suficiente
    public void PerformRandomAttack(Transform target)
    {
        var currentAttacks = combatSettings.Attacks;
        if (currentAttacks == null || currentAttacks.Count == 0) return;

        if (!IsGlobalCooldownReady()) return;

        // Filtra ataques prontos para uso e que tenham PP suficiente
        List<AssistantAttackClass> readyAttacks = new List<AssistantAttackClass>();
        foreach (var atk in currentAttacks)
        {
            float custoPoder = atk.data != null ? atk.data.pontosPoder : 0f;
            if (atk.IsOffCooldown() && saudePokemon != null && saudePokemon.TemPontosPoderPara(custoPoder))
            {
                readyAttacks.Add(atk);
            }
        }

        if (readyAttacks.Count == 0) return;

        var chosen = readyAttacks[Random.Range(0, readyAttacks.Count)];
        float custo = chosen.data != null ? chosen.data.pontosPoder : 0f;
        if (!saudePokemon.ConsumirPontosPoder(custo)) return;

        LastUsedAttack = chosen;

        Vector2 direction = target.position - transform.position;
        ExecuteAttack(chosen, direction.normalized);
    }

    public void PerformManualAttack(AssistantAttackClass chosen, Vector2 direction)
    {
        Debug.Log(blockCombatInput);
        if (blockCombatInput) return;
        if (!IsGlobalCooldownReady() || !chosen.IsOffCooldown())
            return;

        float custoPoder = (chosen.data != null) ? chosen.data.pontosPoder : 0f;
        if (saudePokemon != null && custoPoder > 0f && !saudePokemon.ConsumirPontosPoder(custoPoder))
        {
            // Sem poder suficiente, não ataca!
            // Adicione feedback visual/auditivo aqui se quiser (ex: flash, shake, som, etc)
            return;
        }

        if (chosen.IsChanneled)
        {
            chosen.StartCasting();
            currentChannelingAttack = chosen;
            LastUsedAttack = chosen;

            if (CastingUIManager.Instance != null)
                CastingUIManager.Instance.ShowCastBar(chosen.data.nomeAtaque, chosen.data.castTime);

            chosen.CurrentRoutine = StartCoroutine(ChanneledAttackRoutine(chosen, direction));
        }
        else
        {
            LastUsedAttack = chosen;
            ExecuteAttack(chosen, direction);
        }
    }

    private IEnumerator ChanneledAttackRoutine(AssistantAttackClass attack, Vector2 direction)
    {
        var instance = attack.Instance;
        instance.activeChannelingCoroutine = StartCoroutine(attack.data.AttackRoutine(combatSettings.transform, direction, instance));

        float startTime = Time.time;

        while (attack.IsCasting && Time.time < startTime + attack.data.castTime)
        {
            yield return null;
        }

        if (attack.IsCasting)
        {
            attack.StopCasting();
            attack.TriggerCooldown();
            TriggerGlobalCooldown();
            if (CastingUIManager.Instance != null)
                CastingUIManager.Instance.HideCastBar();
        }

        currentChannelingAttack = null;
        attack.CurrentRoutine = null;
    }

    void ExecuteAttack(AssistantAttackClass chosen, Vector2 direction)
    {
        var instance = chosen.Instance;

        switch (chosen.data.executionType)
        {
            case AttackExecutionType.Instant:
                chosen.data.ExecuteAttack(combatSettings.transform, direction, instance);
                break;
            case AttackExecutionType.Coroutine:
                StartCoroutine(chosen.data.AttackRoutine(combatSettings.transform, direction, instance));
                break;
        }

        chosen.TriggerCooldown();
        TriggerGlobalCooldown();
    }

    public void CancelAttack(AssistantAttackClass assistant)
    {
        if (assistant != null && assistant.CurrentRoutine != null)
        {
            StopCoroutine(assistant.CurrentRoutine);

            assistant.StopCasting();

            var instance = assistant.Instance;

            if (instance.activeChannelingCoroutine != null)
            {
                StopCoroutine(instance.activeChannelingCoroutine);
                instance.activeChannelingCoroutine = null;
            }

            assistant.TriggerCooldown();
            TriggerGlobalCooldown();

            if (CastingUIManager.Instance != null)
                CastingUIManager.Instance.HideCastBar();

            if (currentChannelingAttack == assistant) currentChannelingAttack = null;
            assistant.CurrentRoutine = null;
        }
    }

    public bool IsGlobalCooldownReady() => Time.time >= lastGlobalAttackTime + globalCooldown;
    void TriggerGlobalCooldown() => lastGlobalAttackTime = Time.time;
    public float LastGlobalAttackTime => lastGlobalAttackTime;
    public List<AssistantAttackClass> GetAttackInstances() => combatSettings.Attacks;

    private void OnDestroy()
    {
        foreach (var attack in combatSettings.Attacks)
        {
            attack.CleanupEffects();
        }
    }
}