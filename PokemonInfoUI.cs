using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD para exibir informações de um Pokémon (jogador ou alvo).
/// SIMPLIFICADO: Removidas redundâncias, fluxo claro.
/// </summary>
public class PokemonInfoUI : MonoBehaviour
{
    [Header("Referências Atuais")]
    [SerializeField] private SaudePokemon currentPokemon;
    [SerializeField] private TargetableEntity currentEntity;
    [SerializeField] private StatusEffectManager currentStatusManager;

    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject statusEffectPanel;

    [Header("Barras e Textos")]
    public Image healthBar;
    public TextMeshProUGUI healthText;
    public Image powerBar;
    public TextMeshProUGUI powerText;
    public Image portrait;
    public TextMeshProUGUI nameText;

    [Header("Status Effects")]
    public Transform statusContainer;
    public GameObject statusIconPrefab;
    private Dictionary<StatusEffectType, StatusEffectIcon> activeStatusIcons = new Dictionary<StatusEffectType, StatusEffectIcon>();

    [Header("Configuração")]
    public float lerpSpeed = 5f;

    // Animação das barras
    private float targetHealthFill = 1f;
    private float targetPowerFill = 1f;

    private void Start()
    {
        SetVisible(false);
    }

    private void Update()
    {
        if (currentPokemon != null && mainPanel != null && mainPanel.activeSelf)
        {
            AnimateBars();
        }
    }

    /// <summary>
    /// Configura o HUD para exibir um Pokémon.
    /// </summary>
    public void SetPokemon(SaudePokemon saude, TargetableEntity entity = null)
    {
        // Desregistra eventos anteriores
        UnsubscribeFromEvents();

        // Atualiza referências
        currentPokemon = saude;
        currentEntity = entity;
        currentStatusManager = entity?.GetStatusManager() ?? saude?.GetComponent<StatusEffectManager>();

        if (saude == null)
        {
            SetVisible(false);
            return;
        }

        // Registra novos eventos
        SubscribeToEvents();

        // Atualiza todos os dados
        AtualizarDadosCompletos();

        SetVisible(true);
    }

    /// <summary>
    /// Atualiza nome, portrait, saúde, poder e status de uma vez.
    /// </summary>
    private void AtualizarDadosCompletos()
    {
        if (currentPokemon == null) return;

        AtualizarNomeEPortrait();
        AtualizarSaude(currentPokemon.GetSaudeAtual(), currentPokemon.GetSaudeMaxima());
        AtualizarPoder(currentPokemon.GetPoderAtual(), currentPokemon.GetPoderMaximo());
        AtualizarStatusEffects();
    }

    private void AtualizarNomeEPortrait()
    {
        Mon mon = null;

        // Tenta obter Mon do entity primeiro, depois do SaudePokemon
        if (currentEntity != null)
        {
            mon = currentEntity.GetMon();
        }

        if (mon == null && currentPokemon != null)
        {
            mon = currentPokemon.GetMon();
        }

        if (mon != null && mon.Base != null)
        {
            if (nameText != null)
                nameText.text = $"{mon.Base.Nome} (Lv.{mon.Nivel})";

            if (portrait != null && mon.Base.Portrait != null)
                portrait.sprite = mon.Base.Portrait;
        }
        else
        {
            if (nameText != null)
                nameText.text = currentPokemon?.gameObject.name ?? "??? ";
        }
    }

    #region Eventos

    private void SubscribeToEvents()
    {
        if (currentPokemon != null)
        {
            currentPokemon.OnHealthChanged += AtualizarSaude;
            currentPokemon.OnPowerChanged += AtualizarPoder;
        }

        if (currentEntity != null)
        {
            currentEntity.OnStatusEffect += OnStatusEffectChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (currentPokemon != null)
        {
            currentPokemon.OnHealthChanged -= AtualizarSaude;
            currentPokemon.OnPowerChanged -= AtualizarPoder;
        }

        if (currentEntity != null)
        {
            currentEntity.OnStatusEffect -= OnStatusEffectChanged;
        }
    }

    private void OnStatusEffectChanged(TargetableEntity entity)
    {
        AtualizarStatusEffects();
    }

    #endregion

    #region Atualizações de UI

    private void AtualizarSaude(float atual, float max)
    {
        targetHealthFill = max > 0 ? atual / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.FloorToInt(atual)} / {Mathf.FloorToInt(max)}";
    }

    private void AtualizarPoder(float atual, float max)
    {
        targetPowerFill = max > 0 ? atual / max : 0f;

        if (powerText != null)
            powerText.text = $"{Mathf.RoundToInt((atual / max) * 100)}%";
    }

    private void AnimateBars()
    {
        if (healthBar != null)
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetHealthFill, lerpSpeed * Time.deltaTime);

        if (powerBar != null)
            powerBar.fillAmount = Mathf.Lerp(powerBar.fillAmount, targetPowerFill, lerpSpeed * Time.deltaTime);
    }

    private void AtualizarStatusEffects()
    {
        if (statusEffectPanel != null)
            statusEffectPanel.SetActive(true);

        LimparIconesStatus();

        if (currentStatusManager != null)
        {
            var activeEffects = currentStatusManager.GetActiveEffects();
            if (activeEffects != null)
            {
                foreach (var effect in activeEffects)
                {
                    AdicionarIconeStatus(effect);
                }
            }
        }
    }

    private void AdicionarIconeStatus(StatusEffectInstance effect)
    {
        if (statusIconPrefab == null || statusContainer == null) return;

        var type = effect.effectData.effectType;
        if (!activeStatusIcons.ContainsKey(type))
        {
            var newIcon = Instantiate(statusIconPrefab, statusContainer).GetComponent<StatusEffectIcon>();
            if (newIcon != null)
            {
                newIcon.SetIcon(effect.effectData.icon);
                newIcon.SetDuration(effect.remainingDuration);
                newIcon.SetStacks(effect.currentStacks);
                activeStatusIcons[type] = newIcon;
            }
        }
    }

    private void LimparIconesStatus()
    {
        foreach (var icon in activeStatusIcons.Values)
        {
            if (icon != null && icon.gameObject != null)
                Destroy(icon.gameObject);
        }
        activeStatusIcons.Clear();
    }

    #endregion

    #region Visibilidade

    public void SetVisible(bool visible)
    {
        if (mainPanel != null)
            mainPanel.SetActive(visible);
    }

    public bool IsVisible()
    {
        return mainPanel != null && mainPanel.activeSelf;
    }

    #endregion

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}