using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(DadosAtaque))]
public class DadosAtaqueEditor : Editor
{
    private static class Cor
    {
        public static readonly Color FundoPainelPro = new Color(0.19f, 0.19f, 0.21f, 1f);
        public static readonly Color FundoPainelLight = new Color(0.76f, 0.76f, 0.78f, 1f);

        public static readonly Color FundoCard = new Color(0.17f, 0.17f, 0.20f, 1f);
        public static readonly Color FundoCardHover = new Color(0.21f, 0.21f, 0.25f, 1f);
        public static readonly Color BordaCard = new Color(0.28f, 0.28f, 0.33f, 1f);

        public static readonly Color FundoHeader = new Color(0.12f, 0.12f, 0.16f, 1f);
        public static readonly Color TextoPrimario = new Color(0.92f, 0.92f, 0.95f, 1f);
        public static readonly Color TextoSecundario = new Color(0.55f, 0.55f, 0.65f, 1f);

        public static readonly Color BotaoAdicionar = new Color(0.18f, 0.48f, 0.28f, 1f);
        public static readonly Color BotaoAdicionarHover = new Color(0.22f, 0.58f, 0.34f, 1f);
        public static readonly Color BotaoRemover = new Color(0.48f, 0.15f, 0.15f, 1f);
    }

    internal static readonly Dictionary<CategoriaAcao, DadosVisuaisCategoria> VisuaisCategoria
        = new Dictionary<CategoriaAcao, DadosVisuaisCategoria>
    {
        { CategoriaAcao.Animacao,    new("▶", "d_PlayButton", new Color(0.28f, 0.58f, 0.88f), new Color(0.10f, 0.20f, 0.36f)) },
        { CategoriaAcao.Movimento,   new("➤", "d_MoveTool", new Color(0.22f, 0.78f, 0.48f), new Color(0.08f, 0.28f, 0.18f)) },
        { CategoriaAcao.Dano,        new("⚔", "d_console.erroricon", new Color(0.88f, 0.28f, 0.28f), new Color(0.35f, 0.10f, 0.10f)) },
        { CategoriaAcao.Buff,        new("★", "d_Favorite", new Color(0.90f, 0.72f, 0.18f), new Color(0.35f, 0.27f, 0.06f)) },
        { CategoriaAcao.Projetil,    new("◉", "d_Profiler.NetworkMessages", new Color(0.90f, 0.50f, 0.18f), new Color(0.36f, 0.19f, 0.06f)) },
        { CategoriaAcao.Efeito,      new("✦", "d_ParticleSystem Icon", new Color(0.70f, 0.30f, 0.88f), new Color(0.27f, 0.11f, 0.35f)) },
        { CategoriaAcao.Condicional, new("⟳", "d_preAudioLoopOff", new Color(0.22f, 0.78f, 0.78f), new Color(0.08f, 0.30f, 0.30f)) },
        { CategoriaAcao.Exclusiva,   new("✸", "d_ScriptableObject Icon", new Color(0.90f, 0.28f, 0.58f), new Color(0.36f, 0.10f, 0.22f)) },
        { CategoriaAcao.Geral,       new("◆", "d_Folder Icon", new Color(0.52f, 0.52f, 0.60f), new Color(0.20f, 0.20f, 0.24f)) },
    };

    internal class DadosVisuaisCategoria
    {
        public string IconeTexto;
        public string IconeUnity;
        public Color CorPrincipal;
        public Color CorFundo;
        public DadosVisuaisCategoria(string iconeTexto, string iconeUnity, Color cor, Color fundo)
        { IconeTexto = iconeTexto; IconeUnity = iconeUnity; CorPrincipal = cor; CorFundo = fundo; }
    }

    private DadosAtaque _alvo;
    private SerializedProperty _acoesProp;
    private int _indexHover = -1;

    private GUIStyle _estiloCard, _estiloNomeAcao, _estiloDescAcao,
                     _estiloIconeAcao, _estiloHeader, _estiloBadge,
                     _estiloMiniBtn, _estiloMiniBtnDelete, _estiloVazio,
                     _estiloContainer;

    private bool _estilosInicializados;

    private void OnEnable()
    {
        _alvo = (DadosAtaque)target;
        _acoesProp = serializedObject.FindProperty("acoes");
        _estilosInicializados = false;
    }

