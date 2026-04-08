using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// RetroSpriteAnimator — Sistema de animação por quadros para jogos 2D pixel art.
/// </summary>
public class RetroSpriteAnimator : MonoBehaviour
{
    // =====================================================================
    // ENUMS PÚBLICOS
    // =====================================================================

    public enum Direcao8
    {
        Nenhuma = 0, Cima = 1, Baixo = 2, Esquerda = 3, Direita = 4,
        CimaEsquerda = 5, CimaDireita = 6, BaixoEsquerda = 7, BaixoDireita = 8
    }

    public enum ModoSelecaoAnimacao { Primeira = 0, Aleatoria = 1, PorNome = 2 }

    // =====================================================================
    // CLASSES DE DADOS SERIALIZADAS
    // =====================================================================

    [Serializable]
    public class AnimacaoClip
    {
        public string nome = "Nova Animacao";

        [FormerlySerializedAs("nomeGrupoOrigem")]
        public string nomeSpriteSheetOrigem = "";

        public int[] quadros = new int[0];
        public float taxaQuadros = 10f;
        public bool emLoop = true;
        public bool espelharX = false;
        public bool espelharY = false;
        public List<int> eventosQuadro = new List<int>();
    }

    [Serializable]
    public class GrupoSprites
    {
        public string nome = "Nova Sprite Sheet";
        public Sprite spriteSheet;
        public Sprite[] sprites = new Sprite[0];
    }

    [Serializable]
    public class ConteinerDirecao
    {
        public Direcao8 direcao = Direcao8.Nenhuma;
        public List<AnimacaoClip> animacoes = new List<AnimacaoClip>();
    }

    [Serializable]
    public class CategoriaAnimacao
    {
        public string nome = "Nova Categoria";
        public bool usa8Direcoes = true;

        [FormerlySerializedAs("gruposDeSprites")]
        public List<GrupoSprites> spriteSheets = new List<GrupoSprites>();

        public List<ConteinerDirecao> direcoes = new List<ConteinerDirecao>();
    }

    // =====================================================================
    // CAMPOS PÚBLICOS
    // =====================================================================

    public string sortingLayer = "Default";
    public int sortOrder = 1;
    public List<CategoriaAnimacao> categorias = new List<CategoriaAnimacao>();

    public event Action<string> AoIniciarAnimacao;
    public event Action<string> AoTerminarAnimacao;
    public event Action<string, int> AoAlcancaoQuadro;

    private SpriteRenderer _spriteRenderer;
    private AnimacaoClip _animacaoAtual;
    private GrupoSprites _spriteSheetAtual;
    private string _nomeAnimacaoAtualRegistrado = "";

    private int _indiceQuadro = 0;
    private int _totalQuadrosAnimacao = 0;
    private float _tempoDecorrido = 0f;
    private bool _animacaoTerminou = false;
    private HashSet<int> _eventosQuadroAtivos = new HashSet<int>();

    // =====================================================================
    // INICIALIZAÇÃO
    // =====================================================================

    private void Awake()
    {
        GarantirSpriteRenderer();
    }

