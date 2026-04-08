using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ENUMS DE STATS E NATUREZA
// Ficam aqui para que qualquer script do projeto possa importar sem dependência
// circular entre Mon, NaturezaData e outros sistemas
// ─────────────────────────────────────────────────────────────────────────────

public enum Stat
{
    HP,
    Ataque,
    Defesa,
    SpAtaque,
    SpDefesa,
    Velocidade
}

public enum Natureza
{
    // Neutras — sem modificador de stat
    Hardy,      // Ataque / Ataque   (neutro)
    Docile,     // Defesa / Defesa
    Serious,    // SpVelocidade / SpVelocidade  — não existe, mas mantém o padrão
    Bashful,    // SpAtaque / SpAtaque
    Quirky,     // SpDefesa / SpDefesa

    // +Ataque
    Lonely,     // +Ataque   -Defesa
    Brave,      // +Ataque   -Velocidade
    Adamant,    // +Ataque   -SpAtaque
    Naughty,    // +Ataque   -SpDefesa

    // +Defesa
    Bold,       // +Defesa   -Ataque
    Relaxed,    // +Defesa   -Velocidade
    Impish,     // +Defesa   -SpAtaque
    Lax,        // +Defesa   -SpDefesa

    // +Velocidade
    Timid,      // +Velocidade -Ataque
    Hasty,      // +Velocidade -Defesa
    Jolly,      // +Velocidade -SpAtaque
    Naive,      // +Velocidade -SpDefesa

    // +SpAtaque
    Modest,     // +SpAtaque -Ataque
    Mild,       // +SpAtaque -Defesa
    Quiet,      // +SpAtaque -Velocidade
    Rash,       // +SpAtaque -SpDefesa

    // +SpDefesa
    Calm,       // +SpDefesa -Ataque
    Gentle,     // +SpDefesa -Defesa
    Sassy,      // +SpDefesa -Velocidade
    Careful     // +SpDefesa -SpAtaque
}

// ─────────────────────────────────────────────────────────────────────────────
// Comportamentos para futura IA de equipe (campo 3)
// Não usado ainda — declarado aqui para reservar o espaço semântico
// ─────────────────────────────────────────────────────────────────────────────
public enum ComportamentoEquipe
{
    Neutro,
    Brincalhao,
    Timido,
    Agressivo,
    Curioso,
    Afetivo
}

