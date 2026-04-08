using UnityEngine;

/// <summary>
/// Intermediário entre todos os sistemas do jogo e o RetroSpriteAnimator.
/// Agora utiliza a versão inteligente do motor, sem nomes engessados no código!
/// </summary>
public class PokemonAnimatorController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // NOMES DAS CATEGORIAS (Editáveis no Inspector)
    // ─────────────────────────────────────────────────────────────────
    [Header("Configuração de Categorias")]
    [Tooltip("Nome da categoria no RSA para quando está parado. Ex: 'Ocioso' ou 'Idle'")]
    public string categoriaIdle = "Ocioso";

    [Tooltip("Nome da categoria no RSA para andar/correr. Ex: 'Movimento'")]
    public string categoriaMovimento = "Movimento";

    [Tooltip("Nome da categoria no RSA para ataques. Ex: 'Ataque'")]
    public string categoriaAtaque = "Ataque";

    [Tooltip("Nome da categoria no RSA para tomar dano. Ex: 'Dano' ou 'Hurt'")]
    public string categoriaHurt = "Dano";

    [Tooltip("Nome da categoria no RSA para quando é derrotado. Ex: 'Derrota'")]
    public string categoriaDerrota = "Derrota";

    // ─────────────────────────────────────────────────────────────────
    // REFERÊNCIAS
    // ─────────────────────────────────────────────────────────────────
    [Header("Referências")]
    [SerializeField] private RetroSpriteAnimator rsa;

    // ─────────────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────
    private RetroSpriteAnimator.Direcao8 _direcaoAtual = RetroSpriteAnimator.Direcao8.Baixo;
    private bool _emAtaque = false;
    private bool _emHurt = false;

    // ─────────────────────────────────────────────────────────────────
    // INICIALIZAÇÃO
    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rsa == null)
            rsa = GetComponent<RetroSpriteAnimator>();

        if (rsa == null)
            Debug.LogError($"[PokemonAnimatorController] RSA não encontrado em {gameObject.name}!");
    }

    private void Start()
    {
        // Inscreve nos eventos do RSA
        rsa.AoTerminarAnimacao += OnAnimacaoTerminou;
        rsa.AoAlcancaoQuadro += OnEventoDeQuadro;

        // Começa parado
        TocarIdle(_direcaoAtual);
    }

    private void OnDestroy()
    {
        if (rsa == null) return;
        rsa.AoTerminarAnimacao -= OnAnimacaoTerminou;
        rsa.AoAlcancaoQuadro -= OnEventoDeQuadro;
    }

    // ─────────────────────────────────────────────────────────────────
    // EVENTOS DO RSA
    // ─────────────────────────────────────────────────────────────────

    private void OnAnimacaoTerminou(string nomeAnimacao)
    {
        // Se um ataque terminou, volta para idle automaticamente
        if (_emAtaque)
        {
            _emAtaque = false;
            TocarIdle(_direcaoAtual);
        }

        // Se hurt terminou, volta para idle
        if (_emHurt)
        {
            _emHurt = false;
            TocarIdle(_direcaoAtual);
        }
    }

    private void OnEventoDeQuadro(string nomeAnimacao, int quadro)
    {
        // Útil para sincronizar hitbox de ataque com o frame exato do impacto.
        // Debug.Log($"[PAC] Evento no frame {quadro} do ataque '{nomeAnimacao}'");
    }

    // ─────────────────────────────────────────────────────────────────
    // MÉTODOS PÚBLICOS — chamados pelos outros sistemas
    // ─────────────────────────────────────────────────────────────────

    public void SetDirecao(Vector2 direcao)
    {
        if (direcao == Vector2.zero) return;
        _direcaoAtual = RetroSpriteAnimator.VetorParaDirecao8(direcao);
    }

    /// <summary>
    /// Toca a animação de Idle. Passa só a Categoria e Direção. O motor acha o resto.
    /// </summary>
    public void TocarIdle(RetroSpriteAnimator.Direcao8 direcao)
    {
        if (_emHurt) return;
        _direcaoAtual = direcao;
        _emAtaque = false;

        rsa.TocarAnimacao(categoriaIdle, _direcaoAtual);
    }

    public void TocarIdle(Vector2 direcao)
        => TocarIdle(RetroSpriteAnimator.VetorParaDirecao8(direcao));

    /// <summary>
    /// Toca a animação de Movimento. Passa só a Categoria e Direção.
    /// </summary>
    public void TocarMovimento(Vector2 direcao)
    {
        if (_emAtaque || _emHurt) return;
        SetDirecao(direcao);

        rsa.TocarAnimacao(categoriaMovimento, _direcaoAtual);
    }

    /// <summary>
    /// Toca animação de ataque. 
    /// AQUI é o único lugar onde informamos o NOME, pois um Pokémon tem vários ataques (ex: "Tackle", "Ember").
    /// </summary>
    public void TocarAtaque(string nomeAtaque, Vector2 direcao)
    {
        if (_emHurt) return;
        SetDirecao(direcao);
        _emAtaque = true;

        rsa.TocarAnimacao(categoriaAtaque, _direcaoAtual, RetroSpriteAnimator.ModoSelecaoAnimacao.PorNome, nomeAtaque);
    }

    /// <summary>
    /// Toca a animação de tomar dano (Hurt). Passa só a Categoria e Direção.
    /// </summary>
    public void TocarHurt(Vector2 direcaoAtaque)
    {
        _emHurt = true;
        _emAtaque = false;
        var dir = RetroSpriteAnimator.VetorParaDirecao8(direcaoAtaque);

        rsa.TocarAnimacao(categoriaHurt, dir);
    }

    /// <summary>
    /// Toca animação de derrota. Direção "Nenhuma" pois ele geralmente desmaia no lugar.
    /// </summary>
    public void TocarDerrota()
    {
        _emAtaque = false;
        _emHurt = false;

        rsa.TocarAnimacao(categoriaDerrota, RetroSpriteAnimator.Direcao8.Nenhuma);
    }

    /// <summary>
    /// Para o ataque e força a volta pro Idle.
    /// </summary>
    public void CancelarAtaque()
    {
        _emAtaque = false;
        TocarIdle(_direcaoAtual);
    }

    // ─────────────────────────────────────────────────────────────────
    // GETTERS
    // ─────────────────────────────────────────────────────────────────
    public bool EstaEmAtaque() => _emAtaque;
    public bool EstaEmHurt() => _emHurt;
    public RetroSpriteAnimator.Direcao8 DirecaoAtual => _direcaoAtual;
}