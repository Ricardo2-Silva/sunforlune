using System.Collections;
using UnityEngine;

public enum CategoriaAcao
{
    Geral,
    Animacao,
    Movimento,
    Dano,
    Buff,
    Efeito,
    Projetil,
    Condicional,
    Exclusiva
}

[System.Serializable]
public abstract class AcaoAtaque
{
    [Header("Configuração Visual Customizada")]
    [Tooltip("Arraste uma Sprite aqui para substituir o ícone padrão desta ação no Editor.")]
    public Sprite IconeCustomizado;

    [Tooltip("Nome do ícone padrão da Unity (EditorGUIUtility.IconContent). Ex: 'console.erroricon' ou 'd_console.erroricon'.")]
    public string IconeUnity;

    /// <summary>
    /// Lógica principal da ação durante o ataque.
    /// </summary>
    public abstract IEnumerator Executar(ContextoAtaque ctx);

    // ─── Metadados para o Editor ──────────────────────────────────────────────

    public virtual string NomeExibicao => GetType().Name;

    public virtual string Descricao => "";

    public virtual CategoriaAcao Categoria => CategoriaAcao.Geral;

    // Define qual ícone aparecerá no menu e no card por padrão.
    // Pode ser um ícone interno da Unity ou o caminho de um Sprite seu (ex: "Assets/Icones/fogo.png")
    public virtual string CaminhoIcone => "";
}