using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Novo Pokemon", menuName = "Pokemon/Novo Pokemon")]
public class DadosPokemon : ScriptableObject
{
    [Header("Dados Principais")]
    [SerializeField] private int numPokedexNacional;
    [SerializeField] private string nome;
    [SerializeField] private Tipo primeiroTipo;
    [SerializeField] private Tipo segundoTipo;
    [SerializeField] private int grupoOvos;
    [SerializeField] private bool evolui;
    [SerializeField] private string habOculta;
    [SerializeField] private int genero;
    [SerializeField] private DadosPokemon proxEvolucao;
    [SerializeField] private Sprite portrait;
    [TextArea(15, 20)][SerializeField] private string descricao;

    [Header("Dimensões (para VFX / offsets)")]
    [Tooltip("Altura aproximada do sprite em pixels.")]
    [SerializeField] private int pixelHeight = 64;

    [Header("Base Stats")]
    [SerializeField] private int maxHp;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int spAttack;
    [SerializeField] private int spDefense;
    [SerializeField] private int speed;

    [Header("Experiência e Crescimento")]
    [SerializeField] private int expYield;
    [SerializeField] private GrowthRate growthRate;
    [SerializeField] private int catchRate = 255;

    [Header("Ataques aprendidos por nível")]
    public List<LevelAttackEntry> attackDefinitions;

    [Header("Evoluções")]
    [SerializeField] private List<Evolucao> evolucoes;

    private void OnValidate()
    {
        nome = name;
        if (pixelHeight < 1) pixelHeight = 1;
    }

    // Propriedades
    public int NumDexNacional => numPokedexNacional;
    public string Nome => nome;
    public int GrupoOvos => grupoOvos;
    public bool Evolui => evolui;
    public int Genero => genero;
    public DadosPokemon ProxEvolucao => proxEvolucao;
    public Sprite Portrait => portrait;
    public string Descricao => descricao;
    public Tipo Tipo1 => primeiroTipo;
    public Tipo Tipo2 => segundoTipo;
    public int PixelHeight => pixelHeight;
    public int MaxHp => maxHp;
    public int Attack => attack;
    public int SpAttack => spAttack;
    public int Defense => defense;
    public int SpDefense => spDefense;
    public int Speed => speed;
    public int ExpYield => expYield;
    public GrowthRate GrowthRate => growthRate;
    public int CatchRate => catchRate;
    public List<LevelAttackEntry> AttackDefinitions => attackDefinitions;
    public List<Evolucao> Evolucoes => evolucoes;

    public int GetExpForLevel(int nivelAtual)
    {
        switch (growthRate)
        {
            case GrowthRate.Rapido: return 4 * (nivelAtual * nivelAtual * nivelAtual) / 5;
            case GrowthRate.MedioRapido: return nivelAtual * nivelAtual * nivelAtual;
            default: return -1;
        }
    }
}
public enum GrowthRate { Rapido, MedioRapido }

[System.Serializable]
public class Evolucao
{
    public DadosPokemon evoluiPara;
    public int nivelRequerido;
    //public ItemBase itemRequerido; // se quiser evoluir por item
    //public Troca troca;
}

public enum Tipo
{
    Aco,
    Agua,
    Dragao,
    Eletrico,
    Escuro,
    Fada,
    Fantasma,
    Fogo,
    Gelo,
    Grama,
    Inseto,
    Lutador,
    Nulo,
    Normal,
    Pedra,
    Psiquico,
    Terra,
    Venenoso,
    Voador
}
public enum MetodoEvolucao
{
    Nivel,
    Item,
    Troca
}
public class TypeChart
{
    static float[][] chart =
    {
         //                      Nor   Fogo  Água  Elet  Gra   Gelo  Luta  Ven  Terra  Voa   Psy   Ins   Roc  Fant   Dra   Esc   Aço   Fada
        /*Normal*/  new float[] {1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 0,    1f,   1f,   0.5f, 1f},
        /*Fogo*/    new float[] {1f,   0.5f, 0.5f, 1f,   2f,   2f,   1f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   0.5f, 1f,   2f,   1f},
        /*Water*/   new float[] {1f,   2f,   0.5f, 1f,   0.5f, 1f,   1f,   1f,   2f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f,   1f,   1f},
        /*Electric*/new float[] {1f,   1f,   2f,   0.5f, 0.5f, 1f,   1f,   1f,   0f,   2f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   1f},
        /*Grass*/   new float[] {1f,   0.5f, 2f,   1f,   0.5f, 1f,   1f,   0.5f, 2f,   0.5f, 1f,   0.5f, 2f,   1f,   0.5f, 1f,   0.5f, 1f},
        /*Ice*/     new float[] {1f,   0.5f, 0.5f, 1f,   2f,   0.5f, 1f,   1f,   2f,   2f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f},
        /*Fighting*/new float[] {2f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f,   0.5f, 0.5f, 0.5f, 2f,   0f,   1f,   2f,   2f,   0.5f},
        /*Poison*/  new float[] {1f,   1f,   1f,   1f,   2f,   1f,   1f,   0.5f, 0.5f, 1f,   1f,   1f,   0.5f, 0.5f, 1f,   1f,   0f,   2f},
        /*Ground*/  new float[] {1f,   2f,   1f,   2f,   0.5f, 1f,   1f,   2f,   1f,   0f,   1f,   0.5f, 2f,   1f,   1f,   1f,   2f,   1f},
        /*Flying*/  new float[] {1f,   1f,   1f,   0.5f, 2f,   1f,   2f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   0.5f, 1f},
        /*Psychic*/ new float[] {1f,   1f,   1f,   1f,   1f,   1f,   2f,   2f,   1f,   1f,   0.5f, 1f,   1f,   1f,   1f,   0f,   0.5f, 1f},
        /*Bug*/     new float[] {1f,   0.5f, 1f,   1f,   2f,   1f,   0.5f, 0.5f, 1f,   0.5f, 2f,   1f,   1f,   0.5f, 1f,   2f,   0.5f, 0.5f},
        /*Rock*/    new float[] {1f,   2f,   1f,   1f,   1f,   2f,   0.5f, 1f,   0.5f, 2f,   1f,   2f,   1f,   1f,   1f,   1f,   0.5f, 1f},
        /*Ghost*/   new float[] {0f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   2f,   1f,   0.5f, 1f,   1f},
        /*Dragon*/  new float[] {1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 0f},
        /*Dark*/    new float[] {1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   1f,   2f,   1f,   1f,   2f,   1f,   0.5f, 1f,   0.5f},
        /*Steel*/   new float[] {1f,   0.5f, 0.5f, 0.5f, 1f,   2f,   1f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   0.5f, 2f},
        /*Fairy*/   new float[] {1f,   0.5f, 1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   1f,   1f,   1f,   2f,   2f,   0.5f, 1f}
    };

    public static float GetEffectiveness(Tipo attackType, Tipo defendType)
    {
        if (attackType == Tipo.Nulo || defendType == Tipo.Nulo)
            return 1f;

        int row = (int)attackType - 1;
        int col = (int)defendType - 1;

        return chart[row][col];
    }
}

