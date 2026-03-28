using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PokemonRole
{
    Wild,
    PlayerControlled,
    AllyAI,
    EnemyAI
}

public class RoleHandler : MonoBehaviour
{
    [Header("Current Role")]
    public PokemonRole currentRole = PokemonRole.Wild;

    [Header("Manual Testing Controls")]
    [Space(10)]
    [SerializeField] private bool setToWild = false;
    [SerializeField] private bool setToPlayerControlled = false;
    [SerializeField] private bool setToAllyAI = false;
    [SerializeField] private bool setToEnemyAI = false;

    [Header("Controller References")]
    public GameObject inputController;    // Componente com scripts de controle pelo jogador
    public GameObject aiController;       // Componente com scripts de IA
    public GameObject hudElements;        // HUD de vida, mira, etc. Visível só se for controlado
    public GameObject targetableOverlay;  // Sobreposição de mira (visível se for mirável)

    [Header("Optional")]
    public Animator animator;
    public string playerIdleAnim = "Idle_Player";
    public string wildIdleAnim = "Idle_Wild";

    // Referências necessárias
    public Mon mon;
    public SaudePokemon saudePokemon;
    public TargetableEntity targetableEntity;

    // Eventos
    public System.Action<RoleHandler> OnRoleChanged;

    // Controle para evitar loops infinitos
    private bool isApplyingRole = false;

    private void Awake()
    {
        
    }

    private void Start()
    {
        ApplyRole(currentRole);
    }

    private void Update()
    {
        // Controles manuais para teste no Inspector
        HandleManualControls();
    }

    private void HandleManualControls()
    {
        if (setToWild)
        {
            setToWild = false;
            ChangeRoleManually(PokemonRole.Wild);
        }
        else if (setToPlayerControlled)
        {
            setToPlayerControlled = false;
            ChangeRoleManually(PokemonRole.PlayerControlled);
        }
        else if (setToAllyAI)
        {
            setToAllyAI = false;
            ChangeRoleManually(PokemonRole.AllyAI);
        }
        else if (setToEnemyAI)
        {
            setToEnemyAI = false;
            ChangeRoleManually(PokemonRole.EnemyAI);
        }
    }

    /// <summary>
    /// Método para mudança manual de papel (usado pelos botões do Inspector)
    /// </summary>
    /// <param name="newRole">Novo papel desejado</param>
    public void ChangeRoleManually(PokemonRole newRole)
    {
        if (currentRole == newRole || isApplyingRole) return;

        Debug.Log($"[{gameObject.name}] Mudando papel manualmente: {currentRole} → {newRole}");
        ApplyRole(newRole);
    }

    /// <summary>
    /// Define o papel atual do Pokémon e ativa/desativa componentes conforme necessário.
    /// </summary>
    /// <param name="newRole">Novo papel a ser assumido</param>
    public void ApplyRole(PokemonRole newRole)
    {
        // Evita loops infinitos
        if (isApplyingRole)
        {
            // Debug.LogWarning($"[{gameObject.name}] ApplyRole chamado durante aplicação. Ignorando.");
            return;
        }

        isApplyingRole = true;
        PokemonRole previousRole = currentRole;
        currentRole = newRole;

        // Debug.Log($"[{gameObject.name}] Aplicando papel: {previousRole} → {newRole}");

        switch (newRole)
        {
            case PokemonRole.PlayerControlled:
                EnableInput(true);
                EnableAI(false);
                EnableHUD(true);
                EnableTargetable(false); // Jogador não deve ser mirável por si mesmo
                break;

            case PokemonRole.Wild:
                EnableInput(false);
                EnableAI(true);
                EnableHUD(false);
                EnableTargetable(true);
                break;

            case PokemonRole.AllyAI:
                EnableInput(false);
                EnableAI(true);
                EnableHUD(false);
                EnableTargetable(true); // <-- permite selecionar pokémon do time
                break;

            case PokemonRole.EnemyAI:
                EnableInput(false);
                EnableAI(true);
                EnableHUD(false);
                EnableTargetable(true);
                break;
        }

        // Notifica sobre mudança de papel
        OnRoleChanged?.Invoke(this);

        isApplyingRole = false;
    }

    // Métodos auxiliares existentes...
    private void EnableInput(bool enable)
    {
        if (inputController != null)
        {
            inputController.SetActive(enable);
            //Debug.Log($"[{gameObject.name}] Input Controller: {(enable ? "ATIVADO" : "DESATIVADO")}");
        }
    }

    private void EnableAI(bool enable)
    {
        if (aiController != null)
        {
            aiController.SetActive(enable);

            // CORREÇÃO: Reinicia a IA quando reativada
            if (enable)
            {
                var wildAI = aiController.GetComponent<WildPokemonAI>();
                if (wildAI != null)
                {
                    wildAI.RestartAI();
                }
            }

            //Debug.Log($"[{gameObject.name}] AI Controller: {(enable ? "ATIVADO" : "DESATIVADO")}");
        }
    }

    private void EnableHUD(bool enable)
    {
        if (hudElements != null)
        {
            hudElements.SetActive(enable);
            //Debug.Log($"[{gameObject.name}] HUD Elements: {(enable ? "ATIVADO" : "DESATIVADO")}");
        }
    }

    private void EnableTargetable(bool enable)
    {
        if (targetableOverlay != null)
        {
            targetableOverlay.SetActive(enable);
            //Debug.Log($"[{gameObject.name}] Targetable Overlay: {(enable ? "ATIVADO" : "DESATIVADO")}");
        }

        // Atualiza o TargetableEntity também
        if (targetableEntity != null)
        {
            targetableEntity.isTargetable = enable;
            //Debug.Log($"[{gameObject.name}] Targetable Entity: {(enable ? "ATIVADO" : "DESATIVADO")}");
        }
    }

    // Getters públicos
    public Mon GetMon() => mon;
    public SaudePokemon GetSaudePokemon() => saudePokemon;
    public PokemonRole GetCurrentRole() => currentRole;
    public bool IsPlayerControlled() => currentRole == PokemonRole.PlayerControlled;

}