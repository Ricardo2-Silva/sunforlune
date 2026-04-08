using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NovoAtaque", menuName = "Ataques/DadosAtaque")]
public class DadosAtaque : ScriptableObject
{
    // ─── Identidade ────────────────────────────────────────────────────────────
    [Header("Identidade e Interface")]
    public string nomeAtaque;
    public Sprite icone;
    [TextArea(3, 6)] public string descricao;

    // ─── Parâmetros lógicos essenciais ─────────────────────────────────────────
    [Header("Parâmetros Lógicos Essenciais")]
    public int pontosPoder;
    public float tempoRecarga;
    public float tempoConjuracao;
    public bool ehAtaqueCanalizado;

    [Header("Dados Essenciais de Combate")]
    public float danoBase = 5f;
    public float precisao = 1f;
    public int prioridade = 0;
    public float distanciaIdeal = 1.5f;
    public float alcanceMax = 4f;
    public float raioAoe = 0f;

    [Header("Movimento do Ataque (se aplicável)")]
    public float velocidadeMovimento = 0f;
    public float duracaoMovimento = 0f;

    [Header("Classificação")]
    public Tipo tipo;

    // ─── Lista de ações ────────────────────────────────────────────────────────
    [Header("Sequência de Ações")]
    [SerializeReference]
    public List<AcaoAtaque> acoes = new List<AcaoAtaque>();

    // ─── Execução ──────────────────────────────────────────────────────────────
    public IEnumerator ExecutarRotina(ContextoAtaque ctx)
    {
        if (!ctx.Validar()) yield break;

        foreach (AcaoAtaque acao in acoes)
        {
            if (acao == null || ctx.CancelarSequencia) yield break;

            if (ctx.Executor != null)
                yield return ctx.Executor.StartCoroutine(acao.Executar(ctx));
            else
                yield return ctx.Atacante.StartCoroutine(acao.Executar(ctx));
        }
    }
}