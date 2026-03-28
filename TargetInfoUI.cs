using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controla a UI que mostra informações do alvo selecionado.
/// Integra com o sistema de status effects existente.
/// </summary>
public class TargetInfoUI : MonoBehaviour
{
    [Header("Barra de Vida")]
    public Image healthBar;             // Barra de vida (fillAmount)
    public TextMeshProUGUI healthText;  // Texto com valores de vida

    [Header("Barra de Poder")]
    public Image powerBar;              // Barra de poder (fillAmount)
    public TextMeshProUGUI powerText;   // Texto com porcentagem de poder

    [Header("Nome e Nível")]
    public Image portrait;
    public TextMeshProUGUI nameText;    // Nome e nível

    [Header("Status Effects Panel")]
    public GameObject statusPanel;      // Painel que contém os status effects
    public Transform statusContainer;   // Container dos ícones
    public GameObject statusIconPrefab; // Prefab do ícone

    [Header("Configurações")]
    public float updateInterval = 0.1f; // Frequência de atualização da UI

    // Estado atual
    public GameObject painel;
    public TargetableEntity currentTarget;
    private Dictionary<StatusEffectType, StatusEffectIcon> statusIcons = new Dictionary<StatusEffectType, StatusEffectIcon>();
    public float lastUpdateTime;

    // Animação das barras
    public float lerpSpeed = 3f;
    private float targetHealthFill = 1f;
    private float targetPowerFill = 1f;

    // Eventos conectados
    private SaudePokemon lastSaudePokemon;

    private void Start()
    {
        SetVisible(false);
    }

    private void Update()
    {
        // Atualiza UI periodicamente (em caso de falha de eventos)
        if (currentTarget != null && Time.time - lastUpdateTime >= updateInterval)
        {
            RefreshUI();
            lastUpdateTime = Time.time;
        }

        AnimateBars();
    }

    #region Controle Principal
    public void SetTarget(TargetableEntity target)
    {
        DisconnectFromTargetEvents();
        ClearStatusIcons();

        currentTarget = target;

        if (target != null)
        {
            ConnectToTargetEvents();
            RefreshUI();

            // Inicializa barras para o novo alvo
            if (healthBar != null)
                healthBar.fillAmount = target.GetHealthPercentage();
            if (powerBar != null)
                powerBar.fillAmount = target.GetPowerPercentage();
            if (target.GetMon().Base.Portrait != null)
            {
                portrait.sprite = target.GetMon().Base.Portrait;
            }

            targetHealthFill = healthBar != null ? healthBar.fillAmount : 1f;
            targetPowerFill = powerBar != null ? powerBar.fillAmount : 1f;

            SetupStatusEffects();
        }
    }

    public void SetVisible(bool visible)
    {
        painel.SetActive(visible);

        if (!visible)
        {
            DisconnectFromTargetEvents();
            currentTarget = null;
            ClearStatusIcons();
        }
    }

    public void RefreshUI()
    {
        if (currentTarget == null) return;

        UpdateHealthInfo();
        UpdatePowerInfo();
        UpdateNameInfo();
        UpdateStatusEffects();
    }
    #endregion

    #region Atualização de Informações
    private void UpdateHealthInfo()
    {
        var saudePokemon = currentTarget?.GetSaudePokemon();
        if (saudePokemon == null || healthBar == null || healthText == null) return;

        float atual = saudePokemon.GetSaudeAtual();
        float max = saudePokemon.GetSaudeMaxima();
        targetHealthFill = max > 0 ? atual / max : 0f;
        healthText.text = $"{Mathf.FloorToInt(atual)} / {Mathf.FloorToInt(max)}";
    }

    private void UpdatePowerInfo()
    {
        var saudePokemon = currentTarget?.GetSaudePokemon();
        if (saudePokemon == null || powerBar == null || powerText == null) return;

        float atual = saudePokemon.GetPoderAtual();
        float max = saudePokemon.GetPoderMaximo();
        targetPowerFill = max > 0 ? atual / max : 0f;
        float percentage = targetPowerFill * 100f;
        powerText.text = $"{Mathf.RoundToInt(percentage)}%";
    }

