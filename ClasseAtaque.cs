using UnityEngine;

/// <summary>
/// Wrapper de runtime para um ataque.
/// Mantém cooldown, estado de canalização e a instância de efeitos.
/// Substitui AssistantAttackClass — compatível com o novo DadosAtaque modular.
/// </summary>
[System.Serializable]
public class ClasseAtaque
{
    // ─── Referência aos dados do ataque ─────────────────────────────────
    public DadosAtaque dados;

    // ─── Estado interno ──────────────────────────────────────────────────
    private float _ultimoUso = -Mathf.Infinity;

    [SerializeField, HideInInspector]
    private InstanciaAtaque _instanciaBacking;

    // ─── Propriedades públicas ───────────────────────────────────────────
    public InstanciaAtaque Instancia => _instanciaBacking ??= new InstanciaAtaque();
    public Coroutine RotinaAtual { get; set; }
    public float UltimoUso => _ultimoUso;
    public bool EhCanalizado => dados != null && dados.ehAtaqueCanalizado;
    public bool EstaCanalizando => Instancia.estaCAnalizando;

    // ─── Construtor ──────────────────────────────────────────────────────
    public ClasseAtaque() { }
    public ClasseAtaque(DadosAtaque dados) { this.dados = dados; }

    // ─── Cooldown ────────────────────────────────────────────────────────
    public bool EstaDisponivel()
    {
        if (dados == null) return false;
        return Time.time >= _ultimoUso + dados.tempoRecarga;
    }

    public void AtivarCooldown() => _ultimoUso = Time.time;

    // ─── Canalização ─────────────────────────────────────────────────────
    public void IniciarCAnalizacao() => Instancia.estaCAnalizando = true;
    public void PararCAnalizacao() => Instancia.PararCAnalizacao();

    // ─── Limpeza ─────────────────────────────────────────────────────────
    public void LimparEfeitos() => Instancia.LimparEfeitos();
}