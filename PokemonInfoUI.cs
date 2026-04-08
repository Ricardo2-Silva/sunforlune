using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD para exibir informações de um Pokémon.
/// SIMPLIFICADO: A matemática agora vem estritamente do SaudePokemon.
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

    private float targetHealthFill = 1f;
    private float targetPowerFill = 1f;

    private void Start() => SetVisible(false);

    private void Update()
    {
        if (currentPokemon != null && mainPanel != null && mainPanel.activeSelf)
        {
            AnimateBars();
        }
    }

    public void SetPokemon(SaudePokemon saude, TargetableEntity entity = null)
    {
        UnsubscribeFromEvents();

        currentPokemon = saude;
        currentEntity = entity;
        currentStatusManager = entity?.GetStatusManager() ?? saude?.GetComponent<StatusEffectManager>();

        if (saude == null)
        {
            SetVisible(false);
            return;
        }

        SubscribeToEvents();
        AtualizarDadosCompletos();
        SetVisible(true);
    }

    private void AtualizarDadosCompletos()
    {
        if (currentPokemon == null) return;

        AtualizarNomeEPortrait();
        // Chamada inicial para forçar os valores corretos
        AtualizarSaude(currentPokemon.GetSaudeAtual(), currentPokemon.GetSaudeMaxima());
        AtualizarPoder(currentPokemon.GetPoderAtual(), currentPokemon.GetPoderMaximo());
        AtualizarStatusEffects();
    }

    private void AtualizarNomeEPortrait()
    {
        Mon mon = currentEntity?.GetMon() ?? currentPokemon?.GetMon();

        if (mon != null && mon.Base != null)
        {
            if (nameText != null) nameText.text = $"{mon.Base.Nome} (Lv.{mon.Nivel})";
            if (portrait != null && mon.Base.Portrait != null) portrait.sprite = mon.Base.Portrait;
        }
        else
        {
            if (nameText != null) nameText.text = currentPokemon?.gameObject.name ?? "???";
        }
    }

    private void SubscribeToEvents()
    {
        if (currentPokemon != null)
        {
            currentPokemon.OnHealthChanged += AtualizarSaude;
            currentPokemon.OnPowerChanged += AtualizarPoder;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (currentPokemon != null)
        {
            currentPokemon.OnHealthChanged -= AtualizarSaude;
            currentPokemon.OnPowerChanged -= AtualizarPoder;
        }
    }

    private void AtualizarSaude(float atual, float max)
    {
        // Pede a normalização direto da fonte
        targetHealthFill = currentPokemon.GetSaudeNormalizada();
        if (healthText != null) healthText.text = $"{Mathf.FloorToInt(atual)} / {Mathf.FloorToInt(max)}";
    }

    private void AtualizarPoder(float atual, float max)
    {
        targetPowerFill = currentPokemon.GetPoderNormalizado();
        if (powerText != null) powerText.text = $"{Mathf.RoundToInt(currentPokemon.GetPoderPorcentagem())}%";
    }

    private void AnimateBars()
    {
        if (healthBar != null) healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetHealthFill, lerpSpeed * Time.deltaTime);
        if (powerBar != null) powerBar.fillAmount = Mathf.Lerp(powerBar.fillAmount, targetPowerFill, lerpSpeed * Time.deltaTime);
    }

    private void AtualizarStatusEffects()
    {
        if (statusEffectPanel != null) statusEffectPanel.SetActive(true);
        LimparIconesStatus();

        if (currentStatusManager != null)
        {
            var activeEffects = currentStatusManager.GetActiveEffects();
            if (activeEffects != null)
            {
                foreach (var effect in activeEffects) AdicionarIconeStatus(effect);
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
            if (icon != null && icon.gameObject != null) Destroy(icon.gameObject);
        }
        activeStatusIcons.Clear();
    }

    public void SetVisible(bool visible) { if (mainPanel != null) mainPanel.SetActive(visible); }
    private void OnDestroy() => UnsubscribeFromEvents();
}