    private void InicializarEstilos()
    {
        if (_estilosInicializados) return;
        _estilosInicializados = true;

        _estiloContainer = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        _estiloCard = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 4),
        };

        _estiloNomeAcao = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = Cor.TextoPrimario },
            alignment = TextAnchor.MiddleLeft,
        };

        _estiloDescAcao = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = Cor.TextoSecundario },
            alignment = TextAnchor.MiddleRight,
        };

        _estiloIconeAcao = new GUIStyle(EditorStyles.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };

        _estiloHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = Cor.TextoPrimario },
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 0, 0, 0),
        };

        _estiloBadge = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 10,
            normal = { textColor = Cor.TextoSecundario },
            alignment = TextAnchor.MiddleRight,
        };

        _estiloMiniBtn = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            padding = new RectOffset(2, 2, 1, 1),
            normal = { textColor = new Color(0.75f, 0.75f, 0.80f) },
        };

        _estiloMiniBtnDelete = new GUIStyle(_estiloMiniBtn)
        {
            normal = { textColor = new Color(0.88f, 0.40f, 0.40f) },
            hover = { textColor = new Color(1f, 0.55f, 0.55f) },
        };

        _estiloVazio = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 11,
            wordWrap = true,
            normal = { textColor = new Color(0.45f, 0.45f, 0.52f) },
        };
    }

    public override void OnInspectorGUI()
    {
        InicializarEstilos();
        _alvo = (DadosAtaque)target;
        _acoesProp = serializedObject.FindProperty("acoes");
        serializedObject.Update();

        SerializedProperty it = serializedObject.GetIterator();
        for (bool first = true; it.NextVisible(first); first = false)
        {
            if (it.name == "m_Script" || it.name == "acoes") continue;
            EditorGUILayout.PropertyField(it, true);
        }

        GUILayout.Space(14);
        DesenharPainelAcoes();

        serializedObject.ApplyModifiedProperties();
    }

    private void DesenharPainelAcoes()
    {
        int qtd = _acoesProp.arraySize;

        Rect headerRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
        GUI.Box(headerRect, GUIContent.none, _estiloContainer);
        DrawRectInset(headerRect, Cor.FundoHeader);

        GUI.Label(new Rect(headerRect.x + 8, headerRect.y, 20, 30),
                  "⚡", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter });

        GUI.Label(new Rect(headerRect.x + 28, headerRect.y, 160, 30),
                  "Sequência de Ações", _estiloHeader);

        string badge = qtd == 0 ? "vazio" : $"{qtd} ação{(qtd != 1 ? "ões" : "")}";
        GUI.Label(new Rect(headerRect.xMax - 80, headerRect.y, 74, 30), badge, _estiloBadge);

        Rect fundoRect = EditorGUILayout.BeginVertical(_estiloContainer);
        Color fundoInspector = EditorGUIUtility.isProSkin ? Cor.FundoPainelPro : Cor.FundoPainelLight;
        DrawRectInset(fundoRect, fundoInspector);

        GUILayout.Space(6);

        if (qtd == 0)
        {
            GUILayout.Space(10);
            GUI.Label(GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true)),
                      "Nenhuma ação adicionada.\nUse o botão abaixo para começar.", _estiloVazio);
            GUILayout.Space(10);
        }
        else
        {
            int remover = -1, subir = -1, descer = -1;

            for (int i = 0; i < qtd; i++)
            {
                var res = DesenharCard(i);
                if (res == Cmd.Remover) remover = i;
                if (res == Cmd.Subir) subir = i;
                if (res == Cmd.Descer) descer = i;
            }

            if (remover >= 0) { _acoesProp.DeleteArrayElementAtIndex(remover); }
            if (subir >= 0) { _acoesProp.MoveArrayElement(subir, subir - 1); }
            if (descer >= 0) { _acoesProp.MoveArrayElement(descer, descer + 1); }
        }

        GUILayout.Space(6);
        EditorGUILayout.EndVertical();

        DesenharRodape();
    }

    private enum Cmd { Nenhum, Remover, Subir, Descer }

    private Cmd DesenharCard(int i)
    {
        AcaoAtaque acao = _alvo.acoes[i];
        if (acao == null) return Cmd.Nenhum;

        var vis = VisuaisCategoria.GetValueOrDefault(acao.Categoria,
                  new DadosVisuaisCategoria("◆", "d_Folder Icon", Cor.TextoSecundario, new Color(0.18f, 0.18f, 0.22f)));

        Event ev = Event.current;
        bool isHover = (_indexHover == i);
        Cmd resultado = Cmd.Nenhum;

        Rect barraRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
        barraRect.x -= 2; barraRect.width += 4;
        EditorGUI.DrawRect(barraRect, vis.CorPrincipal);

        Rect cardRect = EditorGUILayout.BeginVertical(_estiloCard);
        DrawRectInset(cardRect, isHover ? Cor.FundoCardHover : Cor.FundoCard);

        EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
        GUILayout.Space(6);

        Rect bolaRect = GUILayoutUtility.GetRect(22, 26, GUILayout.Width(22));
        Rect bola = new Rect(bolaRect.x + 1, bolaRect.y + 5, 16, 16);
        EditorGUI.DrawRect(bola, vis.CorFundo);
        DrawBorderRect(bola, vis.CorPrincipal * 0.7f, 1);

        DrawIconInRect(bola, acao, vis);

        GUILayout.Space(6);

        GUILayout.Label(acao.NomeExibicao, _estiloNomeAcao, GUILayout.ExpandWidth(true));

        if (!string.IsNullOrEmpty(acao.Descricao))
            GUILayout.Label(acao.Descricao, _estiloDescAcao);

        GUILayout.Space(4);

        GUI.enabled = i > 0;
        if (GUILayout.Button("▲", _estiloMiniBtn, GUILayout.Width(22), GUILayout.Height(18)))
            resultado = Cmd.Subir;
        GUI.enabled = i < _alvo.acoes.Count - 1;
        if (GUILayout.Button("▼", _estiloMiniBtn, GUILayout.Width(22), GUILayout.Height(18)))
            resultado = Cmd.Descer;
        GUI.enabled = true;
        if (GUILayout.Button("✕", _estiloMiniBtnDelete, GUILayout.Width(22), GUILayout.Height(18)))
            resultado = Cmd.Remover;

        GUILayout.Space(4);
        EditorGUILayout.EndHorizontal();

        SerializedProperty propAcao = _acoesProp.GetArrayElementAtIndex(i);
        SerializedProperty iter = propAcao.Copy();
        SerializedProperty fim = propAcao.GetEndProperty();

        bool first = true;
        bool temCampos = false;
        while (iter.NextVisible(first) && !SerializedProperty.EqualContents(iter, fim))
        {
            first = false;
            temCampos = true;
            var label = new GUIContent(iter.displayName);
            Rect fieldRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(iter, label, true));
            fieldRect.x += 28;
            fieldRect.width -= 30;
            EditorGUI.PropertyField(fieldRect, iter, label, true);
        }

        if (temCampos) GUILayout.Space(4);

        EditorGUILayout.EndVertical();

        if (cardRect.width > 0)
        {
            if (ev.type == EventType.MouseMove || ev.type == EventType.Repaint)
            {
                bool dentro = cardRect.Contains(ev.mousePosition);
                if (dentro && _indexHover != i) { _indexHover = i; Repaint(); }
                if (!dentro && _indexHover == i) { _indexHover = -1; Repaint(); }
            }
        }

        GUILayout.Space(2);
        return resultado;
    }

    private void DesenharRodape()
    {
        Rect fundoRodape = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(fundoRodape, GUIContent.none, _estiloContainer);
        DrawRectInset(fundoRodape, Cor.FundoHeader);

        float margin = 8f;
        float altBtn = 26f;
        float yBtn = fundoRodape.y + (fundoRodape.height - altBtn) * 0.5f;
        float largLixeira = 34f;

        Rect rectAdd = new Rect(fundoRodape.x + margin, yBtn,
                                fundoRodape.width - margin * 2 - largLixeira - 6, altBtn);

        Color corAdd = rectAdd.Contains(Event.current.mousePosition)
                       ? Cor.BotaoAdicionarHover : Cor.BotaoAdicionar;
        EditorGUI.DrawRect(rectAdd, corAdd);
        DrawBorderRect(rectAdd, new Color(0.30f, 0.70f, 0.45f, 0.6f), 1);

        var estiloAdd = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.95f, 0.68f) },
        };
        GUI.Label(rectAdd, "＋  Adicionar Ação", estiloAdd);

        if (Event.current.type == EventType.MouseDown && rectAdd.Contains(Event.current.mousePosition))
        {
            PopupWindow.Show(rectAdd, new PopupMenuAcoes(serializedObject, _acoesProp));
            Event.current.Use();
        }

        Rect rectLixo = new Rect(rectAdd.xMax + 6, yBtn, largLixeira, altBtn);
        Color corLixo = rectLixo.Contains(Event.current.mousePosition)
                        ? new Color(0.65f, 0.18f, 0.18f) : Cor.BotaoRemover;
        EditorGUI.DrawRect(rectLixo, corLixo);
        DrawBorderRect(rectLixo, new Color(0.80f, 0.30f, 0.30f, 0.5f), 1);

        var lixoIconName = EditorGUIUtility.isProSkin ? "d_TreeEditor.Trash" : "TreeEditor.Trash";
        var lixoIcon = EditorGUIUtility.IconContent(lixoIconName).image as Texture2D;
        if (lixoIcon != null)
            GUI.DrawTexture(new Rect(rectLixo.x + 7, rectLixo.y + 5, 20, 20), lixoIcon, ScaleMode.ScaleToFit);
        else
            GUI.Label(rectLixo, "🗑", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.50f, 0.50f) },
            });

        if (Event.current.type == EventType.MouseDown && rectLixo.Contains(Event.current.mousePosition))
        {
            if (EditorUtility.DisplayDialog("Limpar Ações",
                $"Apagar todas as {_acoesProp.arraySize} ações?", "Sim", "Cancelar"))
                _acoesProp.ClearArray();
            Event.current.Use();
        }

        if (Event.current.type == EventType.MouseMove) Repaint();
    }

    private static void DrawRectInset(Rect r, Color c)
    {
        var inset = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
        EditorGUI.DrawRect(inset, c);
    }

    private static void DrawBorderRect(Rect r, Color c, float t)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    private void DrawIconInRect(Rect rect, AcaoAtaque acao, DadosVisuaisCategoria vis)
    {
        if (acao != null && acao.IconeCustomizado != null)
        {
            DrawSprite(rect, acao.IconeCustomizado);
            return;
        }

        string unityIcon = null;

        if (acao != null && !string.IsNullOrEmpty(acao.IconeUnity))
            unityIcon = acao.IconeUnity;
        else if (!string.IsNullOrEmpty(vis.IconeUnity))
            unityIcon = EditorGUIUtility.isProSkin ? vis.IconeUnity : vis.IconeUnity.Replace("d_", "");

        if (!string.IsNullOrEmpty(unityIcon))
        {
            var tex = EditorGUIUtility.IconContent(unityIcon).image as Texture2D;
            if (tex != null)
            {
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
                return;
            }
        }

        _estiloIconeAcao.normal.textColor = vis.CorPrincipal;
        GUI.Label(rect, vis.IconeTexto, _estiloIconeAcao);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null) return;

        var tex = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);

        GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  POPUP MENU — duas colunas: categorias | ações
