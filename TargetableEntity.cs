using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Componente que torna um objeto selecionável como alvo.
/// Deve ser anexado aos inimigos/NPCs que podem ser alvejados.
/// CORRIGIDO: Sync de isTargetable no Start, OnDisable/OnEnable para recall.
/// </summary>
public class TargetableEntity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sprites de Indicação")]
    public SpriteRenderer hoverSprite;
    public SpriteRenderer selectedSprite;

    [Header("Configurações")]
    public bool isTargetable = true;
    public float detectionRadius = 5f;

    [Header("Componentes necessários")]
    [SerializeField] private SaudePokemon saudePokemon;
    [SerializeField] private Mon mon;
    [SerializeField] private StatusEffectManager statusManager;
    [SerializeField] private MonHurtBox hurtBox;
    public RoleHandler roleHandler;

    // Estados
    private bool isHovered = false;
    private bool isSelected = false;
    public bool isInCombatWithPlayer = false;

    // Controle interno para evitar registro duplo
    private bool hasInitialized = false;

    // Eventos
    public System.Action<TargetableEntity> OnTargetSelected;
    public System.Action<TargetableEntity> OnTargetDeselected;
    public System.Action<TargetableEntity> OnTargetHovered;
    public System.Action<TargetableEntity> OnTargetUnhovered;
    public System.Action<TargetableEntity> OnStatusEffect;

    private void Awake()
    {
        if (!saudePokemon) saudePokemon = GetComponentInChildren<SaudePokemon>() ?? GetComponentInParent<SaudePokemon>();
        if (!mon) mon = GetComponentInChildren<Mon>() ?? GetComponentInParent<Mon>();
        if (!statusManager) statusManager = GetComponentInChildren<StatusEffectManager>() ?? GetComponentInParent<StatusEffectManager>();
        if (!hurtBox) hurtBox = GetComponentInChildren<MonHurtBox>() ?? GetComponentInParent<MonHurtBox>();
        if (roleHandler == null)
            roleHandler = GetComponentInChildren<RoleHandler>(true);
    }

    private void Start()
    {
        SetHoverState(false);
        SetSelectedState(false);

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 1f;
            collider.isTrigger = true;
        }

        // CORREÇÃO 1: Sincroniza isTargetable com o role atual do RoleHandler
        // Isso resolve o problema de ordem de execução entre scripts
        //if (roleHandler != null)
        //{
        //    PokemonRole role = roleHandler.GetCurrentRole();
        //    isTargetable = (role == PokemonRole.Wild || role == PokemonRole.EnemyAI || role == PokemonRole.AllyAI);
        //}

        // Registra no sistema de seleção
        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.RegisterTarget(this);
        else
            Debug.LogWarning("[TargetableEntity] TargetSelectionManager.Instance é null!");

        hasInitialized = true;
    }

    /// <summary>
    /// Quando desativado (ex: RecallPokemonToPokeball), remove do sistema de seleção.
    /// Isso cobre o caso de SetActive(false) onde OnDestroy não é chamado.
    /// </summary>
    private void OnDisable()
    {
        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.UnregisterTarget(this);
    }

    /// <summary>
    /// Quando reativado (ex: ReleasePokemonAtPosition), re-registra no sistema.
    /// </summary>
    private void OnEnable()
    {
        // Só registra se já passou pelo Start (evita registro duplo na primeira ativação)
        if (hasInitialized && TargetSelectionManager.Instance != null)
        {
            //// Re-sincroniza isTargetable com o role atual
            //if (roleHandler != null)
            //{
            //    PokemonRole role = roleHandler.GetCurrentRole();
            //    isTargetable = (role == PokemonRole.Wild || role == PokemonRole.EnemyAI || role == PokemonRole.AllyAI);
            //}

            TargetSelectionManager.Instance.RegisterTarget(this);
        }
    }

    private void OnDestroy()
    {
        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.UnregisterTarget(this);
    }

    #region Interface do Mouse
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isTargetable) return;

        isHovered = true;
        SetHoverState(true);
        OnTargetHovered?.Invoke(this);

        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.OnTargetHovered(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isTargetable) return;

        isHovered = false;
        SetHoverState(false);
        OnTargetUnhovered?.Invoke(this);

        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.OnTargetUnhovered(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isTargetable) return;

        if (TargetSelectionManager.Instance != null)
            TargetSelectionManager.Instance.SelectTarget(this);
    }
    #endregion

    #region Controle de Estados Visuais
    public void SetHoverState(bool hover)
    {
        if (hoverSprite != null)
            hoverSprite.gameObject.SetActive(hover && !isSelected);
    }

    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        if (selectedSprite != null)
            selectedSprite.gameObject.SetActive(selected);

        if (hoverSprite != null && selected)
            hoverSprite.gameObject.SetActive(false);

        if (!selected && isHovered)
            SetHoverState(true);

        if (selected)
            OnTargetSelected?.Invoke(this);
        else
            OnTargetDeselected?.Invoke(this);
    }
    #endregion

    #region Getters para Informações
    public SaudePokemon GetSaudePokemon() => saudePokemon;
    public Mon GetMon() => mon;
    public StatusEffectManager GetStatusManager() => statusManager;
    public MonHurtBox GetHurtBox() => hurtBox;

    public string GetDisplayName()
    {
        if (mon != null && mon.Base != null)
            return $"{mon.Base.Nome} (Lv.{mon.Nivel})";
        return gameObject.name;
    }

    public float GetHealthPercentage()
    {
        if (saudePokemon != null && saudePokemon.GetSaudeMaxima() > 0)
            return saudePokemon.GetSaudeAtual() / saudePokemon.GetSaudeMaxima();
        return 1f;
    }

    public float GetPowerPercentage()
    {
        if (saudePokemon != null && saudePokemon.GetPoderMaximo() > 0)
            return saudePokemon.GetPoderAtual() / saudePokemon.GetPoderMaximo();
        return 1f;
    }

    public bool IsAlive()
    {
        if (saudePokemon != null)
            return saudePokemon.GetSaudeAtual() > 0;
        return true;
    }

    public Vector3 GetPosition() => transform.position;
    public float GetDistanceFrom(Vector3 position) => Vector3.Distance(transform.position, position);
    #endregion

    #region Callbacks para Eventos de Combate
    public void EnterCombatWithPlayer()
    {
        isInCombatWithPlayer = true;
        if (TargetSelectionManager.Instance != null)
        {
            TargetSelectionManager.Instance.AutoSelectTarget(this);
        }
    }

    public void ExitCombatWithPlayer()
    {
        isInCombatWithPlayer = false;
    }

    public void OnDamageTaken()
    {
        if (TargetSelectionManager.Instance != null && isSelected)
            TargetSelectionManager.Instance.RefreshSelectedTargetUI();
    }

    public void OnStatusEffectChanged()
    {
        if (TargetSelectionManager.Instance != null && isSelected)
            TargetSelectionManager.Instance.RefreshSelectedTargetUI();
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
    #endregion
}