/// <summary>
/// Ponto único de consulta para todos os efeitos de natureza.
/// Classe estática — sem instância, sem MonoBehaviour.
///
/// Consumidores:
///   Mon.cs          → GetModificadorStat() nos stats dinâmicos
///   WildPokemonAI   → GetCourage() para derivar bravura automaticamente
///   StatusEffectInstance → GetModificadorStat() nos ticks de DoT (futuro)
///   IA de equipe    → GetComportamentoEquipe() (futuro)
/// </summary>
public static class NaturezaData
{
    /// <summary>
    /// Retorna o multiplicador de stat pela natureza.
    /// 1.1f = aumentado | 0.9f = reduzido | 1.0f = neutro
    /// </summary>
    public static float GetModificadorStat(Natureza natureza, Stat stat)
    {
        switch (natureza)
        {
            // +Ataque
            case Natureza.Lonely:
            case Natureza.Brave:
            case Natureza.Adamant:
            case Natureza.Naughty:
                if (stat == Stat.Ataque) return 1.1f;
                if (stat == Stat.Defesa && natureza == Natureza.Bold) return 0.9f;
                if (stat == Stat.Velocidade && natureza == Natureza.Brave) return 0.9f;
                if (stat == Stat.SpAtaque && natureza == Natureza.Adamant) return 0.9f;
                if (stat == Stat.SpDefesa && natureza == Natureza.Naughty) return 0.9f;
                if (stat == Stat.Defesa && natureza == Natureza.Lonely) return 0.9f;
                return 1.0f;

            // +Defesa
            case Natureza.Bold:
            case Natureza.Relaxed:
            case Natureza.Impish:
            case Natureza.Lax:
                if (stat == Stat.Defesa) return 1.1f;
                if (stat == Stat.Ataque && natureza == Natureza.Bold) return 0.9f;
                if (stat == Stat.Velocidade && natureza == Natureza.Relaxed) return 0.9f;
                if (stat == Stat.SpAtaque && natureza == Natureza.Impish) return 0.9f;
                if (stat == Stat.SpDefesa && natureza == Natureza.Lax) return 0.9f;
                return 1.0f;

            // +Velocidade
            case Natureza.Timid:
            case Natureza.Hasty:
            case Natureza.Jolly:
            case Natureza.Naive:
                if (stat == Stat.Velocidade) return 1.1f;
                if (stat == Stat.Ataque && natureza == Natureza.Timid) return 0.9f;
                if (stat == Stat.Defesa && natureza == Natureza.Hasty) return 0.9f;
                if (stat == Stat.SpAtaque && natureza == Natureza.Jolly) return 0.9f;
                if (stat == Stat.SpDefesa && natureza == Natureza.Naive) return 0.9f;
                return 1.0f;

            // +SpAtaque
            case Natureza.Modest:
            case Natureza.Mild:
            case Natureza.Quiet:
            case Natureza.Rash:
                if (stat == Stat.SpAtaque) return 1.1f;
                if (stat == Stat.Ataque && natureza == Natureza.Modest) return 0.9f;
                if (stat == Stat.Defesa && natureza == Natureza.Mild) return 0.9f;
                if (stat == Stat.Velocidade && natureza == Natureza.Quiet) return 0.9f;
                if (stat == Stat.SpDefesa && natureza == Natureza.Rash) return 0.9f;
                return 1.0f;

            // +SpDefesa
            case Natureza.Calm:
            case Natureza.Gentle:
            case Natureza.Sassy:
            case Natureza.Careful:
                if (stat == Stat.SpDefesa) return 1.1f;
                if (stat == Stat.Ataque && natureza == Natureza.Calm) return 0.9f;
                if (stat == Stat.Defesa && natureza == Natureza.Gentle) return 0.9f;
                if (stat == Stat.Velocidade && natureza == Natureza.Sassy) return 0.9f;
                if (stat == Stat.SpAtaque && natureza == Natureza.Careful) return 0.9f;
                return 1.0f;

            // Neutras
            default: return 1.0f;
        }
    }

    /// <summary>
    /// Retorna o courage derivado da natureza para WildPokemonAI.
    /// Elimina configuração manual de courage no inspector por Pokémon.
    /// 0 = foge sempre | 1 = luta sempre
    /// </summary>
    public static float GetCourage(Natureza natureza)
    {
        switch (natureza)
        {
            // Muito agressivo
            case Natureza.Brave:
            case Natureza.Adamant:
            case Natureza.Naughty:
            case Natureza.Lonely: return 0.85f;

            // Levemente agressivo
            case Natureza.Rash:
            case Natureza.Hasty: return 0.70f;

            // Neutro
            case Natureza.Hardy:
            case Natureza.Docile:
            case Natureza.Serious:
            case Natureza.Bashful:
            case Natureza.Quirky: return 0.50f;

            // Levemente cauteloso
            case Natureza.Bold:
            case Natureza.Impish:
            case Natureza.Jolly:
            case Natureza.Modest: return 0.40f;

            // Muito cauteloso / covarde
            case Natureza.Timid:
            case Natureza.Calm:
            case Natureza.Gentle:
            case Natureza.Careful: return 0.15f;

            // Outros
            default: return 0.50f;
        }
    }

    /// <summary>
    /// Retorna o perfil comportamental para futura IA de equipe.
    /// Não conectado a nenhum sistema ainda — reservado para quando a IA de equipe for criada.
    /// </summary>
    public static ComportamentoEquipe GetComportamentoEquipe(Natureza natureza)
    {
        switch (natureza)
        {
            case Natureza.Jolly:
            case Natureza.Naive: return ComportamentoEquipe.Brincalhao;

            case Natureza.Timid:
            case Natureza.Gentle: return ComportamentoEquipe.Timido;

            case Natureza.Brave:
            case Natureza.Naughty:
            case Natureza.Lonely: return ComportamentoEquipe.Agressivo;

            case Natureza.Calm:
            case Natureza.Careful: return ComportamentoEquipe.Afetivo;

            case Natureza.Hasty:
            case Natureza.Rash: return ComportamentoEquipe.Curioso;

            default: return ComportamentoEquipe.Neutro;
        }
    }
}