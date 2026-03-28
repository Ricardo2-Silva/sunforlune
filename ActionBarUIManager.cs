using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Gerencia a barra de ações (action bar) com slots de ataques. 
/// REFATORADO: Agora é um singleton GLOBAL que fica no Canvas principal.
/// Atualiza automaticamente ao trocar pokémon controlado.
/// </summary>
public class ActionBarUIManager : MonoBehaviour
{
    public static ActionBarUIManager Instance { get; private set; }

    [Header("Referências dos slots (ordem importa:  slot 0 = ataque 0)")]
    public List<ActionBarSlot> slots;

    [Header("Painel Principal")]
    public GameObject actionBarPanel;

    [Header("Estado Atual")]
    [SerializeField] private Mon monAtual;
    [SerializeField] private CombatManager combatManagerAtual;
    [SerializeField] private PerformCombat performCombatAtual;
    [SerializeField] private SaudePokemon saudePokemonAtual;
    [SerializeField] private MonHurtBox hurtBoxAtual;

    private List<AssistantAttackClass> activeAttacks = new List<AssistantAttackClass>(4);

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("[ActionBarUI] Start() chamado");

        SetActionBarVisible(false);

        if (PokemonSwitchManager.Instance != null)
        {
            Debug.Log("[ActionBarUI] PSM encontrado, registrando listener");
            PokemonSwitchManager.Instance.OnControlledMemberChanged.AddListener(OnControlledPokemonChanged);
        }
        else
        {
            Debug.LogError("[ActionBarUI] PSM. Instance é NULL no Start()!");
        }
        // Inicia DESATIVADA
        SetActionBarVisible(false);

        // Registra no evento de troca de controle
        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnControlledMemberChanged.AddListener(OnControlledPokemonChanged);

