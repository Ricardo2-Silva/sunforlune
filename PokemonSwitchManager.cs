using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador central de troca entre pokémons e controle do jogador.
/// CORRIGIDO: TAB só troca entre pokémons em campo, eventos UnityEvent.
/// </summary>
public class PokemonSwitchManager : MonoBehaviour
{
    public static PokemonSwitchManager Instance { get; private set; }

    [Header("Trainer Leader (obrigatório)")]
    [SerializeField] private RoleHandler trainerLeader;

    [Header("Controle")]
    [SerializeField] private RoleHandler controlledMember;
    public const int MAX_TEAM_SIZE = 6;

    [Header("Pokémon Team & Storage")]
    public List<RoleHandler> teamMembers = new List<RoleHandler>();
    public List<RoleHandler> pcStorage = new List<RoleHandler>();

    [Header("Input Settings")]
    public KeyCode switchToLeaderKey = KeyCode.L;
    public KeyCode switchNextPokemonKey = KeyCode.Tab;
    public KeyCode[] directSwitchKeys = new KeyCode[] {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6
    };

    [Header("UI References")]
    public PokemonInfoUI playerHUD;

    [Header("VFX")]
    public GameObject pokeballPrefab;
    public GameObject liberationVFXPrefab;

    // Eventos usando UnityEvent
    [System.Serializable]
    public class RoleHandlerEvent : UnityEngine.Events.UnityEvent<RoleHandler> { }

    public RoleHandlerEvent OnControlledMemberChanged = new RoleHandlerEvent();
    public RoleHandlerEvent OnTrainerLeaderChanged = new RoleHandlerEvent();
    public RoleHandlerEvent OnPokemonAddedToTeam = new RoleHandlerEvent();
    public RoleHandlerEvent OnPokemonRemovedFromTeam = new RoleHandlerEvent();
    public RoleHandlerEvent OnPokemonSentToPC = new RoleHandlerEvent();
    public RoleHandlerEvent OnPokemonRetrievedFromPC = new RoleHandlerEvent();
    public RoleHandlerEvent OnPokemonReleased = new RoleHandlerEvent();

