using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RetroSpriteAnimator))]
public class RetroSpriteAnimatorEditor : Editor
{
    private RetroSpriteAnimator _alvo;

    private SerializedProperty _propSortingLayer;
    private SerializedProperty _propSortOrder;

    private int _indiceCategoriaSelecionada = 0;
    private RetroSpriteAnimator.Direcao8 _direcaoSelecionada = RetroSpriteAnimator.Direcao8.Nenhuma;

    private Dictionary<string, bool> _foldoutsAnimacoes = new Dictionary<string, bool>();
    private Dictionary<string, bool> _foldoutsGrupos = new Dictionary<string, bool>();
    private Dictionary<string, bool> _foldoutsGrade = new Dictionary<string, bool>();

    private int _tamanhoGridSprite = 48;

    private GUIStyle _estiloCabecalho;
    private GUIStyle _estiloSubCabecalho;
    private GUIStyle _estiloCaixa;
    private GUIStyle _estiloBotaoMini;
    private bool _estilosInicializados = false;

    private readonly Color _corCategoria = new Color(0.6f, 0.4f, 0.9f, 1f);
    private readonly Color _corGrupo = new Color(0.2f, 0.6f, 0.9f, 1f);
    private readonly Color _corAnimacao = new Color(0.3f, 0.8f, 0.4f, 1f);
    private readonly Color _corPerigo = new Color(0.9f, 0.3f, 0.3f, 1f);
    private readonly Color _corPreview = new Color(0.9f, 0.7f, 0.2f, 1f);

    private bool _previewTocando = false;
    private int _quadroPreview = 0;
    private double _ultimoTempoPreview = 0;

    private int _indiceAnimacaoPreview = -1;
    private RetroSpriteAnimator.Direcao8 _direcaoPreview = RetroSpriteAnimator.Direcao8.Nenhuma;
    private int _indiceCategoriaPreview = -1;

    private bool _previewNaCena = false;
    private Sprite _spriteSalvo = null;
    private bool _espelharXSalvo = false;
    private bool _espelharYSalvo = false;
    private bool _temSpriteSalvo = false;

    private void OnEnable()
    {
        _alvo = (RetroSpriteAnimator)target;
        _propSortingLayer = serializedObject.FindProperty("sortingLayer");
        _propSortOrder = serializedObject.FindProperty("sortOrder");
    }

    private void OnDisable()
    {
        PararPreviewSeguro();
    }

