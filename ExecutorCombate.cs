using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável por executar ataques do Pokémon.
/// Substitui PerformCombat — usa ClasseAtaque + DadosAtaque (modular).
/// Não quebra scripts existentes que ainda usem PerformCombat.
/// </summary>
public class ExecutorCombate : MonoBehaviour
{
    // ─── Referências ────────────────────────────────────────────────────
    [Header("Referências")]
    [SerializeField] private Mon mon;

    [Header("Cooldown Global")]
    public float cooldownGlobal = 1.0f;
    private float _ultimoUsoGlobal = -Mathf.Infinity;

    [Header("Estado")]
    public bool bloquearInput = false;

    // ─── Internos ────────────────────────────────────────────────────────
    public SaudePokemon saudePokemon;
    public MonHurtBox hurtBox;
    private ClasseAtaque _ataqueCanalizando;
    public ClasseAtaque UltimoAtaque { get; private set; }

    // ════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (mon == null) mon = GetComponentInParent<Mon>() ?? GetComponent<Mon>();
        if (saudePokemon == null) saudePokemon = GetComponentInParent<SaudePokemon>() ?? GetComponent<SaudePokemon>();
    }

    private void Start()
    {
        if (hurtBox != null)
        {
            hurtBox.OnTookDamage += AoReceberDano;
            hurtBox.OnRecoveredFromHurt += AoRecuperarDano;
        }
    }

    private void OnDestroy()
    {
        // Limpeza de ataques ativos
    }

    private void AoReceberDano()
    {
        if (_ataqueCanalizando != null) CancelarAtaque(_ataqueCanalizando);
        bloquearInput = true;
    }

    private void AoRecuperarDano() => bloquearInput = false;

    public void ExecutarAtaqueManual(ClasseAtaque ataque, Vector2 direcao, Transform alvo = null)
    {
        if (bloquearInput) return;
        if (!CooldownGlobalDisponivel()) return;
        if (!ataque.EstaDisponivel()) return;
        if (ataque.dados == null) return;

        float custoPP = ataque.dados.pontosPoder;
        if (saudePokemon != null && custoPP > 0 && !saudePokemon.ConsumirPontosPoder(custoPP))
            return;

        UltimoAtaque = ataque;

        if (ataque.EhCanalizado)
            StartCoroutine(RotinaAtaqueCanalizado(ataque, direcao, alvo));
        else
            ExecutarAtaqueImediato(ataque, direcao, alvo);
    }

    public void ExecutarAtaqueAleatorio(Transform alvo)
    {
        var ataques = mon?.Attacks;
        if (ataques == null || ataques.Count == 0) return;
        if (!CooldownGlobalDisponivel()) return;

        var disponiveis = new List<ClasseAtaque>();
        foreach (var atk in ataques)
        {
            //float pp = atk.dados != null ? atk.dados.pontosPoder : 0f;
            //if (atk.EstaDisponivel() && (saudePokemon == null || saudePokemon.TemPontosPoderPara(pp)))
               // disponiveis.Add(atk);
        }

        if (disponiveis.Count == 0) return;

        var escolhido = disponiveis[Random.Range(0, disponiveis.Count)];
        float custo = escolhido.dados?.pontosPoder ?? 0f;
        if (saudePokemon != null && !saudePokemon.ConsumirPontosPoder(custo)) return;

        UltimoAtaque = escolhido;
        Vector2 dir = ((Vector2)(alvo.position - transform.position)).normalized;
        ExecutarAtaqueImediato(escolhido, dir, alvo);
    }

    public void CancelarAtaque(ClasseAtaque ataque)
    {
        if (ataque == null) return;

        if (ataque.RotinaAtual != null)
        {
            StopCoroutine(ataque.RotinaAtual);
            ataque.RotinaAtual = null;
        }

        if (ataque.Instancia.rotinaCAnalizacao != null)
        {
            StopCoroutine(ataque.Instancia.rotinaCAnalizacao);
            ataque.Instancia.rotinaCAnalizacao = null;
        }

        ataque.PararCAnalizacao();
        ataque.AtivarCooldown();
        AtivarCooldownGlobal();

        if (_ataqueCanalizando == ataque) _ataqueCanalizando = null;

        if (CastingUIManager.Instance != null) CastingUIManager.Instance.HideCastBar();
    }

    private void ExecutarAtaqueImediato(ClasseAtaque ataque, Vector2 direcao, Transform alvo)
    {
        var ctx = CriarContexto(ataque.dados, direcao, alvo);
        var rotina = StartCoroutine(ataque.dados.ExecutarRotina(ctx));
        ataque.RotinaAtual = rotina;

        ataque.AtivarCooldown();
        AtivarCooldownGlobal();
    }

    private IEnumerator RotinaAtaqueCanalizado(ClasseAtaque ataque, Vector2 direcao, Transform alvo)
    {
        ataque.IniciarCAnalizacao();
        _ataqueCanalizando = ataque;

        if (CastingUIManager.Instance != null)
            CastingUIManager.Instance.ShowCastBar(
                ataque.dados.nomeAtaque, ataque.dados.tempoConjuracao);

        var ctx = CriarContexto(ataque.dados, direcao, alvo);
        var rotina = StartCoroutine(ataque.dados.ExecutarRotina(ctx));
        ataque.Instancia.rotinaCAnalizacao = rotina;
        ataque.RotinaAtual = rotina;

        float t = 0f;
        while (ataque.EstaCanalizando && t < ataque.dados.tempoConjuracao)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (ataque.EstaCanalizando)
        {
            ataque.PararCAnalizacao();
            ataque.AtivarCooldown();
            AtivarCooldownGlobal();
            if (CastingUIManager.Instance != null) CastingUIManager.Instance.HideCastBar();
        }

        _ataqueCanalizando = null;
        ataque.RotinaAtual = null;
    }

    private ContextoAtaque CriarContexto(DadosAtaque dados, Vector2 direcao, Transform alvo)
    {
        Transform raiz = mon != null ? mon.transform : transform;
        return new ContextoAtaque(raiz, dados, direcao, alvo, this);
    }

    public bool CooldownGlobalDisponivel() =>
        Time.time >= _ultimoUsoGlobal + cooldownGlobal;

    private void AtivarCooldownGlobal() =>
        _ultimoUsoGlobal = Time.time;

    public float UltimoUsoGlobal => _ultimoUsoGlobal;

    //public List<ClasseAtaque> ObterAtaques() => mon != null ? mon.Attacks : null;
}