    // Controle de empréstimo temporário de líder
    private RoleHandler originalLeader;
    private Coroutine tempLeaderRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ValidateTrainerLeader();
        SwitchControlToLeader();
    }

    private void ValidateTrainerLeader()
    {
        if (trainerLeader == null)
        {
            Debug.LogError("[PSM] Trainer Leader não atribuído!  Sistema não funcionará.");
            enabled = false;
            return;
        }
        originalLeader = trainerLeader;
    }

    private void Update()
    {
        HandleSwitchInput();
    }

    #region Input Handling

    private void HandleSwitchInput()
    {
        if (Input.GetKeyDown(switchToLeaderKey))
            SwitchControlToLeader();

        if (Input.GetKeyDown(switchNextPokemonKey))
            SwitchControlToNextPokemonEmCampo();

        for (int i = 0; i < directSwitchKeys.Length && i < teamMembers.Count; i++)
        {
            if (Input.GetKeyDown(directSwitchKeys[i]))
                TentarTrocarParaPokemon(i);
        }
    }

    #endregion

    #region Control Switching

    public void SwitchControlToLeader()
    {
        if (trainerLeader == null) return;

        // Desativa controle de todos os pokémons
        foreach (var pokemon in teamMembers)
        {
            if (pokemon != null)
                pokemon.ApplyRole(PokemonRole.AllyAI);
        }

        trainerLeader.ApplyRole(PokemonRole.PlayerControlled);
        controlledMember = trainerLeader;

        OnControlledMemberChanged?.Invoke(controlledMember);
        UpdatePlayerHUD();
    }

    /// <summary>
    /// Tenta trocar para um pokémon por índice.  Só troca se estiver em campo.
    /// </summary>
    public void TentarTrocarParaPokemon(int index)
    {
        if (index < 0 || index >= teamMembers.Count) return;

        var pokemon = teamMembers[index];
        if (pokemon == null) return;

        // CORREÇÃO: Só troca se o pokémon estiver em campo
        if (MemberManager.Instance != null && !MemberManager.Instance.IsPokemonOnField(pokemon))
        {
            Debug.Log($"[PSM] Pokémon {pokemon.GetMon()?.Base?.Nome ?? "?"} não está em campo.  Não é possível trocar.");
            return;
        }

        SwitchControlToPokemon(index);
    }

    /// <summary>
    /// Troca controle para um pokémon específico (uso interno, sem verificação de campo).
    /// </summary>
    public void SwitchControlToPokemon(int index)
    {
        if (index < 0 || index >= teamMembers.Count) return;

        var pokemon = teamMembers[index];
        if (pokemon == null) return;

        // Desativa controle do líder
        trainerLeader.ApplyRole(PokemonRole.AllyAI);

        // Desativa outros pokémons e ativa o selecionado
        for (int i = 0; i < teamMembers.Count; i++)
        {
            if (teamMembers[i] != null)
                teamMembers[i].ApplyRole(i == index ? PokemonRole.PlayerControlled : PokemonRole.AllyAI);
        }

        controlledMember = pokemon;

        OnControlledMemberChanged?.Invoke(controlledMember);
        UpdatePlayerHUD();
    }

    /// <summary>
    /// Troca para o próximo pokémon EM CAMPO.
    /// </summary>
    public void SwitchControlToNextPokemonEmCampo()
    {
        // Filtra apenas pokémons em campo
        List<int> indicesEmCampo = new List<int>();

        for (int i = 0; i < teamMembers.Count; i++)
        {
            if (teamMembers[i] != null &&
                MemberManager.Instance != null &&
                MemberManager.Instance.IsPokemonOnField(teamMembers[i]))
            {
                indicesEmCampo.Add(i);
            }
        }

        // Se não há pokémons em campo, volta para o líder
        if (indicesEmCampo.Count == 0)
        {
            SwitchControlToLeader();
            return;
        }

        // Encontra o índice atual
        int indexAtual = -1;
        if (controlledMember != null && controlledMember != trainerLeader)
        {
            indexAtual = teamMembers.IndexOf(controlledMember);
        }

        // Encontra o próximo índice em campo
        int proximoIndiceEmCampo = -1;

        if (indexAtual == -1)
        {
            // Controlando líder ou ninguém, vai para o primeiro em campo
            proximoIndiceEmCampo = indicesEmCampo[0];
        }
        else
        {
            // Encontra o próximo na lista de em campo
            int posicaoAtualNaLista = indicesEmCampo.IndexOf(indexAtual);

            if (posicaoAtualNaLista == -1)
            {
                // Pokémon atual não está em campo (não deveria acontecer)
                proximoIndiceEmCampo = indicesEmCampo[0];
            }
            else
            {
                // Vai para o próximo, com wrap-around
                int proximaPosicao = (posicaoAtualNaLista + 1) % indicesEmCampo.Count;
                proximoIndiceEmCampo = indicesEmCampo[proximaPosicao];
            }
        }

        SwitchControlToPokemon(proximoIndiceEmCampo);
    }

    /// <summary>
    /// Libera um pokémon no mundo na posição especificada.
    /// </summary>
    public void ReleasePokemonAtPosition(RoleHandler rh, Vector3 targetWorldPos)
    {
        if (rh == null || !teamMembers.Contains(rh)) return;

        if (MemberManager.Instance != null && MemberManager.Instance.IsPokemonOnField(rh))
        {
            Debug.Log("[PSM] Pokémon já em campo.  Não é possível lançar novamente.");
            return;
        }

        // Busca o objeto raiz do pokémon
        Transform parentTransform = rh.transform.parent != null ? rh.transform.parent : rh.transform;
        GameObject pokemonRoot = parentTransform.gameObject;

        // Ativa antes de posicionar
        if (!pokemonRoot.activeSelf)
            pokemonRoot.SetActive(true);

        // Posiciona
        pokemonRoot.transform.position = targetWorldPos;

        // Marca como em campo ANTES de trocar controle
        if (MemberManager.Instance != null)
            MemberManager.Instance.SetPokemonOnField(rh, true);

        // Troca para controle do jogador
        SwitchControlToPokemon(teamMembers.IndexOf(rh));

        // VFX de liberação
        if (liberationVFXPrefab != null)
        {
            Instantiate(liberationVFXPrefab, targetWorldPos, Quaternion.identity);
        }
    }

    /// <summary>
    /// Recolhe um pokémon para sua pokébola.
    /// </summary>
    public void RecallPokemonToPokeball(RoleHandler pokemon)
    {
        if (pokemon == null || !teamMembers.Contains(pokemon)) return;

        // Mantém compatibilidade (se alguém ainda chama esse)
        StartCoroutine(RecallUnitRoutine(pokemon, 1.5f));
    }

    public void RecallPokemonToPokeball(RoleHandler pokemon, float delay)
    {
        if (pokemon == null || !teamMembers.Contains(pokemon)) return;
        StartCoroutine(RecallUnitRoutine(pokemon, delay));
    }

    public void RecallPokemonToPokeballImmediate(RoleHandler pokemon)
    {
        if (pokemon == null || !teamMembers.Contains(pokemon)) return;

        if (controlledMember == pokemon)
            SwitchControlToLeader();

        GameObject toDisable = pokemon.transform.parent != null ? pokemon.transform.parent.gameObject : pokemon.gameObject;
        toDisable.SetActive(false);
        pokemon.ApplyRole(PokemonRole.AllyAI);

        if (MemberManager.Instance != null)
            MemberManager.Instance.SetPokemonOnField(pokemon, false);

        Debug.Log($"[PSM] {pokemon.GetMon()?.Base?.Nome ?? "Pokémon"} retornou para sua Pokébola (imediato)!");
    }

    private IEnumerator RecallUnitRoutine(RoleHandler pokemon, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Se estava controlando este pokémon, volta para o líder
        if (controlledMember == pokemon)
        {
            SwitchControlToLeader();
        }

        // Desativa objeto do Pokémon no mundo
        GameObject toDisable = pokemon.transform.parent != null ? pokemon.transform.parent.gameObject : pokemon.gameObject;
        toDisable.SetActive(false);
        pokemon.ApplyRole(PokemonRole.AllyAI);

        // Marca como fora de campo
        if (MemberManager.Instance != null)
            MemberManager.Instance.SetPokemonOnField(pokemon, false);

        Debug.Log($"[PSM] {pokemon.GetMon()?.Base?.Nome ?? "Pokémon"} retornou para sua Pokébola!");
    }

    #endregion

    #region Team Management

    public bool AddPokemonToTeam(RoleHandler pokemon)
    {
        if (pokemon == null) return false;

        if (teamMembers.Count < MAX_TEAM_SIZE)
        {
            teamMembers.Add(pokemon);
            pokemon.ApplyRole(PokemonRole.AllyAI);
            OnPokemonAddedToTeam?.Invoke(pokemon);
            return true;
        }

        // Se time cheio, envia direto pro PC
        pcStorage.Add(pokemon);
        OnPokemonSentToPC?.Invoke(pokemon);
        return false;
    }

    public void SendToPC(RoleHandler pokemon)
    {
        if (teamMembers.Contains(pokemon))
        {
            if (controlledMember == pokemon)
                SwitchControlToLeader();

            teamMembers.Remove(pokemon);
            pcStorage.Add(pokemon);

            OnPokemonRemovedFromTeam?.Invoke(pokemon);
            OnPokemonSentToPC?.Invoke(pokemon);
        }
    }

    public bool RetrieveFromPC(RoleHandler pokemon)
    {
        if (!pcStorage.Contains(pokemon)) return false;

        if (teamMembers.Count < MAX_TEAM_SIZE)
        {
            pcStorage.Remove(pokemon);
            teamMembers.Add(pokemon);
            pokemon.ApplyRole(PokemonRole.AllyAI);

            OnPokemonRetrievedFromPC?.Invoke(pokemon);
            OnPokemonAddedToTeam?.Invoke(pokemon);
            return true;
        }
        return false;
    }

    public void SwapWithPC(RoleHandler teamPokemon, RoleHandler pcPokemon)
    {
        if (!teamMembers.Contains(teamPokemon) || !pcStorage.Contains(pcPokemon))
            return;

        int teamIndex = teamMembers.IndexOf(teamPokemon);

        if (controlledMember == teamPokemon)
            SwitchControlToLeader();

        teamMembers[teamIndex] = pcPokemon;
        pcStorage.Remove(pcPokemon);
        pcStorage.Add(teamPokemon);

        OnPokemonRemovedFromTeam?.Invoke(teamPokemon);
        OnPokemonSentToPC?.Invoke(teamPokemon);
        OnPokemonRetrievedFromPC?.Invoke(pcPokemon);
        OnPokemonAddedToTeam?.Invoke(pcPokemon);
    }

    public void ReleasePokemon(RoleHandler pokemon)
    {
        if (teamMembers.Contains(pokemon))
        {
            if (controlledMember == pokemon)
                SwitchControlToLeader();

            teamMembers.Remove(pokemon);
            OnPokemonRemovedFromTeam?.Invoke(pokemon);
        }
        else if (pcStorage.Contains(pokemon))
        {
            pcStorage.Remove(pokemon);
        }

        OnPokemonReleased?.Invoke(pokemon);
    }

    #endregion

    #region Trainer Leader Management

    public void SetTemporaryLeader(RoleHandler newLeader, float duration)
    {
        if (newLeader == null || newLeader == trainerLeader) return;

        if (tempLeaderRoutine != null)
            StopCoroutine(tempLeaderRoutine);

        if (originalLeader == null)
            originalLeader = trainerLeader;

        trainerLeader.ApplyRole(PokemonRole.AllyAI);
        trainerLeader = newLeader;
        trainerLeader.ApplyRole(PokemonRole.PlayerControlled);

        OnTrainerLeaderChanged?.Invoke(trainerLeader);

        if (duration > 0)
            tempLeaderRoutine = StartCoroutine(RestoreOriginalLeaderAfter(duration));
    }

    private IEnumerator RestoreOriginalLeaderAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (originalLeader != null)
        {
            trainerLeader.ApplyRole(PokemonRole.AllyAI);
            trainerLeader = originalLeader;
            trainerLeader.ApplyRole(PokemonRole.PlayerControlled);
            OnTrainerLeaderChanged?.Invoke(trainerLeader);
        }

        tempLeaderRoutine = null;
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Atualiza o HUD do jogador com informações do pokémon controlado.
    /// </summary>
    private void UpdatePlayerHUD()
    {
        if (playerHUD == null) return;

        // Se está controlando o líder, esconde o HUD do jogador
        if (controlledMember == trainerLeader)
        {
            playerHUD.SetVisible(false);
            return;
        }

        if (controlledMember == null)
        {
            playerHUD.SetVisible(false);
            return;
        }

        // Busca o objeto raiz do pokémon
        Transform rootTransform = controlledMember.transform.parent != null
            ? controlledMember.transform.parent
            : controlledMember.transform;

        // Busca componentes
        SaudePokemon saude = rootTransform.GetComponentInChildren<SaudePokemon>();
        TargetableEntity entity = rootTransform.GetComponentInChildren<TargetableEntity>();

        if (saude != null)
        {
            playerHUD.SetPokemon(saude, entity);
        }
        else
        {
            playerHUD.SetVisible(false);
        }
    }

    #endregion

    #region Getters

    public RoleHandler GetControlledMember() => controlledMember;
    public RoleHandler GetTrainerLeader() => trainerLeader;
    public List<RoleHandler> GetTeamMembers() => new List<RoleHandler>(teamMembers);
    public List<RoleHandler> GetPCStorage() => new List<RoleHandler>(pcStorage);
    public bool IsTeamFull() => teamMembers.Count >= MAX_TEAM_SIZE;

    /// <summary>
    /// Verifica se o membro controlado é um pokémon (não o líder).
    /// </summary>
    public bool IsControllingPokemon() => controlledMember != null && controlledMember != trainerLeader;

    #endregion
}