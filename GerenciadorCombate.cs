using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lê input do jogador e delega a execução ao ExecutorCombate.
/// Substitui CombatManager — não interfere com o script original.
/// </summary>
public class GerenciadorCombate : MonoBehaviour
{
    // ─── Referências ────────────────────────────────────────────────────
    [Header("Dependências")]
    public ExecutorCombate executor;
    public Animator animador;
    public SaudePokemon saudePokemon;

    [Header("Teclas de Ataque")]
    [SerializeField] private KeyCode[] teclasAtaque;

    [Header("Ataques Disponíveis")]
    [SerializeField] private List<ClasseAtaque> ataques = new List<ClasseAtaque>();

    // ─── Estado interno ──────────────────────────────────────────────────
    private Dictionary<KeyCode, bool> _teclaPressionada = new Dictionary<KeyCode, bool>();

    // ════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        foreach (var tecla in teclasAtaque)
            _teclaPressionada[tecla] = false;
    }

    private void Update()
    {
        if (executor == null) { Debug.LogError("[GerenciadorCombate] ExecutorCombate ausente!"); return; }

        for (int i = 0; i < teclasAtaque.Length; i++)
        {
            if (i >= ataques.Count) continue;

            ClasseAtaque ataque = ataques[i];
            if (ataque?.dados == null) continue;

            // Verifica PP antes de qualquer coisa
            float pp = ataque.dados.pontosPoder;
            if (saudePokemon != null && !saudePokemon.TemPontosPoderPara(pp)) continue;

            KeyCode tecla = teclasAtaque[i];

            // ── KeyDown: dispara o ataque ─────────────────────────────────
            if (Input.GetKeyDown(tecla))
            {
                _teclaPressionada[tecla] = true;

                Vector2 direcao = DirecaoParaMouse();
                if (animador != null)
                    GestaoAnimador.Animar(transform.position, animador, "AttackX", "AttackY", true);

                TentarUsarAtaque(i, direcao);
            }
            // ── KeyUp: cancela se for canalizado ──────────────────────────
            else if (Input.GetKeyUp(tecla))
            {
                _teclaPressionada[tecla] = false;

                if (ataque.EhCanalizado && ataque.EstaCanalizando)
                    executor.CancelarAtaque(ataque);
            }
        }
    }

    // ─── API Pública ─────────────────────────────────────────────────────

    public void TentarUsarAtaque(int indice, Vector2 direcao, Transform alvo = null)
    {
        if (indice < 0 || indice >= ataques.Count) return;
        var ataque = ataques[indice];
        if (ataque == null) return;

        executor.ExecutarAtaqueManual(ataque, direcao, alvo);
    }

    public void DefinirAtaques(List<ClasseAtaque> novosAtaques) =>
        ataques = novosAtaques;

    public List<ClasseAtaque> ObterAtaques() => ataques;

    // ─── Utilitário ──────────────────────────────────────────────────────
    private Vector2 DirecaoParaMouse()
    {
        if (Camera.main == null) return Vector2.right;
        Vector2 posicaoMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return (posicaoMouse - (Vector2)transform.position).normalized;
    }
}