// ═══════════════════════════════════════════════════════════════════════
public class PopupMenuAcoes : PopupWindowContent
{
    private readonly SerializedObject _so;
    private readonly SerializedProperty _acoesProp;

    private readonly Dictionary<CategoriaAcao, List<ItemAcaoMenu>> _porCategoria;
    private readonly List<CategoriaAcao> _ordemCategorias;

    private CategoriaAcao _catAtual;
    private Vector2 _scrollCat;
    private Vector2 _scrollAcoes;
    private int _hoverCat = -1;
    private int _hoverAcao = -1;

    private static readonly Color FundoEsq = new Color(0.10f, 0.10f, 0.13f);
    private static readonly Color FundoDir = new Color(0.14f, 0.14f, 0.17f);
    private static readonly Color CorSelecionada = new Color(0.18f, 0.30f, 0.48f);
    private static readonly Color CorHover = new Color(0.20f, 0.20f, 0.25f);
    private static readonly Color Divisor = new Color(0.25f, 0.25f, 0.30f);

    private const float LargColunaEsq = 110f;
    private const float AltLinha = 30f;
    private const float AltLinhaAcao = 44f;

    private class ItemAcaoMenu
    {
        public Type Tipo;
        public string Nome;
        public string Descricao;
        public DadosAtaqueEditor.DadosVisuaisCategoria Vis;
        public AcaoAtaque Instancia;
    }