    private void GarantirSpriteRenderer()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        _spriteRenderer.sortingLayerName = sortingLayer;
        _spriteRenderer.sortingOrder = sortOrder;
    }

    // =====================================================================
    // LOOP DE ATUALIZAÇÃO
    // =====================================================================

    private void Update()
    {
        if (_animacaoAtual == null || _spriteSheetAtual == null) return;
        if (_animacaoTerminou) return;
        if (_animacaoAtual.quadros == null || _animacaoAtual.quadros.Length == 0) return;
        if (_animacaoAtual.taxaQuadros <= 0f) return;
        if (_spriteSheetAtual.sprites == null || _spriteSheetAtual.sprites.Length == 0) return;

        if (_nomeAnimacaoAtualRegistrado != _animacaoAtual.nome)
        {
            _nomeAnimacaoAtualRegistrado = _animacaoAtual.nome;
            AoIniciarAnimacao?.Invoke(_animacaoAtual.nome);
        }

        _tempoDecorrido += Time.deltaTime;
        float intervaloQuadro = 1f / Mathf.Max(0.1f, _animacaoAtual.taxaQuadros);
        int quadroAnterior = _indiceQuadro;

        while (_tempoDecorrido >= intervaloQuadro)
        {
            _tempoDecorrido -= intervaloQuadro;
            _indiceQuadro++;

            if (_indiceQuadro >= _totalQuadrosAnimacao)
            {
                if (_animacaoAtual.emLoop)
                {
                    _indiceQuadro = 0;
                }
                else
                {
                    _indiceQuadro = _totalQuadrosAnimacao - 1;
                    _animacaoTerminou = true;
                    AoTerminarAnimacao?.Invoke(_animacaoAtual.nome);
                    break;
                }
            }
        }

        if (_indiceQuadro != quadroAnterior && _eventosQuadroAtivos.Contains(_indiceQuadro))
            AoAlcancaoQuadro?.Invoke(_animacaoAtual.nome, _indiceQuadro);

        int spriteIndex = _animacaoAtual.quadros[_indiceQuadro];
        if (spriteIndex >= 0 && spriteIndex < _spriteSheetAtual.sprites.Length)
            _spriteRenderer.sprite = _spriteSheetAtual.sprites[spriteIndex];

        _spriteRenderer.flipX = _animacaoAtual.espelharX;
        _spriteRenderer.flipY = _animacaoAtual.espelharY;
    }

    // =====================================================================
    // MÉTODOS PÚBLICOS — TOCAR ANIMAÇÃO (A LÓGICA INTELIGENTE)
    // =====================================================================

    /// <summary>
    /// 1. O MAIS SIMPLES: Pega Categoria -> 1ª Sprite Sheet -> Direção -> 1ª Animação
    /// </summary>
    public void TocarAnimacao(string nomeCategoria, Direcao8 direcao)
    {
        TocarAnimacao(nomeCategoria, "", direcao, ModoSelecaoAnimacao.Primeira, "");
    }

    /// <summary>
    /// 2. COM MODO DE SELEÇÃO: Pega Categoria -> 1ª Sprite Sheet -> Direção -> Modo Escolhido -> (Nome Opcional)
    /// </summary>
    public void TocarAnimacao(string nomeCategoria, Direcao8 direcao, ModoSelecaoAnimacao modoSelecao, string nomeAnimacao = "")
    {
        TocarAnimacao(nomeCategoria, "", direcao, modoSelecao, nomeAnimacao);
    }

    /// <summary>
    /// 3. FULL CONTROLE: Pega Categoria -> Sprite Sheet Específica -> Direção -> Modo Escolhido -> (Nome Opcional)
    /// </summary>
    public void TocarAnimacao(string nomeCategoria, string nomeSpriteSheet, Direcao8 direcao, ModoSelecaoAnimacao modoSelecao, string nomeAnimacao = "")
    {
        if (!TentarObterCategoria(nomeCategoria, out var categoria))
        {
            Debug.LogWarning($"[RetroSpriteAnimator] Categoria '{nomeCategoria}' não encontrada!");
            return;
        }

        // Inteligência: Se não passar o nome da Sprite Sheet, pega a PRIMEIRA automaticamente.
        GrupoSprites spriteSheetOrigem = null;
        if (string.IsNullOrEmpty(nomeSpriteSheet))
        {
            if (categoria.spriteSheets.Count > 0) spriteSheetOrigem = categoria.spriteSheets[0];
        }
        else
        {
            spriteSheetOrigem = categoria.spriteSheets.Find(g => g != null && g.nome.Equals(nomeSpriteSheet, StringComparison.OrdinalIgnoreCase));
        }

        if (spriteSheetOrigem == null)
        {
            Debug.LogWarning($"[RetroSpriteAnimator] Nenhuma Sprite Sheet válida encontrada na categoria '{categoria.nome}'.");
            return;
        }

        var conteiner = ObterOuCriarConteinerDirecao(categoria, direcao);
        var candidatos = conteiner.animacoes.FindAll(a => a != null && a.nomeSpriteSheetOrigem.Equals(spriteSheetOrigem.nome, StringComparison.OrdinalIgnoreCase));

        if (candidatos.Count == 0)
        {
            Debug.LogWarning($"[RetroSpriteAnimator] Nenhuma animação configurada usando a Sprite Sheet '{spriteSheetOrigem.nome}' na direção '{direcao}'.");
            return;
        }

        AnimacaoClip escolhida = null;

        switch (modoSelecao)
        {
            case ModoSelecaoAnimacao.Primeira:
                escolhida = candidatos[0];
                break;

            case ModoSelecaoAnimacao.Aleatoria:
                escolhida = candidatos[UnityEngine.Random.Range(0, candidatos.Count)];
                break;

            case ModoSelecaoAnimacao.PorNome:
                if (string.IsNullOrEmpty(nomeAnimacao)) return;
                escolhida = candidatos.Find(a => a.nome.Equals(nomeAnimacao, StringComparison.OrdinalIgnoreCase));
                if (escolhida == null)
                {
                    Debug.LogWarning($"[RetroSpriteAnimator] Animação '{nomeAnimacao}' não encontrada em {categoria.nome}/{spriteSheetOrigem.nome}/{direcao}.");
                    return;
                }
                break;
        }

        DefinirAnimacao(escolhida, spriteSheetOrigem);
    }

    /// <summary>
    /// Busca global direto pelo nome (método antigo para compatibilidade)
    /// </summary>
    public void TocarAnimacaoGlobal(string nomeAnimacao)
    {
        if (TentarEncontrarAnimacaoGlobal(nomeAnimacao, out var animacao, out var spriteSheet))
        {
            DefinirAnimacao(animacao, spriteSheet);
            return;
        }
        Debug.LogWarning($"[RetroSpriteAnimator] Animação '{nomeAnimacao}' não encontrada globalmente!");
    }

    // =====================================================================
    // MÉTODOS INTERNOS E UTILITÁRIOS
    // =====================================================================

    public void PararAnimacao()
    {
        _animacaoAtual = null;
        _spriteSheetAtual = null;
        _nomeAnimacaoAtualRegistrado = "";
        _indiceQuadro = 0;
        _tempoDecorrido = 0f;
        _animacaoTerminou = false;
        _eventosQuadroAtivos.Clear();
    }

    private void DefinirAnimacao(AnimacaoClip animacao, GrupoSprites spriteSheet)
    {
        if (animacao == null || spriteSheet == null) return;

        bool ehNova = (_animacaoAtual == null || _animacaoAtual.nome != animacao.nome);

        _animacaoAtual = animacao;
        _spriteSheetAtual = spriteSheet;
        _totalQuadrosAnimacao = animacao.quadros != null ? animacao.quadros.Length : 0;
        _animacaoTerminou = false;

        ReconstruirEventosDeQuadro();

        if (ehNova)
        {
            _indiceQuadro = 0;
            _tempoDecorrido = 0f;
            _nomeAnimacaoAtualRegistrado = "";
        }
    }

    private void ReconstruirEventosDeQuadro()
    {
        _eventosQuadroAtivos.Clear();
        if (_animacaoAtual?.eventosQuadro == null) return;
        foreach (int ev in _animacaoAtual.eventosQuadro) _eventosQuadroAtivos.Add(ev);
    }

    public bool TentarObterCategoria(string nomeCategoria, out CategoriaAnimacao categoria)
    {
        categoria = null;
        if (categorias == null) return false;
        categoria = categorias.Find(c => c != null && c.nome.Equals(nomeCategoria, StringComparison.OrdinalIgnoreCase));
        return categoria != null;
    }

    public ConteinerDirecao ObterOuCriarConteinerDirecao(CategoriaAnimacao categoria, Direcao8 direcao)
    {
        if (categoria.direcoes == null) categoria.direcoes = new List<ConteinerDirecao>();

        var conteiner = categoria.direcoes.Find(b => b != null && b.direcao == direcao);
        if (conteiner == null)
        {
            conteiner = new ConteinerDirecao { direcao = direcao, animacoes = new List<AnimacaoClip>() };
            categoria.direcoes.Add(conteiner);
        }
        return conteiner;
    }

    private bool TentarEncontrarAnimacaoGlobal(string nomeAnimacao, out AnimacaoClip animacao, out GrupoSprites spriteSheet)
    {
        animacao = null; spriteSheet = null;
        if (categorias == null) return false;

        foreach (var cat in categorias)
        {
            if (cat?.direcoes == null || cat.spriteSheets == null) continue;
            foreach (var conteiner in cat.direcoes)
            {
                if (conteiner?.animacoes == null) continue;
                var encontrada = conteiner.animacoes.Find(x => x != null && x.nome.Equals(nomeAnimacao, StringComparison.OrdinalIgnoreCase));
                if (encontrada == null) continue;

                var g = cat.spriteSheets.Find(sg => sg != null && sg.nome.Equals(encontrada.nomeSpriteSheetOrigem, StringComparison.OrdinalIgnoreCase));
                if (g == null) continue;

                animacao = encontrada;
                spriteSheet = g;
                return true;
            }
        }
        return false;
    }

    public bool AnimacaoTerminou() => _animacaoTerminou;
    public string ObterNomeAnimacaoAtual() => _animacaoAtual != null ? _animacaoAtual.nome : "";
    public int ObterIndiceQuadroAtual() => _indiceQuadro;

    public SpriteRenderer ObterSpriteRenderer()
    {
        GarantirSpriteRenderer();
        return _spriteRenderer;
    }

    public static Direcao8 VetorParaDirecao8(Vector2 direcao)
    {
        if (direcao == Vector2.zero) return Direcao8.Nenhuma;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        if (angulo < 0) angulo += 360f;

        if (angulo >= 337.5f || angulo < 22.5f) return Direcao8.Direita;
        if (angulo >= 22.5f && angulo < 67.5f) return Direcao8.CimaDireita;
        if (angulo >= 67.5f && angulo < 112.5f) return Direcao8.Cima;
        if (angulo >= 112.5f && angulo < 157.5f) return Direcao8.CimaEsquerda;
        if (angulo >= 157.5f && angulo < 202.5f) return Direcao8.Esquerda;
        if (angulo >= 202.5f && angulo < 247.5f) return Direcao8.BaixoEsquerda;
        if (angulo >= 247.5f && angulo < 292.5f) return Direcao8.Baixo;
        if (angulo >= 292.5f && angulo < 337.5f) return Direcao8.BaixoDireita;

        return Direcao8.Baixo;
    }
}