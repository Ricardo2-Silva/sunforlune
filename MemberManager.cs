using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Gerencia os slots de membros da equipe e estado de pokémons em campo.
/// MELHORADO: Métodos auxiliares para conversão RoleHandler/TargetableEntity.
/// </summary>
public class MemberManager : MonoBehaviour
{
    public static MemberManager Instance { get; private set; }

    [Header("Slots")]
    public List<MemberSlot> slots = new List<MemberSlot>(6);

    [Header("Sprites")]
    public Sprite emptySlotSprite;
    public Sprite pokeballClosedSprite;
    public Sprite pokeballOpenSprite;

    [Header("Círculo de Raio de Soltura")]
    public GameObject summonRadiusCircle;
    public float summonRadius = 6.0f;

    // Estado de pokémons em campo
    private Dictionary<RoleHandler, bool> pokemonOnFieldState = new Dictionary<RoleHandler, bool>();

    private Transform TrainerTransform
    {
        get
        {
            if (PokemonSwitchManager.Instance == null) return null;
            var leader = PokemonSwitchManager.Instance.GetTrainerLeader();
            return leader != null ? leader.transform : null;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Registra nos eventos do PSM para atualizar automaticamente
        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnPokemonAddedToTeam.AddListener(OnTeamChanged);
            PokemonSwitchManager.Instance.OnPokemonRemovedFromTeam.AddListener(OnTeamChanged);
        }
    }

    private void OnDestroy()
    {
        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnPokemonAddedToTeam.RemoveListener(OnTeamChanged);
            PokemonSwitchManager.Instance.OnPokemonRemovedFromTeam.RemoveListener(OnTeamChanged);
        }
    }

    private void OnTeamChanged(RoleHandler rh)
    {
        SyncWithTeam(PokemonSwitchManager.Instance.GetTeamMembers());
    }

    private void Update()
    {
        // Sincroniza com o time a cada frame (pode ser otimizado se necessário)
        if (PokemonSwitchManager.Instance != null)
        {
            SyncWithTeam(PokemonSwitchManager.Instance.GetTeamMembers());
        }
    }

    public void SyncWithTeam(List<RoleHandler> teamMembers)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            RoleHandler rh = (i < teamMembers.Count) ? teamMembers[i] : null;
            slots[i].SetRoleHandler(rh);

            // Inicializa estado se necessário
            if (rh != null && !pokemonOnFieldState.ContainsKey(rh))
            {
                pokemonOnFieldState[rh] = false;
            }
        }
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (PokemonSwitchManager.Instance == null) return;

        var team = PokemonSwitchManager.Instance.GetTeamMembers();

        for (int i = 0; i < slots.Count; i++)
        {
            RoleHandler rh = (i < team.Count) ? team[i] : null;
            Image icon = slots[i].slotIcon;

            if (rh == null)
            {
                icon.sprite = emptySlotSprite;
                slots[i].SetInteractable(false);
            }
            else
            {
                bool emCampo = IsPokemonOnField(rh);
                icon.sprite = emCampo ? pokeballOpenSprite : pokeballClosedSprite;
                slots[i].SetInteractable(!emCampo);
            }
        }
    }

    #region Estado de Campo

    public bool IsPokemonOnField(RoleHandler rh)
    {
        if (rh == null) return false;
        return pokemonOnFieldState.ContainsKey(rh) && pokemonOnFieldState[rh];
    }

    public void SetPokemonOnField(RoleHandler rh, bool value)
    {
        if (rh == null) return;
        pokemonOnFieldState[rh] = value;
        UpdateUI();
    }

    /// <summary>
    /// Verifica se um pokémon está em campo usando TargetableEntity.
    /// </summary>
    public bool IsPokemonOnFieldByEntity(TargetableEntity entity)
    {
        if (entity == null) return false;

        RoleHandler rh = GetRoleHandlerFromEntity(entity);
        return rh != null && IsPokemonOnField(rh);
    }

    /// <summary>
    /// Define estado de campo usando TargetableEntity.
    /// </summary>
    public void SetPokemonOnFieldByEntity(TargetableEntity entity, bool value)
    {
        if (entity == null) return;

        RoleHandler rh = GetRoleHandlerFromEntity(entity);
        if (rh != null)
        {
            SetPokemonOnField(rh, value);
        }
    }

    #endregion

    #region Conversão RoleHandler <-> TargetableEntity

    /// <summary>
    /// Obtém o RoleHandler a partir de um TargetableEntity. 
    /// </summary>
    public RoleHandler GetRoleHandlerFromEntity(TargetableEntity entity)
    {
        if (entity == null) return null;

        // Primeiro tenta a referência direta
        if (entity.roleHandler != null)
            return entity.roleHandler;

        // Busca no objeto e hierarquia
        Transform root = entity.transform.parent != null ? entity.transform.parent : entity.transform;
        return root.GetComponentInChildren<RoleHandler>();
    }

    /// <summary>
    /// Obtém o TargetableEntity a partir de um RoleHandler.
    /// </summary>
    public TargetableEntity GetEntityFromRoleHandler(RoleHandler rh)
    {
        if (rh == null) return null;

        Transform root = rh.transform.parent != null ? rh.transform.parent : rh.transform;
        return root.GetComponentInChildren<TargetableEntity>();
    }

    #endregion

    #region Raio de Soltura

    public void ShowSummonRadius(bool show)
    {
        if (summonRadiusCircle != null)
        {
            summonRadiusCircle.SetActive(show);
            if (show && TrainerTransform != null)
            {
                summonRadiusCircle.transform.position = TrainerTransform.position;
            }
        }
    }

    #endregion

    #region Notificações (para uso externo)

    public void NotifyPokemonRecalled(RoleHandler rh)
    {
        SetPokemonOnField(rh, false);
    }

    public void NotifyPokemonReleased(RoleHandler rh)
    {
        SetPokemonOnField(rh, true);
    }

    #endregion
}