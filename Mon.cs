using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa uma instância única de Pokémon no jogo.
/// Responsável por nível, ataques, experiência, evolução e componentes visuais/físicos.
/// NÃO controla HP diretamente (SRP: script SaudePokemon faz isso).
/// </summary>
public class Mon : MonoBehaviour
{
    [Header("Dados base do Pokémon (imutáveis)")]
    [SerializeField] private DadosPokemon _base;

    [Header("Nível e Experiência")]
    [SerializeField] private int currentLevel;
    [SerializeField] private int currentExp;

    [Header("Captura")]
    [SerializeField] private bool isCaptured = false;
    [SerializeField] private PokeballData capturedPokeball;

    // Ataques aprendidos
    public List<AssistantAttackClass> Attacks = new List<AssistantAttackClass>();

    // Componentes visuais e físicos
    public Rigidbody2D Rb2D;
    public Animator anim;
    public SpriteRenderer SpriteRenderer;
    public Transform ObjTransform;

    // Animadores e Partículas // Fazer avalição se as componentes serão postas aqui ou de outro script para mega-e e evolução
    [HideInInspector] public RuntimeAnimatorController posturaContinua;
    [HideInInspector] public GameObject objetoParticula2;
    [HideInInspector] public ParticleSystem attackParticle2;

    // Propriedades de fácil acesso
    public DadosPokemon Base => _base;
    public int Nivel => currentLevel;
    public int ExpAtual => currentExp;

    public bool IsCaptured => isCaptured;
    public PokeballData CapturedPokeball => capturedPokeball;
    // Inicialização dos componentes e dados
    private void Awake()
    {
        AtualizarAtaquesPorNivel();
    }

    /// <summary>
    /// Atualiza a lista de ataques disponíveis de acordo com o nível atual e os dados base.
    /// </summary>
    public void AtualizarAtaquesPorNivel()
    {
        Attacks.Clear();
        if (_base == null || _base.AttackDefinitions == null) return;

        foreach (var entry in _base.AttackDefinitions)
        {
            if (entry.level <= currentLevel && entry.attack != null)
            {
                // Evita aprender o mesmo ataque mais de uma vez
                if (!Attacks.Exists(a => a.data == entry.attack))
                    Attacks.Add(new AssistantAttackClass(entry.attack));
            }
        }
    }

    #region Stats Dinâmicos
    public int MaxHp => Mathf.FloorToInt((_base.MaxHp * currentLevel) / 100f) + 10;
    public int Attack => Mathf.FloorToInt((_base.Attack * currentLevel) / 100f) + 5;
    public int Defense => Mathf.FloorToInt((_base.Defense * currentLevel) / 100f) + 5;
    public int SpAttack => Mathf.FloorToInt((_base.SpAttack * currentLevel) / 100f) + 5;
    public int SpDefense => Mathf.FloorToInt((_base.SpDefense * currentLevel) / 100f) + 5;
    public int Speed => Mathf.FloorToInt((_base.Speed * currentLevel) / 100f) + 5;
    #endregion

    /// <summary>
    /// Ganha experiência e faz o level up, se necessário.
    /// </summary>
    public void GanharExperiencia(int quantidade)
    {
        currentExp += quantidade;

        // Checa se atingiu o exp necessário para subir de nível
        int expProxNivel = _base.GetExpForLevel(currentLevel + 1);
        while (currentExp >= expProxNivel)
        {
            SubirNivel();
            expProxNivel = _base.GetExpForLevel(currentLevel + 1);
        }
    }

    private void SubirNivel()
    {
        currentLevel++;
        AtualizarAtaquesPorNivel();
        // Aqui pode-se animar o level up, atualizar UI, etc.
        Debug.Log($"{_base.Nome} subiu para o nível {currentLevel}!");
        // Checa se pode evoluir
        ChecarEvolucao();
    }

    /// <summary>
    /// Checa se há evolução disponível para o nível atual e executa.
    /// </summary>
    private void ChecarEvolucao()
    {
        if (_base.Evolucoes == null) return;

        foreach (var evo in _base.Evolucoes)
        {
            if (evo.nivelRequerido > 0 && currentLevel >= evo.nivelRequerido && evo.evoluiPara != null)
            {
                Evoluir(evo.evoluiPara);
                break;
            }
        }
    }

    /// <summary>
    /// Evolui este Pokémon para uma nova espécie.
    /// </summary>
    public void Evoluir(DadosPokemon novaBase)
    {
        if (novaBase == null)
        {
            Debug.LogWarning("Tentando evoluir para uma base nula!");
            return;
        }

        // Troca dados base para a espécie evoluída
        _base = novaBase;
        AtualizarAtaquesPorNivel();

        // (Opcional) Chamar efeitos visuais/sonoros
        StartCoroutine(EfeitoEvolucao());

        Debug.Log($"Seu Pokémon evoluiu para {_base.Nome}!");
    }

    private IEnumerator EfeitoEvolucao()
    {
        // Exemplo: disparar partículas, animações, etc.
        Debug.Log("Pokémon está evoluindo...");
        if (attackParticle2 != null) attackParticle2.Play();
        yield return new WaitForSeconds(1.5f);
        Debug.Log("Evolução completa!");
        // Atualize UI, sprite, ou dispare eventos conforme necessário.
    }

    /// <summary>
    /// Permite inicializar Mon com espécie e nível específicos (usado por Spawner).
    /// </summary>
    public void SetarDados(DadosPokemon dados, int nivel)
    {
        _base = dados;
        currentLevel = nivel;
        currentExp = _base.GetExpForLevel(nivel);
        AtualizarAtaquesPorNivel();
    }
    public void StopAttack()
    {
        if (anim != null)
            anim.SetBool("Attack", false);

        if (Rb2D != null && Rb2D.velocity == Vector2.zero && anim != null)
        {
            anim.SetBool("Chase", false);
        }
    }
}