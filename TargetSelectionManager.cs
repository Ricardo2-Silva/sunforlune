using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gerenciador central do sistema de seleção de alvos.
/// </summary>
public class TargetSelectionManager : MonoBehaviour
{
    public static TargetSelectionManager Instance { get; private set; }

    [Header("Configurações")]
    public KeyCode deselectKey = KeyCode.Escape;
    public KeyCode nextTargetKey = KeyCode.Tab;
    public float maxTabDistance = 10f;
    public float maxDeselectionDistance = 20f;

    [Header("UI References")]
    public PokemonInfoUI targetHUD;

    [Header("Estado Atual")]
    [SerializeField] private TargetableEntity currentSelectedTarget;
    [SerializeField] private TargetableEntity currentHoveredTarget;

    private List<TargetableEntity> allTargets = new List<TargetableEntity>();

    public Transform playerTransform;

    public Action<TargetableEntity> OnTargetSelected;
    public Action OnTargetDeselected;

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

    private void Update()
    {
        HandleInput();
        CheckAutoDeselectByDistance();
        AtualizarPlayerTransform();
    }

    private void AtualizarPlayerTransform()
    {
        if (PokemonSwitchManager.Instance == null) return;

        var controlled = PokemonSwitchManager.Instance.GetControlledMember();
        if (controlled != null)
        {
            Transform root = controlled.transform.parent != null ? controlled.transform.parent : controlled.transform;
            playerTransform = root;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(deselectKey)) DeselectTarget();
        if (Input.GetKeyDown(nextTargetKey)) SelectNextTarget();
    }

    private void CheckAutoDeselectByDistance()
    {
        if (currentSelectedTarget != null && playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentSelectedTarget.transform.position);
            if (distance > maxDeselectionDistance)
            {
                DeselectTarget();
            }
        }
    }

    #region Gerenciamento de Alvos
    public void RegisterTarget(TargetableEntity target)
    {
        if (target != null && !allTargets.Contains(target))
            allTargets.Add(target);
    }

    public void UnregisterTarget(TargetableEntity target)
    {
        allTargets.Remove(target);
        if (currentSelectedTarget == target) DeselectTarget();
        if (currentHoveredTarget == target) currentHoveredTarget = null;
    }

    public void SelectTarget(TargetableEntity target, bool isAutoSelection = false)
    {
        if (target == null || !target.isTargetable) return;
        if (isAutoSelection && currentSelectedTarget != null) return;
        if (currentSelectedTarget == target) return; // Previne piscar a UI se já estiver selecionado

        bool altPressionado = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (!altPressionado && IsSelfTarget(target)) return;

        if (currentSelectedTarget != null)
            currentSelectedTarget.SetSelectedState(false);

        currentSelectedTarget = target;
        target.SetSelectedState(true);

        // Envia os dados iniciais do alvo para o HUD (as atualizações dinâmicas de HP ocorrerão via eventos)
        if (targetHUD != null)
            targetHUD.SetPokemon(target.GetSaudePokemon(), target);

        if (currentHoveredTarget == target)
            currentHoveredTarget = null;

        OnTargetSelected?.Invoke(target);
    }

    public void AutoSelectTarget(TargetableEntity target)
    {
        SelectTarget(target, isAutoSelection: true);
    }

    public void DeselectTarget()
    {
        if (currentSelectedTarget != null)
        {
            currentSelectedTarget.SetSelectedState(false);
            currentSelectedTarget = null;
        }

        if (targetHUD != null)
            targetHUD.SetVisible(false); // Supondo que você crie um método SetVisible no seu HUD para esconder.

        OnTargetDeselected?.Invoke();
    }

    public void SelectNextTarget()
    {
        if (playerTransform == null) return;

        var validTargets = allTargets
            .Where(t => t != null && t.isTargetable && t.isInCombatWithPlayer)
            .Where(t => !IsSelfTarget(t))
            .Where(t => t.GetDistanceFrom(playerTransform.position) <= maxTabDistance)
            .OrderBy(t => t.GetDistanceFrom(playerTransform.position))
            .ToList();

        if (validTargets.Count == 0) return;

        if (currentSelectedTarget == null)
        {
            SelectTarget(validTargets[0]);
            return;
        }

        int currentIndex = validTargets.IndexOf(currentSelectedTarget);
        if (currentIndex >= 0)
        {
            int nextIndex = (currentIndex + 1) % validTargets.Count;
            SelectTarget(validTargets[nextIndex]);
        }
        else
        {
            SelectTarget(validTargets[0]);
        }
    }
    #endregion

    #region Self-Target Check
    private bool IsSelfTarget(TargetableEntity target)
    {
        if (target == null) return false;

        // Bloqueia apenas quem está sendo explicitamente controlado pelo jogador
        if (target.roleHandler != null && target.roleHandler.GetCurrentRole() == PokemonRole.PlayerControlled)
            return true;

        return false;
    }
    #endregion

    #region Callbacks de Hover 
    public void OnTargetHovered(TargetableEntity target)
    {
        if (target == currentSelectedTarget) return; // Ignora o Hover se já for o selecionado
        currentHoveredTarget = target;
    }

    public void OnTargetUnhovered(TargetableEntity target)
    {
        if (currentHoveredTarget == target)
            currentHoveredTarget = null;
    }
    #endregion

    // ❌ A REGIÃO "#region UI Updates" E O MÉTODO RefreshSelectedTargetUI FORAM DELETADOS

    #region Getters
    public TargetableEntity GetSelectedTarget() => currentSelectedTarget;
    public TargetableEntity GetHoveredTarget() => currentHoveredTarget;
    public bool HasSelectedTarget() => currentSelectedTarget != null;
    public List<TargetableEntity> GetAllTargets() => new List<TargetableEntity>(allTargets);

    public List<TargetableEntity> GetTargetsInRange(Vector3 position, float range)
    {
        return allTargets
            .Where(t => t != null && t.isTargetable)
            .Where(t => t.GetDistanceFrom(position) <= range)
            .OrderBy(t => t.GetDistanceFrom(position))
            .ToList();
    }
    #endregion
}