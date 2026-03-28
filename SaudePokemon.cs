using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaudePokemon : MonoBehaviour
{
    [Header("Saúde")]
    public float pontosSaude;
    public float saudeMax;

    [Header("Pontos de Poder (PP)")]
    [Tooltip("PP inicial. Valor padrão: 200.")]
    public float pontosPoder = 200f;
    public float pontosMaxPoder = 200f;
    public float regeneracaoPorSegundo = 20f;
    private Coroutine rotinaRegeneracao;

    [Header("Barras Pequenas Flutuantes")]
    public GameObject barraVidaPequenaPrefab;

    public float velocLerp = 3f;
    public Mon mon;

    // Componentes para integração
    public TargetableEntity targetableEntity;
    //public FloatingHealthBar floatingHealthBar;

    // Eventos para notificações
    public System.Action<float, float> OnHealthChanged;
    public System.Action<float, float> OnPowerChanged;

    void Awake()
    {
        // Criar barra flutuante se necessário
        if (barraVidaPequenaPrefab != null && !IsPlayerCharacter())
        {
            //CreateFloatingHealthBar();
        }
    }

    void Start()
    {
        if (mon != null && (saudeMax == 0 || pontosSaude == 0))
        {
            saudeMax = mon.MaxHp;
            pontosSaude = saudeMax;
        }
        if (pontosMaxPoder <= 0) pontosMaxPoder = pontosPoder > 0 ? pontosPoder : 200f;
        if (pontosPoder <= 0) pontosPoder = pontosMaxPoder;

        OnHealthChanged?.Invoke(pontosSaude, saudeMax);
        OnPowerChanged?.Invoke(pontosPoder, pontosMaxPoder);
    }

    void Update()
    {
        velocLerp = 3f * Time.deltaTime;
    }

    //private void CreateFloatingHealthBar()
    //{
    //    if (barraVidaPequenaPrefab == null) return;

    //    GameObject barraObj = Instantiate(barraVidaPequenaPrefab, transform.position, Quaternion.identity);
    //    //floatingHealthBar = barraObj.GetComponent<FloatingHealthBar>();

    //   // if (floatingHealthBar != null && targetableEntity != null)
    //   // {
    //        //floatingHealthBar.Initialize(targetableEntity);
    //   // }
    //}

    // Determina se este é o personagem do jogador
    private bool IsPlayerCharacter()
    {
        // Você pode usar tags, layers, ou outros métodos para identificar o jogador
        return gameObject.CompareTag("Player") || transform.parent?.CompareTag("Player") == true;
    }

    // Cálculo de dano real - atualizado com notificações
    public void DanoReal(Mon attacker, AttackData move)
    {
        float modifiers = Random.Range(0.85f, 1f);
        float a = (2 * attacker.Nivel + 10) / 250f;
        float d = a * move.damage * ((float)attacker.Attack / (mon.Defense > 0 ? mon.Defense : 1)) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        float oldHealth = pontosSaude;
        pontosSaude -= damage;
        pontosSaude = Mathf.Clamp(pontosSaude, 0, saudeMax);

        // Notificar mudança de saúde
        OnHealthChanged?.Invoke(pontosSaude, saudeMax);

        // Mostrar barra flutuante ao receber dano
        //if (floatingHealthBar != null)
        //{
        //    floatingHealthBar.OnDamageTaken();
        //}

        // Notificar entidade que recebeu dano
        if (targetableEntity != null)
        {
            targetableEntity.OnDamageTaken();
        }

        Debug.Log($"Dano causado: {damage}");
    }
    public void ReceberDano(float dano) // recolocado, mas desnecessário, revisar
    {
        float oldHealth = pontosSaude;
        pontosSaude -= dano;
        pontosSaude = Mathf.Clamp(pontosSaude, 0, saudeMax);

        // Notificar mudança
        OnHealthChanged?.Invoke(pontosSaude, saudeMax);

        // Mostrar barra flutuante
        //if (floatingHealthBar != null)
        //{
        //    floatingHealthBar.OnDamageTaken();
        //}

        // Notificar entidade
        if (targetableEntity != null)
        {
            targetableEntity.OnDamageTaken();
        }
    }
    public void Curar(int curarMontante)
    {
        float oldHealth = pontosSaude;
        pontosSaude += curarMontante;
        pontosSaude = Mathf.Clamp(pontosSaude, 0, saudeMax);

        // Notificar mudança
        OnHealthChanged?.Invoke(pontosSaude, saudeMax);
    }

    public bool ConsumirPontosPoder(float custo)
    {
        if (pontosPoder < custo) return false;

        float oldPower = pontosPoder;
        pontosPoder -= custo;
        pontosPoder = Mathf.Clamp(pontosPoder, 0, pontosMaxPoder);

        // Notificar mudança
        OnPowerChanged?.Invoke(pontosPoder, pontosMaxPoder);

        if (rotinaRegeneracao == null && pontosPoder < pontosMaxPoder)
        {
            rotinaRegeneracao = StartCoroutine(RegenerarPontosPoder());
        }
        return true;
    }

    private IEnumerator RegenerarPontosPoder()
    {
        while (pontosPoder < pontosMaxPoder)
        {
            float oldPower = pontosPoder;
            pontosPoder += regeneracaoPorSegundo * Time.deltaTime;
            pontosPoder = Mathf.Clamp(pontosPoder, 0, pontosMaxPoder);

            // Notificar mudança apenas se houve mudança significativa
            if (Mathf.Abs(pontosPoder - oldPower) > 0.1f)
            {
                OnPowerChanged?.Invoke(pontosPoder, pontosMaxPoder);
            }

            yield return null;
        }
        rotinaRegeneracao = null;
        OnPowerChanged?.Invoke(pontosPoder, pontosMaxPoder);
    }

    // Métodos auxiliares para acesso externo
    public float GetPercentPoder() => pontosPoder / pontosMaxPoder;
    public bool TemPontosPoderPara(float custo) => pontosPoder >= custo;
    public float GetSaudeAtual() => pontosSaude;
    public float GetSaudeMaxima() => saudeMax;
    public float GetPoderAtual() => pontosPoder;
    public float GetPoderMaximo() => pontosMaxPoder;
    public Mon GetMon() => mon;

    public string GetDisplayName() // recolocado, mas desnecessário, revisar
    {
        if (mon != null && mon.Base != null)
            return $"{mon.Base.Nome} (Lv.{mon.Nivel})";
        return gameObject.name;
    }
    public float GetHealthPercentage() // recolocado, mas desnecessário, revisar
    {
        if (saudeMax > 0)
            return pontosSaude / saudeMax;
        return 1f;
    }

    public float GetPowerPercentage() // recolocado, mas desnecessário, revisar
    {
        if (pontosMaxPoder > 0)
            return pontosPoder / pontosMaxPoder;
        return 1f;
    }
}