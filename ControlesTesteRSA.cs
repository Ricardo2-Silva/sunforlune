using System;
using UnityEngine;

/// <summary>
/// Controlador de teste para o RetroSpriteAnimator.
/// Agora usando a lógica simplificada: Categoria -> Direção.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(RetroSpriteAnimator))]
public class ControlesTesteRSA : MonoBehaviour
{
    // =====================================================================
    // ESTRUTURA DE CONFIGURAÇÃO DE ESTADO
    // Apenas a Categoria importa agora! O motor resolve o resto.
    // =====================================================================

    [Serializable]
    public class ConfiguracaoEstadoRSA
    {
        [Tooltip("Categoria no RSA (ex: 'Movimento', 'Ocioso')")]
        public string categoria = "Movimento";
    }

    [Header("Referências")]
    public RetroSpriteAnimator rsa;
    public Rigidbody2D corpoRigido;

    [Header("Configuração dos Estados de Animação")]
    public ConfiguracaoEstadoRSA animacaoParado = new ConfiguracaoEstadoRSA { categoria = "Ocioso" };
    public ConfiguracaoEstadoRSA animacaoAndar = new ConfiguracaoEstadoRSA { categoria = "Movimento" };
    public ConfiguracaoEstadoRSA animacaoCorrer = new ConfiguracaoEstadoRSA { categoria = "Movimento" };
    public ConfiguracaoEstadoRSA animacaoDash = new ConfiguracaoEstadoRSA { categoria = "Dash" };

    [Header("Movimento")]
    public float velocidadeAndar = 150f;
    public float velocidadeCorrer = 280f;

    [Header("Dash")]
    public float forcaDash = 700f;
    public float duracaoDash = 0.18f;
    public float tempoCooldownDash = 0.6f;
    public KeyCode teclaDash = KeyCode.Space;

    [Header("Mira Orbital")]
    public bool usarMiraOrbital = false;
    public Camera cameraPrincipal;
    public Transform indicadorMira;
    public float raioMira = 0.8f;

    private Vector2 _entrada;
    private RetroSpriteAnimator.Direcao8 _direcaoAtual = RetroSpriteAnimator.Direcao8.Baixo;

    private bool _correndo = false;
    private bool _emDash = false;
    private float _tempoDash = 0f;
    private float _tempoCooldown = 0f;
    private Vector2 _direcaoDash;

    private Vector2 _posicaoMira;
    private RetroSpriteAnimator.Direcao8 _direcaoMira = RetroSpriteAnimator.Direcao8.Baixo;

    private void Awake()
    {
        if (rsa == null) rsa = GetComponent<RetroSpriteAnimator>();
        if (corpoRigido == null) corpoRigido = GetComponent<Rigidbody2D>();
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        corpoRigido.gravityScale = 0f;
        corpoRigido.freezeRotation = true;
    }

    private void Update()
    {
        ProcessarEntrada();
        ProcessarDash();
        AtualizarMiraOrbital();
        AtualizarAnimacao();
    }

    private void FixedUpdate()
    {
        AplicarMovimento();
    }

    private void ProcessarEntrada()
    {
        if (_emDash) return;

        _entrada.x = Input.GetAxisRaw("Horizontal");
        _entrada.y = Input.GetAxisRaw("Vertical");
        _entrada = Vector2.ClampMagnitude(_entrada, 1f);

        _correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void AplicarMovimento()
    {
        if (_emDash)
        {
            _tempoDash -= Time.fixedDeltaTime;
            if (_tempoDash <= 0f) FinalizarDash();
            return;
        }

        float velocidade = _correndo ? velocidadeCorrer : velocidadeAndar;
        corpoRigido.velocity = _entrada * velocidade * Time.fixedDeltaTime;
    }

    private void ProcessarDash()
    {
        if (_tempoCooldown > 0f) _tempoCooldown -= Time.deltaTime;

        if (!_emDash && _tempoCooldown <= 0f && Input.GetKeyDown(teclaDash))
            IniciarDash();
    }

    private void IniciarDash()
    {
        _direcaoDash = _entrada != Vector2.zero
            ? _entrada.normalized
            : DirecaoParaVetor(_direcaoAtual);

        if (_direcaoDash == Vector2.zero) return;

        _emDash = true;
        _tempoDash = duracaoDash;

        corpoRigido.velocity = Vector2.zero;
        corpoRigido.AddForce(_direcaoDash * forcaDash, ForceMode2D.Impulse);
    }

    private void FinalizarDash()
    {
        _emDash = false;
        _tempoCooldown = tempoCooldownDash;
        corpoRigido.velocity = Vector2.zero;
    }

    private void AtualizarMiraOrbital()
    {
        if (!usarMiraOrbital || cameraPrincipal == null) return;

        Vector3 posMouseMundo = cameraPrincipal.ScreenToWorldPoint(Input.mousePosition);
        posMouseMundo.z = 0f;

        _posicaoMira = (Vector2)(posMouseMundo - transform.position);
        _direcaoMira = RetroSpriteAnimator.VetorParaDirecao8(_posicaoMira);

        if (indicadorMira != null)
        {
            Vector2 offset = _posicaoMira.normalized * raioMira;
            indicadorMira.position = transform.position + (Vector3)offset;
        }
    }

    private void AtualizarAnimacao()
    {
        if (usarMiraOrbital)
        {
            _direcaoAtual = _direcaoMira;
        }
        else
        {
            if (_entrada != Vector2.zero)
                _direcaoAtual = RetroSpriteAnimator.VetorParaDirecao8(_entrada);
        }

        ConfiguracaoEstadoRSA estadoAtual;

        if (_emDash) estadoAtual = animacaoDash;
        else if (_entrada != Vector2.zero) estadoAtual = _correndo ? animacaoCorrer : animacaoAndar;
        else estadoAtual = animacaoParado;

        // O novo método super simplificado: Só Categoria e Direção!
        // Ele vai lá dentro da categoria, pega a primeira sprite sheet, e joga a primeira animação que achar na direção.
        rsa.TocarAnimacao(estadoAtual.categoria, _direcaoAtual);
    }

    private static Vector2 DirecaoParaVetor(RetroSpriteAnimator.Direcao8 dir)
    {
        return dir switch
        {
            RetroSpriteAnimator.Direcao8.Cima => Vector2.up,
            RetroSpriteAnimator.Direcao8.Baixo => Vector2.down,
            RetroSpriteAnimator.Direcao8.Esquerda => Vector2.left,
            RetroSpriteAnimator.Direcao8.Direita => Vector2.right,
            RetroSpriteAnimator.Direcao8.CimaEsquerda => new Vector2(-1f, 1f).normalized,
            RetroSpriteAnimator.Direcao8.CimaDireita => new Vector2(1f, 1f).normalized,
            RetroSpriteAnimator.Direcao8.BaixoEsquerda => new Vector2(-1f, -1f).normalized,
            RetroSpriteAnimator.Direcao8.BaixoDireita => new Vector2(1f, -1f).normalized,
            _ => Vector2.zero
        };
    }
}