    public PopupMenuAcoes(SerializedObject so, SerializedProperty prop)
    {
        _so = so;
        _acoesProp = prop;
        _porCategoria = new Dictionary<CategoriaAcao, List<ItemAcaoMenu>>();
        _ordemCategorias = new List<CategoriaAcao>
        {
            CategoriaAcao.Animacao, CategoriaAcao.Movimento, CategoriaAcao.Dano,
            CategoriaAcao.Buff,     CategoriaAcao.Projetil,  CategoriaAcao.Efeito,
            CategoriaAcao.Condicional, CategoriaAcao.Exclusiva, CategoriaAcao.Geral
        };

        foreach (var tipo in TypeCache.GetTypesDerivedFrom<AcaoAtaque>()
                                      .Where(t => !t.IsAbstract)
                                      .OrderBy(t => t.Name))
        {
            AcaoAtaque inst;
            try { inst = (AcaoAtaque)Activator.CreateInstance(tipo); } catch { continue; }

            var cat = inst.Categoria;
            if (!_porCategoria.ContainsKey(cat)) _porCategoria[cat] = new List<ItemAcaoMenu>();
            _porCategoria[cat].Add(new ItemAcaoMenu
            {
                Tipo = tipo,
                Nome = inst.NomeExibicao,
                Descricao = inst.Descricao,
                Vis = DadosAtaqueEditor.VisuaisCategoria.GetValueOrDefault(cat,
                            new DadosAtaqueEditor.DadosVisuaisCategoria("◆", "d_Folder Icon",
                                new Color(0.52f, 0.52f, 0.60f), new Color(0.20f, 0.20f, 0.24f))),
                Instancia = inst
            });
        }

        _catAtual = _ordemCategorias.FirstOrDefault(c => _porCategoria.ContainsKey(c));
    }

