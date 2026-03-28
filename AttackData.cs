using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class AttackData : ScriptableObject
{
    [Header("Identidade e Interface")]
    public string nomeAtaque;
    public Sprite icone;
    [TextArea(3, 10)] public string descricao;

    [Header("Parâmetros Lógicos")]
    public int habilidadeIndex;
    public int pontosPoder;
    public int precisao;
    public int prioridade;
    public bool sempreAcerta;
    public float cooldown; // Cooldown individual
    public float moveSpeed;
    public float moveDuration;
    public float damage;
    public float aoeRadius;

    [Header("Parâmetros IA")]
    public float idealDistance;
    public float alcanceMax;
    public int priority;

    [Header("Classificação")]
    public Tipo tipo;
    public Categoria categoria;
    public TipoInput tipoInput;
    public RequirimentoPostura postura;

    [Header("Efeitos Visuais")]
    public bool trailEmmiter = false;
    public TrailRenderer trail;
    public GameObject attackEffectPrefab;
    public GameObject trailRendererPrefab;//padronizar o nome

    public AttackExecutionType executionType;
    public bool aplicaKnockback;


    //_______________Efeitos de ataques _____________

    [Header("Effect Parameters")]
    public StatusEffectType effectType;
    public EffectStackType stackType;
    public int maxStacks = 1;
    

    [Header("Timing")]
    public float duration = 5f;     // -1 para efeitos permanentes
    public float tickRate = 1f;     // Frequência de aplicação do efeito (para DoTs/HoTs)

    [Header("Effect Values")]
    public float effectValue;       // Valor base do efeito (dano, cura, modificador)
    public float stackMultiplier;
    [Header("Visual Effects")]
    public Color tintColor = Color.white;
    public GameObject effectPrefab;
    public ParticleSystem particles;

    [Header("Status Effects Aplicados")]
    public List<StatusEffect> statusEffectsToApply;
    [Range(0, 1)]
    public float effectChance = 1f;

    //_________________________________________________

    [Header("Configuração de conjuração")]
    public bool isChanneledAttack = false; // This is a PROPERTY of the attack, not state
    public float castTime = 2.0f; // Tempo necessário para conjurar o feitiço
    public GameObject objetoParticula;
    public GameObject objetoParticula2;
    public ParticleSystem attackParticle;
    public ParticleSystem attackParticle2;

    [Header("Sound Effects")]
    public AudioClip applySound;
    public AudioClip tickSound;
    public AudioClip removeSound;

    private void OnValidate()
    {
        nomeAtaque = name;
    }

    // These methods now take an AttackInstance parameter to store state
    public abstract void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance);
    public abstract IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance);
}

public enum AttackExecutionType
{
    Instant,
    Coroutine
}

public enum RequirimentoPostura
{
    longoAlcance,
    curtoAlcance,
    autoCura,
    particula,
    conjurar,
    status
}

public enum TipoInput
{
    Instantanea,
    Conjuracao,
    Multiplos
}

public enum Categoria
{
    Fisico,
    Especial,
    Status
}

public enum Status
{
    Ok,
    Sleep,
    Burn,
    Poison,
    Paralyzed,
    Frozen,
    Fainted
}

[System.Serializable]
public class LevelAttackEntry
{
    public int level;
    public AttackData attack;
}