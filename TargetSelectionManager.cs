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

    public System.Action<TargetableEntity> OnTargetSelected;
    public System.Action OnTargetDeselected;

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
            Transform root = controlled.transform.parent != null
                ? controlled.transform.parent
                : controlled.transform;
            playerTransform = root;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(deselectKey))
        {
            DeselectTarget();
        }

        if (Input.GetKeyDown(nextTargetKey))
        {
            SelectNextTarget();
        }
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
        {
            allTargets.Add(target);
        }
    }

    public void UnregisterTarget(TargetableEntity target)
    {
        allTargets.Remove(target);

        if (currentSelectedTarget == target)
        {
            DeselectTarget();
        }
    }

    public void SelectTarget(TargetableEntity target, bool isAutoSelection = false)
    {
        if (target == null || !target.isTargetable) return;

        if (isAutoSelection && currentSelectedTarget != null) return;

        bool altPressionado = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (!altPressionado && IsSelfTarget(target)) return;

        if (currentSelectedTarget != null)
        {
            currentSelectedTarget.SetSelectedState(false);
        }

        currentSelectedTarget = target;
        target.SetSelectedState(true);

        if (targetHUD != null)
        {
            targetHUD.SetPokemon(target.GetSaudePokemon(), target);
        }

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
        {
            targetHUD.SetVisible(false);
        }

        OnTargetDeselected?.Invoke();
    }

    public void SelectNextTarget()
    {
        if (playerTransform == null) return;

        var validTargets = allTargets
            .Where(t => t != null && t.isTargetable && t.IsAlive() && t.isInCombatWithPlayer)
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
        if (PokemonSwitchManager.Instance == null) return false;

        var controlled = PokemonSwitchManager.Instance.GetControlledMember();
        if (controlled == null) return false;

        if (target.roleHandler != null && target.roleHandler == controlled) return true;

        Transform controlledRoot = controlled.transform.parent != null
            ? controlled.transform.parent
            : controlled.transform;

        TargetableEntity controlledEntity = controlledRoot.GetComponentInChildren<TargetableEntity>();
        if (controlledEntity != null && controlledEntity == target) return true;

        return false;
    }

    #endregion

    #region Callbacks de Hover
    public void OnTargetHovered(TargetableEntity target)
    {
        currentHoveredTarget = target;
    }

    public void OnTargetUnhovered(TargetableEntity target)
    {
        if (currentHoveredTarget == target)
        {
            currentHoveredTarget = null;
        }
    }
    #endregion

    #region UI Updates
    public void RefreshSelectedTargetUI()
    {
        if (targetHUD != null && currentSelectedTarget != null)
        {
            targetHUD.SetPokemon(currentSelectedTarget.GetSaudePokemon(), currentSelectedTarget);
        }
    }
    #endregion

    #region Getters
    public TargetableEntity GetSelectedTarget() => currentSelectedTarget;
    public TargetableEntity GetHoveredTarget() => currentHoveredTarget;
    public bool HasSelectedTarget() => currentSelectedTarget != null;
    public List<TargetableEntity> GetAllTargets() => new List<TargetableEntity>(allTargets);
    public List<TargetableEntity> GetTargetsInRange(Vector3 position, float range)
    {
        return allTargets
            .Where(t => t != null && t.isTargetable && t.IsAlive())
            .Where(t => t.GetDistanceFrom(position) <= range)
            .OrderBy(t => t.GetDistanceFrom(position))
            .ToList();
    }
    #endregion
}