using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfoUI : MonoBehaviour
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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetTarget(SaudePokemon target)
    {
        //DisconnectFromTargetEvents();
        ClearStatusIcons();

        //currentTarget = target;

        if (target != null)
        {
           // ConnectToTargetEvents();
            //RefreshUI();

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
    private void SetupStatusEffects()
    {
        if (currentTarget == null) return;

        if (statusPanel != null)
            statusPanel.SetActive(true);
    }
    private void ConnectToTargetEvents()
    {
        if (currentTarget == null) return;

        var saudePokemon = currentTarget.GetSaudePokemon();
        if (saudePokemon != null)
        {
            //saudePokemon.OnHealthChanged += OnTargetHealthChanged;
            //saudePokemon.OnPowerChanged += OnTargetPowerChanged;
            lastSaudePokemon = saudePokemon;
        }
    }
    private void DisconnectFromTargetEvents()
    {
        if (lastSaudePokemon != null)
        {
            //lastSaudePokemon.OnHealthChanged -= OnTargetHealthChanged;
            //lastSaudePokemon.OnPowerChanged -= OnTargetPowerChanged;
           //lastSaudePokemon = null;
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
    public void SetVisible(bool visible)
    {
        painel.SetActive(visible);

        if (!visible)
        {
            //DisconnectFromTargetEvents();
            currentTarget = null;
            ClearStatusIcons();
        }
    }

}