            // Verifica se já há um pokémon controlado ao iniciar
            var controlado = PokemonSwitchManager.Instance.GetControlledMember();
            if (controlado != null)
            {
                OnControlledPokemonChanged(controlado);
            }
        }
    }

    private void OnDestroy()
    {
        // Desregistra evento
        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnControlledMemberChanged.RemoveListener(OnControlledPokemonChanged);
        }

        // Desregistra eventos do hurtbox atual
        DesregistrarEventosHurtBox();
    }

    private void DesregistrarEventosHurtBox()
    {
        if (hurtBoxAtual != null)
        {
            hurtBoxAtual.OnTookDamage -= ShowBlockedOverlay;
            hurtBoxAtual.OnRecoveredFromHurt -= HideBlockedOverlay;
        }
    }

    /// <summary>
    /// Chamado automaticamente quando o pokémon controlado muda.
    /// </summary>
    private void OnControlledPokemonChanged(RoleHandler newControlled)
    {
        Debug.Log($"[ActionBarUI] OnControlledPokemonChanged chamado.  newControlled = {(newControlled != null ? newControlled.name : "NULL")}");
        // Limpa referências anteriores
        DesregistrarEventosHurtBox();
        LimparReferencias();

        if (newControlled == null)
        {
            SetActionBarVisible(false);
            return;
        }

        // Verifica se é o Trainer Leader (não usa action bar)
        if (newControlled == PokemonSwitchManager.Instance.GetTrainerLeader())
        {
            SetActionBarVisible(false);
            return;
        }

        // Busca o objeto raiz do pokémon
        Transform rootTransform = newControlled.transform.parent != null
            ? newControlled.transform.parent
            : newControlled.transform;

        // Atualiza todas as referências
        monAtual = newControlled.GetMon();
        saudePokemonAtual = rootTransform.GetComponentInChildren<SaudePokemon>();
        hurtBoxAtual = rootTransform.GetComponentInChildren<MonHurtBox>();
        performCombatAtual = rootTransform.GetComponentInChildren<PerformCombat>();
        combatManagerAtual = rootTransform.GetComponentInChildren<CombatManager>();

        // Valida se encontrou o Mon
        if (monAtual == null)
        {
            Debug.LogWarning($"[ActionBarUI] Mon não encontrado em {newControlled.name}");
            SetActionBarVisible(false);
            return;
        }

        // Registra eventos do novo hurtbox
        if (hurtBoxAtual != null)
        {
            hurtBoxAtual.OnTookDamage += ShowBlockedOverlay;
            hurtBoxAtual.OnRecoveredFromHurt += HideBlockedOverlay;
        }

        // Inicializa action bar com ataques do novo pokémon
        InicializarActionBar();
        SetActionBarVisible(true);
    }

    private void LimparReferencias()
    {
        monAtual = null;
        combatManagerAtual = null;
        performCombatAtual = null;
        saudePokemonAtual = null;
        hurtBoxAtual = null;
        activeAttacks.Clear();
    }

    /// <summary>
    /// Inicializa a action bar com os últimos 4 ataques do pokémon.
    /// </summary>
    public void InicializarActionBar()
    {
        activeAttacks.Clear();

        if (monAtual == null || monAtual.Attacks == null)
        {
            AtualizarSlots();
            return;
        }

        var allAttacks = monAtual.Attacks;
        int count = Mathf.Min(4, allAttacks.Count);

        for (int i = 0; i < count; i++)
        {
            activeAttacks.Add(allAttacks[allAttacks.Count - count + i]);
        }

        AtualizarSlots();

        // Atualiza o CombatManager com os ataques disponíveis
        if (combatManagerAtual != null)
        {
            combatManagerAtual.SetAvailableAttacks(activeAttacks);
        }
    }

    public void SetActionBarAttack(int slot, AssistantAttackClass novoAtaque)
    {
        if (slot < 0 || slot >= slots.Count) return;
        if (novoAtaque == null) return;
        if (activeAttacks.Contains(novoAtaque)) return;

        if (slot < activeAttacks.Count)
            activeAttacks[slot] = novoAtaque;
        else if (slot == activeAttacks.Count)
            activeAttacks.Add(novoAtaque);

        AtualizarSlots();

        if (combatManagerAtual != null)
        {
            combatManagerAtual.SetAvailableAttacks(activeAttacks);
        }
    }

    private void Update()
    {
        if (performCombatAtual == null || !actionBarPanel.activeSelf) return;

        float globalCooldown = performCombatAtual.globalCooldown;
        float lastGlobal = performCombatAtual.LastGlobalAttackTime;
        float globalTimeLeft = Mathf.Max(0, (lastGlobal + globalCooldown) - Time.time);
        bool globalEmCooldown = globalTimeLeft > 0;

        for (int i = 0; i < slots.Count; i++)
        {
            AtualizarSlotCooldown(i, globalEmCooldown, globalTimeLeft, globalCooldown);
        }
    }

    private void AtualizarSlotCooldown(int i, bool globalEmCooldown, float globalTimeLeft, float globalCooldown)
    {
        var slot = slots[i];

        // Atualiza overlay de cooldown global
        if (slot.cooldownOverlayGlobal != null)
        {
            slot.cooldownOverlayGlobal.enabled = globalEmCooldown;
            slot.cooldownOverlayGlobal.fillAmount = (globalEmCooldown && globalCooldown > 0f)
                ? (globalTimeLeft / globalCooldown)
                : 0f;
        }

        // Valores padrão
        bool emCooldown = false;
        float timeLeft = 0f;
        float cooldown = 0f;
        float custoPoder = 0f;
        bool podeAtacar = true;

        // Calcula valores se houver ataque neste slot
        if (i < activeAttacks.Count && activeAttacks[i] != null && activeAttacks[i].data != null)
        {
            var ataque = activeAttacks[i];
            cooldown = ataque.data.cooldown;
            float lastUsed = ataque.LastUsedTime;
            timeLeft = Mathf.Max(0, (lastUsed + cooldown) - Time.time);
            emCooldown = timeLeft > 0;

            custoPoder = ataque.data.pontosPoder;
            podeAtacar = saudePokemonAtual != null ? saudePokemonAtual.TemPontosPoderPara(custoPoder) : true;
        }

        // Atualiza overlay de cooldown da skill
        if (slot.cooldownOverlaySkill != null)
        {
            slot.cooldownOverlaySkill.enabled = emCooldown;
            slot.cooldownOverlaySkill.fillAmount = (emCooldown && cooldown > 0f)
                ? (timeLeft / cooldown)
                : 0f;
        }

        // Atualiza texto de cooldown
        if (slot.cooldownText != null)
        {
            slot.cooldownText.text = emCooldown ? timeLeft.ToString("F1") + "s" : "";
        }

        // Atualiza cor do ícone baseado em poder disponível
        if (slot.icon != null)
        {
            slot.icon.color = podeAtacar ? Color.white : Color.gray;
        }
    }

    public void AtualizarSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < activeAttacks.Count && activeAttacks[i] != null && activeAttacks[i].data != null)
            {
                var ataque = activeAttacks[i];
                slots[i].icon.sprite = ataque.data.icone;
                slots[i].icon.enabled = true;

                if (slots[i].cooldownOverlaySkill != null)
                    slots[i].cooldownOverlaySkill.enabled = false;
                if (slots[i].cooldownOverlayGlobal != null)
                    slots[i].cooldownOverlayGlobal.enabled = false;
                if (slots[i].blockedOverlay != null)
                    slots[i].blockedOverlay.enabled = false;
                if (slots[i].cooldownText != null)
                    slots[i].cooldownText.text = "";
            }
            else
            {
                slots[i].SetEmpty();
            }
        }
    }

    public void OnActionBarButton(int slotIndex)
    {
        if (combatManagerAtual == null || performCombatAtual == null) return;
        if (slotIndex < 0 || slotIndex >= activeAttacks.Count) return;

        var atk = activeAttacks[slotIndex];
        if (atk == null || atk.data == null) return;

        // Verifica cooldown da skill
        float cd = atk.data.cooldown;
        float last = atk.LastUsedTime;
        float timeLeft = Mathf.Max(0, (last + cd) - Time.time);

        // Verifica cooldown global
        float globalCooldown = performCombatAtual.globalCooldown;
        float lastGlobal = performCombatAtual.LastGlobalAttackTime;
        float globalTimeLeft = Mathf.Max(0, (lastGlobal + globalCooldown) - Time.time);

        // Verifica poder
        float custoPoder = atk.data.pontosPoder;
        bool podeAtacar = saudePokemonAtual != null ? saudePokemonAtual.TemPontosPoderPara(custoPoder) : true;

        if (globalTimeLeft > 0 || timeLeft > 0 || !podeAtacar) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - (Vector2)combatManagerAtual.transform.position).normalized;
        combatManagerAtual.TryUseAttack(slotIndex, direction);
    }

    public void ShowBlockedOverlay()
    {
        foreach (var slot in slots)
            slot.ShowBlockedOverlay(true);
    }

    public void HideBlockedOverlay()
    {
        foreach (var slot in slots)
            slot.ShowBlockedOverlay(false);
    }

    /// <summary>
    /// Mostra/esconde o painel da action bar.
    /// </summary>
    public void SetActionBarVisible(bool visible)
    {
        if (actionBarPanel != null)
        {
            actionBarPanel.SetActive(visible);
        }
    }

    // Getters para acesso externo se necessário
    public Mon GetMonAtual() => monAtual;
    public List<AssistantAttackClass> GetActiveAttacks() => new List<AssistantAttackClass>(activeAttacks);
}