    public override Vector2 GetWindowSize() => new Vector2(420, 320);

    public override void OnGUI(Rect rect)
    {
        Event ev = Event.current;

        EditorGUI.DrawRect(rect, FundoEsq);

        Rect rectEsq = new Rect(0, 0, LargColunaEsq, rect.height);
        EditorGUI.DrawRect(rectEsq, FundoEsq);

        EditorGUI.DrawRect(new Rect(LargColunaEsq, 0, 1, rect.height), Divisor);

        var categoriasCom = _ordemCategorias.Where(c => _porCategoria.ContainsKey(c)).ToList();

        float totalAltEsq = categoriasCom.Count * AltLinha;
        Rect viewportEsq = new Rect(0, 0, LargColunaEsq - 1, rect.height);
        Rect contentEsq = new Rect(0, 0, LargColunaEsq - 1, Mathf.Max(totalAltEsq, rect.height));

        _scrollCat = GUI.BeginScrollView(viewportEsq, _scrollCat, contentEsq, false, false);

        for (int ci = 0; ci < categoriasCom.Count; ci++)
        {
            CategoriaAcao cat = categoriasCom[ci];
            bool sel = (cat == _catAtual);
            bool hover = (_hoverCat == ci);
            var vis = DadosAtaqueEditor.VisuaisCategoria.GetValueOrDefault(cat,
                         new DadosAtaqueEditor.DadosVisuaisCategoria("◆", "d_Folder Icon",
                             new Color(0.52f, 0.52f, 0.60f), new Color(0.20f, 0.20f, 0.24f)));

            Rect linhaRect = new Rect(0, ci * AltLinha, LargColunaEsq - 1, AltLinha);

            if (sel) EditorGUI.DrawRect(linhaRect, CorSelecionada);
            else if (hover) EditorGUI.DrawRect(linhaRect, CorHover);

            if (sel) EditorGUI.DrawRect(new Rect(0, linhaRect.y, 3, AltLinha), vis.CorPrincipal);

            Rect bolinha = new Rect(linhaRect.x + 6, linhaRect.y + 7, 16, 16);
            EditorGUI.DrawRect(bolinha, vis.CorFundo);
            var estiloIco = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = vis.CorPrincipal }
            };

            var iconName = EditorGUIUtility.isProSkin ? vis.IconeUnity : vis.IconeUnity.Replace("d_", "");
            var iconTex = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            if (iconTex != null)
                GUI.DrawTexture(bolinha, iconTex, ScaleMode.ScaleToFit);
            else
                GUI.Label(bolinha, vis.IconeTexto, estiloIco);