    private void UpdateNameInfo()
    {
        if (nameText != null && currentTarget != null)
            nameText.text = currentTarget.GetDisplayName();
    }

    private void AnimateBars()
    {
        if (healthBar != null)
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetHealthFill, lerpSpeed * Time.deltaTime);

        if (powerBar != null)
            powerBar.fillAmount = Mathf.Lerp(powerBar.fillAmount, targetPowerFill, lerpSpeed * Time.deltaTime);
    }
    #endregion

    #region Sistema de Status Effects
    private void SetupStatusEffects()
    {
        if (currentTarget == null) return;

        if (statusPanel != null)
            statusPanel.SetActive(true);
    }

    private void UpdateStatusEffects()
    {
        // Exemplo: depende da implementação do seu StatusEffectManager
        // var statusManager = currentTarget.GetStatusManager();
        // if (statusManager == null) return;
        // var activeEffects = statusManager.GetActiveEffects();
        // // Atualize os ícones conforme necessário...
    }

    private void UpdateOrCreateStatusIcon(StatusEffectInstance effect)
    {
        var effectType = effect.effectData.effectType;
        if (statusIcons.TryGetValue(effectType, out var existingIcon))
        {
            existingIcon.SetDuration(effect.remainingDuration);
            existingIcon.SetStacks(effect.currentStacks);
        }
        else
        {
            CreateStatusIcon(effect);
        }
    }

    private void CreateStatusIcon(StatusEffectInstance effect)
    {
        if (statusIconPrefab == null || statusContainer == null) return;

        var iconObj = Instantiate(statusIconPrefab, statusContainer);
        var iconComponent = iconObj.GetComponent<StatusEffectIcon>();

        if (iconComponent != null)
        {
            iconComponent.SetIcon(effect.effectData.icon);
            iconComponent.SetDuration(effect.remainingDuration);
            iconComponent.SetStacks(effect.currentStacks);

            statusIcons[effect.effectData.effectType] = iconComponent;
        }
    }

    private void RemoveStatusIcon(StatusEffectType effectType)
    {
        if (statusIcons.TryGetValue(effectType, out var icon))
        {
            if (icon != null && icon.gameObject != null)
                Destroy(icon.gameObject);
            statusIcons.Remove(effectType);
        }
    }

    private void ClearStatusIcons()
    {
        foreach (var icon in statusIcons.Values)
        {
            if (icon != null && icon.gameObject != null)
                Destroy(icon.gameObject);
        }
        statusIcons.Clear();
    }
    #endregion

    #region Eventos de Callback
    // Garante que a UI responde a eventos de vida/poder do alvo
    private void OnTargetHealthChanged(float currentHealth, float maxHealth)
    {
        targetHealthFill = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        if (healthText != null)
            healthText.text = $"{Mathf.FloorToInt(currentHealth)} / {Mathf.FloorToInt(maxHealth)}";
    }

    private void OnTargetPowerChanged(float currentPower, float maxPower)
    {
        targetPowerFill = maxPower > 0 ? currentPower / maxPower : 0f;
        if (powerText != null)
        {
            float percentage = targetPowerFill * 100f;
            powerText.text = $"{Mathf.RoundToInt(percentage)}%";
        }
    }

    private void ConnectToTargetEvents()
    {
        if (currentTarget == null) return;

        var saudePokemon = currentTarget.GetSaudePokemon();
        if (saudePokemon != null)
        {
            saudePokemon.OnHealthChanged += OnTargetHealthChanged;
            saudePokemon.OnPowerChanged += OnTargetPowerChanged;
            lastSaudePokemon = saudePokemon;
        }
    }

    private void DisconnectFromTargetEvents()
    {
        if (lastSaudePokemon != null)
        {
            lastSaudePokemon.OnHealthChanged -= OnTargetHealthChanged;
            lastSaudePokemon.OnPowerChanged -= OnTargetPowerChanged;
            lastSaudePokemon = null;
        }
    }
    #endregion
}