    private void InicializarEstilos()
    {
        if (_estilosInicializados) return;

        _estiloCabecalho = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        _estiloSubCabecalho = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        _estiloCaixa = new GUIStyle("box") { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(4, 4, 4, 4) };
        _estiloBotaoMini = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 20, fontStyle = FontStyle.Bold };
        _estilosInicializados = true;
    }

    public override void OnInspectorGUI()
    {
        if (_alvo == null) return;

        serializedObject.Update();
        InicializarEstilos();

        EditorGUILayout.Space(5);

        // CABEÇALHO COM BOTÃO PEQUENO NATIVO DA UNITY
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("⬛ Retro Sprite Animator", _estiloCabecalho);

        GUIContent iconRefresh = EditorGUIUtility.IconContent("Refresh");
        iconRefresh.tooltip = "Atualizar e corrigir referências de Sprite Sheets";

        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
        if (GUILayout.Button(iconRefresh, EditorStyles.miniButton, GUILayout.Width(28), GUILayout.Height(20)))
        {
            ValidarEAtualizarReferenciasGerais();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        DesenharSeparador();
        DesenharConfiguracoesGerais();
        EditorGUILayout.Space(5);
        DesenharSeparador();
        DesenharUI_Categorias();

        serializedObject.ApplyModifiedProperties();

        if (_previewTocando) Repaint();
    }

    private void ValidarEAtualizarReferenciasGerais()
    {
        if (_alvo.categorias == null) return;

        bool fezAlteracao = false;
        int correcoes = 0;

        foreach (var cat in _alvo.categorias)
        {
            if (cat.spriteSheets == null || cat.spriteSheets.Count == 0) continue;

            List<string> nomesValidos = new List<string>();
            foreach (var s in cat.spriteSheets)
            {
                if (s != null && !string.IsNullOrEmpty(s.nome))
                    nomesValidos.Add(s.nome);
            }

            if (nomesValidos.Count == 0) continue;
            if (cat.direcoes == null) continue;

            foreach (var dir in cat.direcoes)
            {
                if (dir.animacoes == null) continue;
                foreach (var anim in dir.animacoes)
                {
                    if (string.IsNullOrEmpty(anim.nomeSpriteSheetOrigem) || !nomesValidos.Contains(anim.nomeSpriteSheetOrigem))
                    {
                        anim.nomeSpriteSheetOrigem = nomesValidos[0];
                        fezAlteracao = true;
                        correcoes++;
                    }
                }
            }
        }

        if (fezAlteracao)
        {
            EditorUtility.SetDirty(_alvo);
            Debug.Log($"[RetroSpriteAnimator] Atualização concluída! {correcoes} referência(s) corrigida(s).");
        }
        else
        {
            Debug.Log("[RetroSpriteAnimator] Atualização concluída! Todas as referências já estavam corretas.");
        }
    }

    private void DesenharConfiguracoesGerais()
    {
        EditorGUILayout.LabelField("Configurações Gerais", _estiloSubCabecalho);
        EditorGUILayout.Space(3);
        DesenharPopupSortingLayer();
        EditorGUILayout.PropertyField(_propSortOrder, new GUIContent("Ordem na Camada (Sort Order)"));
    }

    private void DesenharPopupSortingLayer()
    {
        string atual = _propSortingLayer.stringValue;
        var camadas = SortingLayer.layers;
        string[] nomes = new string[camadas.Length];
        int indiceAtual = 0;

        for (int i = 0; i < camadas.Length; i++)
        {
            nomes[i] = camadas[i].name;
            if (camadas[i].name == atual) indiceAtual = i;
        }

        EditorGUI.BeginChangeCheck();
        int novoIndice = EditorGUILayout.Popup("Camada (Sorting Layer)", indiceAtual, nomes);
        if (EditorGUI.EndChangeCheck())
            _propSortingLayer.stringValue = nomes[novoIndice];
    }

    private void DesenharUI_Categorias()
    {
        EditorGUILayout.BeginVertical(_estiloCaixa);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Categorias", _estiloSubCabecalho);

        GUI.backgroundColor = _corCategoria;
        if (GUILayout.Button("+ Nova Categoria", _estiloBotaoMini, GUILayout.Width(130)))
        {
            Undo.RecordObject(_alvo, "Adicionar Categoria");
            _alvo.categorias.Add(new RetroSpriteAnimator.CategoriaAnimacao());
            EditorUtility.SetDirty(_alvo);
            _indiceCategoriaSelecionada = Mathf.Clamp(_alvo.categorias.Count - 1, 0, _alvo.categorias.Count - 1);
            PararPreviewSeguro();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);

        if (_alvo.categorias == null) _alvo.categorias = new List<RetroSpriteAnimator.CategoriaAnimacao>();

        if (_alvo.categorias.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma categoria criada. Clique em '+ Nova Categoria' para começar.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        _indiceCategoriaSelecionada = Mathf.Clamp(_indiceCategoriaSelecionada, 0, _alvo.categorias.Count - 1);

        string[] nomesCategorias = new string[_alvo.categorias.Count];
        for (int i = 0; i < _alvo.categorias.Count; i++)
        {
            var c = _alvo.categorias[i];
            nomesCategorias[i] = (c == null || string.IsNullOrEmpty(c.nome)) ? $"Categoria {i}" : c.nome;
        }

        EditorGUI.BeginChangeCheck();
        _indiceCategoriaSelecionada = EditorGUILayout.Popup("Categoria Atual", _indiceCategoriaSelecionada, nomesCategorias);
        if (EditorGUI.EndChangeCheck())
        {
            var cat = _alvo.categorias[_indiceCategoriaSelecionada];
            _direcaoSelecionada = ValidarDirecao(cat, _direcaoSelecionada);
            PararPreviewSeguro();
        }

        var categoria = _alvo.categorias[_indiceCategoriaSelecionada];
        if (categoria == null) { EditorGUILayout.EndVertical(); return; }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string novoNome = EditorGUILayout.TextField("Nome da Categoria", categoria.nome);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_alvo, "Renomear Categoria");
            categoria.nome = novoNome;
            EditorUtility.SetDirty(_alvo);
        }

        GUI.backgroundColor = _corPerigo;
        if (GUILayout.Button("Remover", _estiloBotaoMini, GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Remover Categoria", $"Remover '{categoria.nome}'?", "Sim", "Cancelar"))
            {
                Undo.RecordObject(_alvo, "Remover Categoria");
                _alvo.categorias.RemoveAt(_indiceCategoriaSelecionada);
                EditorUtility.SetDirty(_alvo);
                _indiceCategoriaSelecionada = Mathf.Clamp(_indiceCategoriaSelecionada - 1, 0, Mathf.Max(0, _alvo.categorias.Count - 1));
                PararPreviewSeguro();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        bool usa8 = EditorGUILayout.Toggle("Usar 8 Direções", categoria.usa8Direcoes);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_alvo, "Alternar 8 Direções");
            categoria.usa8Direcoes = usa8;
            EditorUtility.SetDirty(_alvo);
            _direcaoSelecionada = ValidarDirecao(categoria, _direcaoSelecionada);
            PararPreviewSeguro();
        }

        EditorGUILayout.Space(8);
        DesenharGruposSpritesCategoria(categoria);

        EditorGUILayout.Space(8);
        DesenharSeparador();
        EditorGUILayout.Space(4);
        DesenharSeletorDirecao(categoria);

        EditorGUILayout.Space(8);
        DesenharAreaDirecao(categoria, _direcaoSelecionada);

        EditorGUILayout.EndVertical();
    }

    private void DesenharGruposSpritesCategoria(RetroSpriteAnimator.CategoriaAnimacao categoria)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Configuração de Sprite Sheets", _estiloSubCabecalho);

        GUI.backgroundColor = _corGrupo;
        if (GUILayout.Button("+ Nova Sprite Sheet", _estiloBotaoMini, GUILayout.Width(140)))
        {
            Undo.RecordObject(_alvo, "Adicionar Sprite Sheet");
            if (categoria.spriteSheets == null)
                categoria.spriteSheets = new List<RetroSpriteAnimator.GrupoSprites>();
            categoria.spriteSheets.Add(new RetroSpriteAnimator.GrupoSprites());
            EditorUtility.SetDirty(_alvo);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (categoria.spriteSheets == null || categoria.spriteSheets.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma Sprite Sheet configurada. Adicione uma para começar.", MessageType.Info);
            return;
        }

        for (int i = 0; i < categoria.spriteSheets.Count; i++)
            DesenharUmGrupoSprites(categoria, i);
    }

    private void DesenharUmGrupoSprites(RetroSpriteAnimator.CategoriaAnimacao categoria, int indice)
    {
        var spriteSheet = categoria.spriteSheets[indice];
        string chave = $"grupo_{_indiceCategoriaSelecionada}_{indice}";

        if (!_foldoutsGrupos.ContainsKey(chave)) _foldoutsGrupos[chave] = true;

        EditorGUILayout.BeginVertical(_estiloCaixa);
        EditorGUILayout.BeginHorizontal();

        string label = string.IsNullOrEmpty(spriteSheet.nome) ? $"Sprite Sheet {indice}" : spriteSheet.nome;
        int contagemSprites = spriteSheet.sprites != null ? spriteSheet.sprites.Length : 0;

        _foldoutsGrupos[chave] = EditorGUILayout.Foldout(_foldoutsGrupos[chave],
            $"🖼 {label}  [{contagemSprites} sprites]", true);

        GUI.backgroundColor = _corPerigo;
        if (GUILayout.Button("✕", _estiloBotaoMini, GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Remover Sprite Sheet", $"Remover '{label}'?", "Sim", "Cancelar"))
            {
                Undo.RecordObject(_alvo, "Remover Sprite Sheet");
                categoria.spriteSheets.RemoveAt(indice);
                PararPreviewSeguro();
                EditorUtility.SetDirty(_alvo);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (_foldoutsGrupos[chave])
        {
            EditorGUI.BeginChangeCheck();
            string novoNome = EditorGUILayout.TextField("Nome (Chave)", spriteSheet.nome);
            if (EditorGUI.EndChangeCheck() && novoNome != spriteSheet.nome)
            {
                Undo.RecordObject(_alvo, "Renomear Sprite Sheet");

                // Renomeação em cascata
                foreach (var d in categoria.direcoes)
                {
                    if (d.animacoes == null) continue;
                    foreach (var a in d.animacoes)
                    {
                        if (a.nomeSpriteSheetOrigem == spriteSheet.nome)
                            a.nomeSpriteSheetOrigem = novoNome;
                    }
                }

                spriteSheet.nome = novoNome;
                EditorUtility.SetDirty(_alvo);
            }

            EditorGUI.BeginChangeCheck();
            Sprite novaSheet = (Sprite)EditorGUILayout.ObjectField("Arquivo da Sprite Sheet", spriteSheet.spriteSheet, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_alvo, "Trocar Sprite Sheet");
                spriteSheet.spriteSheet = novaSheet;
                CarregarSpritesDoGrupo(spriteSheet);
                EditorUtility.SetDirty(_alvo);
            }

            if (spriteSheet.spriteSheet != null && (spriteSheet.sprites == null || spriteSheet.sprites.Length == 0))
                EditorGUILayout.HelpBox("Nenhuma sprite encontrada. Verifique se o Sprite Mode está como 'Multiple' e se a imagem foi fatiada.", MessageType.Warning);

            if (spriteSheet.sprites != null && spriteSheet.sprites.Length > 0)
                DesenharGradeDeSprites(spriteSheet, chave);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DesenharSeletorDirecao(RetroSpriteAnimator.CategoriaAnimacao categoria)
    {
        EditorGUILayout.LabelField("Direção Selecionada", _estiloSubCabecalho);

        List<RetroSpriteAnimator.Direcao8> permitidas = ObterDirecoesPermitidas(categoria);
        string[] labels = new string[permitidas.Count];
        int indiceAtual = 0;

        for (int i = 0; i < permitidas.Count; i++)
        {
            labels[i] = TraduziirDirecao(permitidas[i]);
            if (permitidas[i] == _direcaoSelecionada) indiceAtual = i;
        }

        EditorGUI.BeginChangeCheck();
        indiceAtual = EditorGUILayout.Popup("Editar Direção", indiceAtual, labels);
        if (EditorGUI.EndChangeCheck())
        {
            _direcaoSelecionada = permitidas[indiceAtual];
            PararPreviewSeguro();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Seleção Rápida", EditorStyles.miniBoldLabel);
        DesenharDpad(categoria);
    }

    private string TraduziirDirecao(RetroSpriteAnimator.Direcao8 dir)
    {
        return dir switch
        {
            RetroSpriteAnimator.Direcao8.Nenhuma => "— Nenhuma —",
            RetroSpriteAnimator.Direcao8.Cima => "↑ Cima",
            RetroSpriteAnimator.Direcao8.Baixo => "↓ Baixo",
            RetroSpriteAnimator.Direcao8.Esquerda => "← Esquerda",
            RetroSpriteAnimator.Direcao8.Direita => "→ Direita",
            RetroSpriteAnimator.Direcao8.CimaEsquerda => "↖ Cima-Esquerda",
            RetroSpriteAnimator.Direcao8.CimaDireita => "↗ Cima-Direita",
            RetroSpriteAnimator.Direcao8.BaixoEsquerda => "↙ Baixo-Esquerda",
            RetroSpriteAnimator.Direcao8.BaixoDireita => "↘ Baixo-Direita",
            _ => dir.ToString()
        };
    }

    private void DesenharDpad(RetroSpriteAnimator.CategoriaAnimacao categoria)
    {
        bool diag = categoria.usa8Direcoes;
        GUILayoutOption largBtn = GUILayout.Width(38);
        GUILayoutOption altBtn = GUILayout.Height(28);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        DesenharBotaoDir(diag ? RetroSpriteAnimator.Direcao8.CimaEsquerda : (RetroSpriteAnimator.Direcao8?)null, "↖", largBtn, altBtn);
        DesenharBotaoDir(RetroSpriteAnimator.Direcao8.Cima, "↑", largBtn, altBtn);
        DesenharBotaoDir(diag ? RetroSpriteAnimator.Direcao8.CimaDireita : (RetroSpriteAnimator.Direcao8?)null, "↗", largBtn, altBtn);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DesenharBotaoDir(RetroSpriteAnimator.Direcao8.Esquerda, "←", largBtn, altBtn);
        GUI.enabled = false;
        GUILayout.Button("·", largBtn, altBtn);
        GUI.enabled = true;
        DesenharBotaoDir(RetroSpriteAnimator.Direcao8.Direita, "→", largBtn, altBtn);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DesenharBotaoDir(diag ? RetroSpriteAnimator.Direcao8.BaixoEsquerda : (RetroSpriteAnimator.Direcao8?)null, "↙", largBtn, altBtn);
        DesenharBotaoDir(RetroSpriteAnimator.Direcao8.Baixo, "↓", largBtn, altBtn);
        DesenharBotaoDir(diag ? RetroSpriteAnimator.Direcao8.BaixoDireita : (RetroSpriteAnimator.Direcao8?)null, "↘", largBtn, altBtn);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DesenharBotaoDir(RetroSpriteAnimator.Direcao8? dir, string texto, params GUILayoutOption[] opcoes)
    {
        if (dir == null)
        {
            GUI.enabled = false;
            GUILayout.Button(" ", opcoes);
            GUI.enabled = true;
            return;
        }

        bool selecionado = (_direcaoSelecionada == dir.Value);
        Color corAntiga = GUI.backgroundColor;
        GUI.backgroundColor = selecionado ? _corCategoria : Color.white;

        if (GUILayout.Button(texto, opcoes))
        {
            _direcaoSelecionada = dir.Value;
            PararPreviewSeguro();
        }
        GUI.backgroundColor = corAntiga;
    }

    private List<RetroSpriteAnimator.Direcao8> ObterDirecoesPermitidas(RetroSpriteAnimator.CategoriaAnimacao categoria)
    {
        var lista = new List<RetroSpriteAnimator.Direcao8>
        {
            RetroSpriteAnimator.Direcao8.Nenhuma, RetroSpriteAnimator.Direcao8.Cima, RetroSpriteAnimator.Direcao8.Baixo,
            RetroSpriteAnimator.Direcao8.Esquerda, RetroSpriteAnimator.Direcao8.Direita
        };

        if (categoria.usa8Direcoes)
        {
            lista.Add(RetroSpriteAnimator.Direcao8.CimaEsquerda); lista.Add(RetroSpriteAnimator.Direcao8.CimaDireita);
            lista.Add(RetroSpriteAnimator.Direcao8.BaixoEsquerda); lista.Add(RetroSpriteAnimator.Direcao8.BaixoDireita);
        }
        return lista;
    }

    private RetroSpriteAnimator.Direcao8 ValidarDirecao(RetroSpriteAnimator.CategoriaAnimacao categoria, RetroSpriteAnimator.Direcao8 dir)
    {
        if (dir == RetroSpriteAnimator.Direcao8.Nenhuma) return dir;
        if (!categoria.usa8Direcoes && (dir == RetroSpriteAnimator.Direcao8.CimaEsquerda || dir == RetroSpriteAnimator.Direcao8.CimaDireita ||
             dir == RetroSpriteAnimator.Direcao8.BaixoEsquerda || dir == RetroSpriteAnimator.Direcao8.BaixoDireita))
            return RetroSpriteAnimator.Direcao8.Nenhuma;
        return dir;
    }

    private void DesenharAreaDirecao(RetroSpriteAnimator.CategoriaAnimacao categoria, RetroSpriteAnimator.Direcao8 direcao)
    {
        var conteiner = _alvo.ObterOuCriarConteinerDirecao(categoria, direcao);
        if (conteiner.animacoes == null) conteiner.animacoes = new List<RetroSpriteAnimator.AnimacaoClip>();

        EditorGUILayout.LabelField($"Animações — {TraduziirDirecao(direcao)}", _estiloSubCabecalho);
        EditorGUILayout.Space(4);
        DesenharListaAnimacoes(categoria, conteiner);
    }

    private void DesenharListaAnimacoes(RetroSpriteAnimator.CategoriaAnimacao categoria, RetroSpriteAnimator.ConteinerDirecao conteiner)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Animações desta Direção", _estiloSubCabecalho);

        GUI.backgroundColor = _corAnimacao;
        if (GUILayout.Button("+ Nova Animação", _estiloBotaoMini, GUILayout.Width(130)))
        {
            Undo.RecordObject(_alvo, "Adicionar Animação");
            conteiner.animacoes.Add(new RetroSpriteAnimator.AnimacaoClip
            {
                nome = "Nova Animacao",
                nomeSpriteSheetOrigem = "",
                taxaQuadros = 10f,
                emLoop = true,
                quadros = new int[0],
                eventosQuadro = new List<int>()
            });
            EditorUtility.SetDirty(_alvo);

            if (categoria.spriteSheets != null && categoria.spriteSheets.Count > 0)
            {
                conteiner.animacoes[conteiner.animacoes.Count - 1].nomeSpriteSheetOrigem = categoria.spriteSheets[0].nome;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (conteiner.animacoes.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma animação nesta direção. Clique em '+ Nova Animação' para criar.", MessageType.Info);
            return;
        }

        for (int i = 0; i < conteiner.animacoes.Count; i++)
            DesenharUmaAnimacao(categoria, conteiner, i);
    }

    private void DesenharUmaAnimacao(RetroSpriteAnimator.CategoriaAnimacao categoria, RetroSpriteAnimator.ConteinerDirecao conteiner, int indiceAnimacao)
    {
        var animacao = conteiner.animacoes[indiceAnimacao];
        if (animacao == null) return;

        string chave = $"anim_{_indiceCategoriaSelecionada}_{_direcaoSelecionada}_{indiceAnimacao}";
        if (!_foldoutsAnimacoes.ContainsKey(chave)) _foldoutsAnimacoes[chave] = false;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        string label = string.IsNullOrEmpty(animacao.nome) ? $"Animação {indiceAnimacao}" : animacao.nome;
        int numQuadros = animacao.quadros != null ? animacao.quadros.Length : 0;

        _foldoutsAnimacoes[chave] = EditorGUILayout.Foldout(_foldoutsAnimacoes[chave],
            $"▶ {label}  [{numQuadros} quadros]", true);

        if (GUILayout.Button("Copiar de...", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            GenericMenu menu = new GenericMenu();
            foreach (var direcaoCat in categoria.direcoes)
            {
                if (direcaoCat.animacoes == null) continue;
                foreach (var animOutra in direcaoCat.animacoes)
                {
                    if (direcaoCat.direcao == conteiner.direcao && animOutra == animacao) continue;
                    string textoMenu = $"{TraduziirDirecao(direcaoCat.direcao)}/{animOutra.nome}";
                    menu.AddItem(new GUIContent(textoMenu), false, () =>
                    {
                        Undo.RecordObject(_alvo, "Copiar Animação");
                        animacao.nomeSpriteSheetOrigem = animOutra.nomeSpriteSheetOrigem;
                        animacao.taxaQuadros = animOutra.taxaQuadros;
                        animacao.emLoop = animOutra.emLoop;
                        animacao.quadros = (int[])animOutra.quadros.Clone();
                        EditorUtility.SetDirty(_alvo);
                    });
                }
            }
            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("Nenhuma outra animação encontrada"));
            menu.ShowAsContext();
        }

        bool ehEstePreview = EhEstePreview(_indiceCategoriaSelecionada, _direcaoSelecionada, indiceAnimacao);

        GUI.backgroundColor = ehEstePreview ? _corPerigo : _corPreview;
        if (GUILayout.Button(ehEstePreview ? "⏹ Parar" : "▶ Visualizar", _estiloBotaoMini, GUILayout.Width(85)))
        {
            if (ehEstePreview) PararPreviewSeguro();
            else IniciarPreview(_indiceCategoriaSelecionada, _direcaoSelecionada, indiceAnimacao);
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = _corPerigo;
        if (GUILayout.Button("✕", _estiloBotaoMini, GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Remover Animação", $"Remover '{label}'?", "Sim", "Cancelar"))
            {
                Undo.RecordObject(_alvo, "Remover Animação");
                if (ehEstePreview) PararPreviewSeguro();
                conteiner.animacoes.RemoveAt(indiceAnimacao);
                EditorUtility.SetDirty(_alvo);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (_foldoutsAnimacoes[chave])
        {
            EditorGUI.BeginChangeCheck();
            string novoNome = EditorGUILayout.TextField("Nome", animacao.nome);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_alvo, "Renomear Animação");
                animacao.nome = novoNome;
                EditorUtility.SetDirty(_alvo);
            }

            DesenharSeletorGrupoAnimacao(categoria, animacao);

            EditorGUI.BeginChangeCheck();
            float fps = EditorGUILayout.FloatField("Taxa de Quadros (FPS)", animacao.taxaQuadros);
            bool loop = EditorGUILayout.Toggle("Repetir em Loop", animacao.emLoop);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_alvo, "Configurações da Animação");
                animacao.taxaQuadros = Mathf.Max(0.1f, fps);
                animacao.emLoop = loop;
                EditorUtility.SetDirty(_alvo);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool espX = EditorGUILayout.Toggle("Espelhar X", animacao.espelharX);
            bool espY = EditorGUILayout.Toggle("Espelhar Y", animacao.espelharY);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_alvo, "Espelhamento");
                animacao.espelharX = espX;
                animacao.espelharY = espY;
                EditorUtility.SetDirty(_alvo);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DesenharEventosDeQuadro(animacao);
            EditorGUILayout.Space(6);

            var grupoOrigem = categoria.spriteSheets.Find(g => g != null && g.nome == animacao.nomeSpriteSheetOrigem);
            if (grupoOrigem == null)
            {
                EditorGUILayout.HelpBox("Selecione uma Sprite Sheet válida para editar quadros e visualizar.", MessageType.Warning);
            }
            else
            {
                DesenharEditorDeQuadros(grupoOrigem, animacao);
                if (EhEstePreview(_indiceCategoriaSelecionada, _direcaoSelecionada, indiceAnimacao))
                    DesenharPreviewEmbutido(grupoOrigem, animacao);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DesenharSeletorGrupoAnimacao(RetroSpriteAnimator.CategoriaAnimacao categoria, RetroSpriteAnimator.AnimacaoClip animacao)
    {
        var nomes = new List<string>();
        foreach (var g in categoria.spriteSheets)
            if (g != null && !string.IsNullOrEmpty(g.nome)) nomes.Add(g.nome);

        if (nomes.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma Sprite Sheet configurada nesta categoria.", MessageType.Warning);
            if (!string.IsNullOrEmpty(animacao.nomeSpriteSheetOrigem))
            {
                animacao.nomeSpriteSheetOrigem = "";
                EditorUtility.SetDirty(_alvo);
            }
            return;
        }

        int indiceAtual = nomes.IndexOf(animacao.nomeSpriteSheetOrigem);
        bool nomeInvalido = (indiceAtual == -1);

        if (nomeInvalido) indiceAtual = 0;

        EditorGUI.BeginChangeCheck();
        int novoIndice = EditorGUILayout.Popup("Sprite Sheet Fonte", indiceAtual, nomes.ToArray());

        if (EditorGUI.EndChangeCheck() || nomeInvalido)
        {
            Undo.RecordObject(_alvo, "Trocar Fonte da Animação");
            animacao.nomeSpriteSheetOrigem = nomes[novoIndice];
            EditorUtility.SetDirty(_alvo);
        }
    }

    private void DesenharEventosDeQuadro(RetroSpriteAnimator.AnimacaoClip animacao)
    {
        EditorGUILayout.LabelField("Eventos por Quadro", _estiloSubCabecalho);
        if (animacao.eventosQuadro == null) animacao.eventosQuadro = new List<int>();

        int totalQuadros = animacao.quadros != null ? animacao.quadros.Length : 0;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Adicionar", GUILayout.Width(90)))
        {
            Undo.RecordObject(_alvo, "Adicionar Evento");
            animacao.eventosQuadro.Add(0);
            EditorUtility.SetDirty(_alvo);
        }
        if (GUILayout.Button("Ordenar", GUILayout.Width(70)))
        {
            Undo.RecordObject(_alvo, "Ordenar Eventos");
            animacao.eventosQuadro.Sort();
            for (int i = animacao.eventosQuadro.Count - 2; i >= 0; i--)
                if (animacao.eventosQuadro[i] == animacao.eventosQuadro[i + 1]) animacao.eventosQuadro.RemoveAt(i + 1);
            EditorUtility.SetDirty(_alvo);
        }
        GUI.backgroundColor = _corPerigo;
        if (GUILayout.Button("Limpar", GUILayout.Width(65)))
        {
            Undo.RecordObject(_alvo, "Limpar Eventos");
            animacao.eventosQuadro.Clear();
            EditorUtility.SetDirty(_alvo);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (animacao.eventosQuadro.Count == 0) return;

        bool foraDoLimite = false;
        for (int i = 0; i < animacao.eventosQuadro.Count; i++)
        {
            int ev = animacao.eventosQuadro[i];
            if (totalQuadros > 0 && (ev < 0 || ev >= totalQuadros)) foraDoLimite = true;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Evento {i}", GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            int v = EditorGUILayout.IntField(ev);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_alvo, "Editar Evento");
                animacao.eventosQuadro[i] = Mathf.Max(0, v);
                EditorUtility.SetDirty(_alvo);
            }
            GUI.backgroundColor = _corPerigo;
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                Undo.RecordObject(_alvo, "Remover Evento");
                animacao.eventosQuadro.RemoveAt(i);
                EditorUtility.SetDirty(_alvo);
                EditorGUILayout.EndHorizontal();
                break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        if (foraDoLimite)
            EditorGUILayout.HelpBox("Alguns eventos estão fora do intervalo da animação.", MessageType.Warning);
    }

    private void DesenharEditorDeQuadros(RetroSpriteAnimator.GrupoSprites grupo, RetroSpriteAnimator.AnimacaoClip animacao)
    {
        EditorGUILayout.LabelField("Sequência de Quadros", _estiloSubCabecalho);

        string framesTexto = animacao.quadros != null ? string.Join(", ", animacao.quadros) : "";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Índices:", GUILayout.Width(55));
        EditorGUI.BeginChangeCheck();
        string novoTexto = EditorGUILayout.TextField(framesTexto);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_alvo, "Editar Quadros");
            animacao.quadros = ParsearSequenciaQuadros(novoTexto);
            EditorUtility.SetDirty(_alvo);
            if (_previewTocando) _quadroPreview = Mathf.Clamp(_quadroPreview, 0, Mathf.Max(0, (animacao.quadros?.Length ?? 1) - 1));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Formatos aceitos:  0, 1, 2, 3  |  0-3  |  0-3x2 (repete 2×)", MessageType.None);

        if (grupo.sprites == null || grupo.sprites.Length == 0) return;

        EditorGUILayout.Space(4);
        if (animacao.quadros != null && animacao.quadros.Length > 0) DesenharTiraDeQuadros(grupo, animacao);
    }

    private void DesenharTiraDeQuadros(RetroSpriteAnimator.GrupoSprites grupo, RetroSpriteAnimator.AnimacaoClip animacao)
    {
        EditorGUILayout.BeginVertical("box");
        float larguraDisp = EditorGUIUtility.currentViewWidth - 70;
        int tamTira = 40;
        int colunas = Mathf.Max(1, Mathf.FloorToInt(larguraDisp / (tamTira + 8)));
        int linhas = Mathf.CeilToInt((float)animacao.quadros.Length / colunas);

        for (int l = 0; l < linhas; l++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < colunas; c++)
            {
                int idQuadro = l * colunas + c;
                if (idQuadro >= animacao.quadros.Length) break;

                int idSprite = animacao.quadros[idQuadro];

                EditorGUILayout.BeginVertical(GUILayout.Width(tamTira + 4));
                Rect rectSprite = GUILayoutUtility.GetRect(tamTira, tamTira, GUILayout.Width(tamTira), GUILayout.Height(tamTira));

                if (idSprite >= 0 && idSprite < grupo.sprites.Length && grupo.sprites[idSprite] != null)
                    DesenharPreviewSprite(rectSprite, grupo.sprites[idSprite]);
                else
                {
                    EditorGUI.DrawRect(rectSprite, new Color(0.8f, 0.2f, 0.2f, 0.5f));
                    GUI.Label(rectSprite, "?", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 16 });
                }

                GUIStyle estiloId = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 };
                EditorGUILayout.LabelField($"[{idQuadro}]={idSprite}", estiloId, GUILayout.Width(tamTira));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DesenharGradeDeSprites(RetroSpriteAnimator.GrupoSprites grupo, string chaveGrupo)
    {
        string chaveGrade = $"grade_{chaveGrupo}";
        if (!_foldoutsGrade.ContainsKey(chaveGrade)) _foldoutsGrade[chaveGrade] = false;

        _foldoutsGrade[chaveGrade] = EditorGUILayout.Foldout(_foldoutsGrade[chaveGrade],
            $"Sprites Carregados ({grupo.sprites.Length} encontrados)", true);
        if (!_foldoutsGrade[chaveGrade]) return;

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tamanho da Miniatura:", GUILayout.Width(140));
        _tamanhoGridSprite = (int)EditorGUILayout.Slider(_tamanhoGridSprite, 32, 128);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(3);

        float larg = EditorGUIUtility.currentViewWidth - 50;
        int colunas = Mathf.Max(1, Mathf.FloorToInt(larg / (_tamanhoGridSprite + 10)));
        int total = grupo.sprites.Length;
        int linhas = Mathf.CeilToInt((float)total / colunas);

        EditorGUILayout.BeginVertical("box");
        for (int l = 0; l < linhas; l++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < colunas; c++)
            {
                int id = l * colunas + c;
                if (id >= total) break;

                EditorGUILayout.BeginVertical(GUILayout.Width(_tamanhoGridSprite + 4));
                Rect rect = GUILayoutUtility.GetRect(_tamanhoGridSprite, _tamanhoGridSprite,
                    GUILayout.Width(_tamanhoGridSprite), GUILayout.Height(_tamanhoGridSprite));

                if (grupo.sprites[id] != null) DesenharPreviewSprite(rect, grupo.sprites[id]);

                GUIStyle estiloIdx = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                EditorGUILayout.LabelField(id.ToString(), estiloIdx, GUILayout.Width(_tamanhoGridSprite));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void CarregarSpritesDoGrupo(RetroSpriteAnimator.GrupoSprites grupo)
    {
        if (grupo.spriteSheet == null) { grupo.sprites = new Sprite[0]; return; }
        string caminho = AssetDatabase.GetAssetPath(grupo.spriteSheet);
        Object[] dados = AssetDatabase.LoadAllAssetRepresentationsAtPath(caminho);
        var lista = new List<Sprite>();
        foreach (var d in dados) if (d is Sprite s) lista.Add(s);
        grupo.sprites = lista.ToArray();
    }

    private void IniciarPreview(int indiceCategoria, RetroSpriteAnimator.Direcao8 direcao, int indiceAnimacao)
    {
        var animacao = ObterAnimacaoParaPreview(indiceCategoria, direcao, indiceAnimacao, out var grupoOrigem);
        if (animacao == null || grupoOrigem == null ||
            animacao.quadros == null || animacao.quadros.Length == 0 ||
            animacao.taxaQuadros <= 0f ||
            grupoOrigem.sprites == null || grupoOrigem.sprites.Length == 0) return;

        if (_previewTocando) PararPreviewSeguro();

        SalvarSpriteDaCenaSeguro();
        _indiceCategoriaPreview = indiceCategoria;
        _direcaoPreview = direcao;
        _indiceAnimacaoPreview = indiceAnimacao;
        _quadroPreview = 0;
        _previewTocando = true;
        _ultimoTempoPreview = EditorApplication.timeSinceStartup;
        _previewNaCena = false;

        EditorApplication.update += AtualizarEditor;
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void PararPreviewSeguro()
    {
        _previewTocando = false;
        _indiceCategoriaPreview = -1;
        _indiceAnimacaoPreview = -1;
        _direcaoPreview = RetroSpriteAnimator.Direcao8.Nenhuma;
        _quadroPreview = 0;
        _previewNaCena = false;

        RestaurarSpriteDaCenaSeguro();
        EditorApplication.update -= AtualizarEditor;
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void AtualizarEditor()
    {
        if (!_previewTocando) return;
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Repaint();
    }

    private bool EhEstePreview(int indiceCategoria, RetroSpriteAnimator.Direcao8 direcao, int indiceAnimacao)
    {
        return _previewTocando && _indiceCategoriaPreview == indiceCategoria &&
               _direcaoPreview == direcao && _indiceAnimacaoPreview == indiceAnimacao;
    }

    private void DesenharPreviewEmbutido(RetroSpriteAnimator.GrupoSprites grupoOrigem, RetroSpriteAnimator.AnimacaoClip animacao)
    {
        DesenharSeparador();
        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview da Animação", _estiloSubCabecalho);

        bool novoNaCena = EditorGUILayout.ToggleLeft("Mostrar na Cena", _previewNaCena, GUILayout.Width(130));
        if (novoNaCena != _previewNaCena)
        {
            _previewNaCena = novoNaCena;
            if (!_previewNaCena) RestaurarSpriteDaCenaSeguro();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Quadro: {_quadroPreview + 1} / {animacao.quadros.Length}", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(3);

        float tamPreview = 80;
        Rect rectPrev = GUILayoutUtility.GetRect(tamPreview, tamPreview, GUILayout.ExpandWidth(true));
        Rect rectCentro = new Rect(rectPrev.x + (rectPrev.width - tamPreview) * 0.5f, rectPrev.y, tamPreview, tamPreview);
        EditorGUI.DrawRect(rectCentro, new Color(0.12f, 0.12f, 0.12f, 1f));

        if (animacao.quadros == null || animacao.quadros.Length == 0 ||
            grupoOrigem.sprites == null || grupoOrigem.sprites.Length == 0) return;

        _quadroPreview = Mathf.Clamp(_quadroPreview, 0, animacao.quadros.Length - 1);
        int idSprite = animacao.quadros[_quadroPreview];

        if (idSprite >= 0 && idSprite < grupoOrigem.sprites.Length && grupoOrigem.sprites[idSprite] != null)
        {
            Sprite spriteAtual = grupoOrigem.sprites[idSprite];
            if (animacao.espelharX || animacao.espelharY)
            {
                Matrix4x4 backup = GUI.matrix;
                Vector2 pivo = new Vector2(rectCentro.x + rectCentro.width * 0.5f, rectCentro.y + rectCentro.height * 0.5f);
                GUIUtility.ScaleAroundPivot(new Vector2(animacao.espelharX ? -1 : 1, animacao.espelharY ? -1 : 1), pivo);
                DesenharPreviewSprite(rectCentro, spriteAtual);
                GUI.matrix = backup;
            }
            else DesenharPreviewSprite(rectCentro, spriteAtual);

            if (_previewNaCena) AtualizarPreviewNaCenaSeguro(spriteAtual, animacao.espelharX, animacao.espelharY);
        }

        AvancarQuadroPreview(animacao);
    }

    private void AvancarQuadroPreview(RetroSpriteAnimator.AnimacaoClip animacao)
    {
        double tempoAtual = EditorApplication.timeSinceStartup;
        double decorrido = tempoAtual - _ultimoTempoPreview;
        float intervalo = 1f / Mathf.Max(0.1f, animacao.taxaQuadros);

        if (decorrido >= intervalo)
        {
            _ultimoTempoPreview = tempoAtual;
            _quadroPreview++;
            if (_quadroPreview >= animacao.quadros.Length)
            {
                if (animacao.emLoop) _quadroPreview = 0;
                else { _quadroPreview = animacao.quadros.Length - 1; PararPreviewSeguro(); }
            }
        }
    }

    private void SalvarSpriteDaCenaSeguro()
    {
        try
        {
            if (this == null || _alvo == null) return;
            var sr = _alvo.GetComponent<SpriteRenderer>();
            if (sr != null) { _spriteSalvo = sr.sprite; _espelharXSalvo = sr.flipX; _espelharYSalvo = sr.flipY; _temSpriteSalvo = true; }
        }
        catch (System.Exception) { }
    }

    private void RestaurarSpriteDaCenaSeguro()
    {
        try
        {
            if (this == null || _alvo == null) return;
            var sr = _alvo.GetComponent<SpriteRenderer>();
            if (sr != null && _temSpriteSalvo) { sr.sprite = _spriteSalvo; sr.flipX = _espelharXSalvo; sr.flipY = _espelharYSalvo; }
        }
        catch (System.Exception) { }

        _temSpriteSalvo = false;
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void AtualizarPreviewNaCenaSeguro(Sprite sprite, bool espelharX, bool espelharY)
    {
        try
        {
            if (this == null || _alvo == null) return;
            var sr = _alvo.GetComponent<SpriteRenderer>() ?? _alvo.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.flipX = espelharX; sr.flipY = espelharY;
        }
        catch (System.Exception) { }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private RetroSpriteAnimator.AnimacaoClip ObterAnimacaoParaPreview(int indiceCategoria, RetroSpriteAnimator.Direcao8 direcao, int indiceAnimacao, out RetroSpriteAnimator.GrupoSprites grupoOrigem)
    {
        grupoOrigem = null;
        if (_alvo.categorias == null || indiceCategoria < 0 || indiceCategoria >= _alvo.categorias.Count) return null;
        var cat = _alvo.categorias[indiceCategoria];
        if (cat == null) return null;
        var conteiner = _alvo.ObterOuCriarConteinerDirecao(cat, direcao);
        if (conteiner == null || conteiner.animacoes == null || indiceAnimacao < 0 || indiceAnimacao >= conteiner.animacoes.Count) return null;
        var animacao = conteiner.animacoes[indiceAnimacao];
        if (animacao == null) return null;
        grupoOrigem = cat.spriteSheets.Find(g => g != null && g.nome == animacao.nomeSpriteSheetOrigem);
        return animacao;
    }

    private void DesenharPreviewSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        Rect texCoords = new Rect(sprite.textureRect.x / sprite.texture.width, sprite.textureRect.y / sprite.texture.height,
            sprite.textureRect.width / sprite.texture.width, sprite.textureRect.height / sprite.texture.height);

        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

        float proporcao = sprite.textureRect.width / sprite.textureRect.height;
        Rect rectDesenho = rect;

        if (proporcao > 1f) { float novaAlt = rect.height / proporcao; rectDesenho.y += (rect.height - novaAlt) * 0.5f; rectDesenho.height = novaAlt; }
        else if (proporcao < 1f) { float novaLarg = rect.width * proporcao; rectDesenho.x += (rect.width - novaLarg) * 0.5f; rectDesenho.width = novaLarg; }

        GUI.DrawTextureWithTexCoords(rectDesenho, sprite.texture, texCoords);
    }

    private void DesenharSeparador()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private int[] ParsearSequenciaQuadros(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return new int[0];

        var quadros = new List<int>();
        foreach (string parte in entrada.Split(','))
        {
            string limpo = parte.Trim();
            if (limpo.Contains("x") && limpo.Contains("-"))
            {
                string[] xParts = limpo.Split('x');
                if (xParts.Length == 2 && int.TryParse(xParts[1].Trim(), out int reps))
                {
                    string[] rangeParts = xParts[0].Trim().Split('-');
                    if (rangeParts.Length == 2 && int.TryParse(rangeParts[0].Trim(), out int ini) && int.TryParse(rangeParts[1].Trim(), out int fim))
                    {
                        for (int r = 0; r < reps; r++)
                        {
                            if (ini <= fim) for (int i = ini; i <= fim; i++) quadros.Add(i);
                            else for (int i = ini; i >= fim; i--) quadros.Add(i);
                        }
                        continue;
                    }
                }
            }

            if (limpo.Contains("-"))
            {
                string[] tracos = limpo.Split('-');
                if (tracos.Length == 2 && int.TryParse(tracos[0].Trim(), out int i1) && int.TryParse(tracos[1].Trim(), out int i2))
                {
                    if (i1 <= i2) for (int i = i1; i <= i2; i++) quadros.Add(i);
                    else for (int i = i1; i >= i2; i--) quadros.Add(i);
                    continue;
                }
            }

            if (int.TryParse(limpo, out int val)) quadros.Add(val);
        }
        return quadros.ToArray();
    }
}