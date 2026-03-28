using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public enum StatusEffectType
{
    // Primary Status
    None,
    Poison,
    Burn,
    Paralysis,
    Sleep,
    Freeze,

    // Secondary Status
    Confusion,
    Flinch,
    Infatuation,
    Curse,

    // Field Effects
    Sandstorm,
    Rain,
    HarshSunlight,
    Hail,
    Heal,

    // Stat Modifiers
    AttackUp,
    AttackDown,
    DefenseUp,
    DefenseDown,
    SpeedUp,
    SpeedDown,
    AccuracyUp,
    AccuracyDown,
    EvasionUp,
    EvasionDown
}

public enum EffectStackType
{
    None,           // Não acumula, apenas reseta duração
    Stack,          // Acumula até um limite
    Refresh,        // Atualiza duração
    Independent     // Aplica independentemente
}

[CreateAssetMenu(fileName = "New Status Effect", menuName = "Pokemon/Status Effect")]
public class StatusEffect : ScriptableObject
{
    [Header("Identity")]
    public string effectName;
    public Sprite icon;
    [TextArea(3, 5)]
    public string description;

    [Header("Effect Parameters")]
    public StatusEffectType effectType;
    public EffectStackType stackType;
    public int maxStacks = 1;

    [Header("Timing")]
    public float duration = 5f;     // -1 para efeitos permanentes
    public float tickRate = 1f;     // Frequência de aplicação do efeito (para DoTs/HoTs)

    [Header("Effect Values")]
    public float effectValue;       // Valor base do efeito (dano, cura, modificador)
    public float stackMultiplier;   // Multiplicador por stack

    [Header("Visual Effects")]
    public Color tintColor = Color.white;
    public GameObject effectPrefab;
    public ParticleSystem particles;

    [Header("Sound Effects")]
    public AudioClip applySound;
    public AudioClip tickSound;
    public AudioClip removeSound;
}
