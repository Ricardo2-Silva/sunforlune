using UnityEngine;

/// <summary>
/// Guarda o estado em runtime de uma execução de ataque.
/// Substituição de AttackInstance — não interfere com scripts existentes.
/// </summary>
[System.Serializable]
public class InstanciaAtaque
{
    // ─── Estado de canalização ──────────────────────────────────────────
    public bool estaCAnalizando { get; set; }
    public Coroutine rotinaCAnalizacao { get; set; }

    // ─── Efeitos visuais ativos ─────────────────────────────────────────
    public ParticleSystem sistemaParticula { get; set; }
    public GameObject objetoParticula { get; set; }
    public TrailRenderer trilha { get; set; }

    public InstanciaAtaque()
    {
        estaCAnalizando = false;
        rotinaCAnalizacao = null;
        sistemaParticula = null;
        objetoParticula = null;
        trilha = null;
    }

    /// <summary>Para a canalização e limpa efeitos com fade-out de 2s.</summary>
    public void PararCAnalizacao()
    {
        estaCAnalizando = false;
        rotinaCAnalizacao = null;

        if (sistemaParticula != null)
            sistemaParticula.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (objetoParticula != null)
        {
            Object.Destroy(objetoParticula, 2f);
            objetoParticula = null;
        }

        sistemaParticula = null;
    }

    /// <summary>Destrói efeitos imediatamente.</summary>
    public void LimparEfeitos()
    {
        if (objetoParticula != null)
        {
            Object.Destroy(objetoParticula);
            objetoParticula = null;
        }
        sistemaParticula = null;
        estaCAnalizando = false;
    }
}