            var estiloNomeCat = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = sel ? Color.white : new Color(0.65f, 0.65f, 0.72f) },
                fontStyle = sel ? FontStyle.Bold : FontStyle.Normal,
            };
            GUI.Label(new Rect(linhaRect.x + 28, linhaRect.y, linhaRect.width - 30, AltLinha),
                      cat.ToString(), estiloNomeCat);

            if (linhaRect.Contains(ev.mousePosition))
            {
                if (_hoverCat != ci) { _hoverCat = ci; editorWindow.Repaint(); }
                if (ev.type == EventType.MouseDown && ev.button == 0)
                {
                    _catAtual = cat;
                    _scrollAcoes = Vector2.zero;
                    _hoverAcao = -1;
                    ev.Use();
                }
            }
            else if (_hoverCat == ci)
            {
                _hoverCat = -1;
                editorWindow.Repaint();
            }
        }

        GUI.EndScrollView();

        float xDir = LargColunaEsq + 1;
        float largDir = rect.width - xDir;
        Rect rectDir = new Rect(xDir, 0, largDir, rect.height);
        EditorGUI.DrawRect(rectDir, FundoDir);

        List<ItemAcaoMenu> acoes = _porCategoria.ContainsKey(_catAtual)
                                   ? _porCategoria[_catAtual] : new List<ItemAcaoMenu>();

        float totalAltDir = acoes.Count * AltLinhaAcao;
        Rect viewportDir = new Rect(xDir, 0, largDir, rect.height);
        Rect contentDir = new Rect(xDir, 0, largDir, Mathf.Max(totalAltDir, rect.height));

        _scrollAcoes = GUI.BeginScrollView(viewportDir, _scrollAcoes, contentDir, false, false);

        for (int ai = 0; ai < acoes.Count; ai++)
        {
            var item = acoes[ai];
            bool hover = (_hoverAcao == ai);

            Rect itemRect = new Rect(xDir, ai * AltLinhaAcao, largDir, AltLinhaAcao);

            if (hover) EditorGUI.DrawRect(itemRect, CorHover);

            EditorGUI.DrawRect(new Rect(itemRect.x + 8, itemRect.yMax - 1, itemRect.width - 16, 1),
                               new Color(0.22f, 0.22f, 0.27f));

            Rect bolinha = new Rect(itemRect.x + 10, itemRect.y + 10, 22, 22);
            EditorGUI.DrawRect(bolinha, item.Vis.CorFundo);

            var iconName = EditorGUIUtility.isProSkin ? item.Vis.IconeUnity : item.Vis.IconeUnity.Replace("d_", "");
            var iconTex = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            if (iconTex != null)
                GUI.DrawTexture(bolinha, iconTex, ScaleMode.ScaleToFit);
            else
                GUI.Label(bolinha, item.Vis.IconeTexto, new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = item.Vis.CorPrincipal }
                });

            var estiloNome = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = hover ? Color.white : new Color(0.88f, 0.88f, 0.92f) }
            };
            GUI.Label(new Rect(itemRect.x + 40, itemRect.y + 5, itemRect.width - 50, 18),
                      item.Nome, estiloNome);

            if (!string.IsNullOrEmpty(item.Descricao))
            {
                var estiloDesc = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.50f, 0.50f, 0.58f) }
                };
                GUI.Label(new Rect(itemRect.x + 40, itemRect.y + 22, itemRect.width - 50, 16),
                          item.Descricao, estiloDesc);
            }

            if (itemRect.Contains(ev.mousePosition))
            {
                if (_hoverAcao != ai) { _hoverAcao = ai; editorWindow.Repaint(); }
                if (ev.type == EventType.MouseDown && ev.button == 0)
                {
                    AdicionarAcao(item.Tipo);
                    editorWindow.Close();
                    ev.Use();
                }
            }
            else if (_hoverAcao == ai)
            {
                _hoverAcao = -1;
                editorWindow.Repaint();
            }
        }

        GUI.EndScrollView();

        if (ev.type == EventType.MouseMove) editorWindow.Repaint();
    }

    private void AdicionarAcao(Type tipo)
    {
        _so.Update();
        int novoIndex = _acoesProp.arraySize;
        _acoesProp.arraySize++;
        _acoesProp.GetArrayElementAtIndex(novoIndex).managedReferenceValue =
            Activator.CreateInstance(tipo);
        _so.ApplyModifiedProperties();
    }
}