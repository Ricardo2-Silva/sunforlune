using System.Collections;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE ANIMAÇÃO
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoTocarAnimacaoRSA : AcaoAtaque
{
    [Tooltip("Nome exato da animação configurada no RetroSpriteAnimator / PokemonAnimatorController")]
    public string nomeAnimacao = "AtaqueBasico";

    public override string NomeExibicao => "Tocar Animação";
    public override string Descricao => $"RSA: \"{nomeAnimacao}\"";
    public override CategoriaAcao Categoria => CategoriaAcao.Animacao;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        ctx.Animador?.TocarAtaque(nomeAnimacao, ctx.Direcao);
        yield return null;
    }
}

[System.Serializable]
public class AcaoAguardarFimAnimacao : AcaoAtaque
{
    [Tooltip("Tempo máximo de espera (segurança contra animações que nunca terminam)")]
    public float timeoutSegundos = 3f;

    public override string NomeExibicao => "Aguardar Fim da Animação";
    public override string Descricao => $"Timeout: {timeoutSegundos}s";
    public override CategoriaAcao Categoria => CategoriaAcao.Animacao;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (ctx.Animador == null) yield break;

        float t = 0f;
        while (!ctx.Animador.EstaEmAtaque() == false && t < timeoutSegundos)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE MOVIMENTO
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoInvestida : AcaoAtaque
{
    [Tooltip("Força do impulso em ForceMode2D.Impulse")]
    public float forca = 15f;
    [Tooltip("Duração do dash em segundos")]
    public float duracao = 0.2f;

    public override string NomeExibicao => "Investida (Dash)";
    public override string Descricao => $"Força {forca} / {duracao}s";
    public override CategoriaAcao Categoria => CategoriaAcao.Movimento;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (ctx.RbAtacante == null) yield break;

        ctx.RbAtacante.velocity = Vector2.zero;
        ctx.RbAtacante.AddForce(ctx.Direcao.normalized * forca, ForceMode2D.Impulse);

        float t = 0f;
        while (t < duracao)
        {
            if (ctx.CancelarSequencia) break;
            t += Time.deltaTime;
            yield return null;
        }

        ctx.RbAtacante.velocity = Vector2.zero;
    }
}

[System.Serializable]
public class AcaoModificarVelocidade : AcaoAtaque
{
    [Tooltip("Multiplicador de velocidade aplicado ao Mon")]
    public float multiplicador = 1.5f;
    [Tooltip("Duração do buff de velocidade em segundos")]
    public float duracao = 15f;

    public override string NomeExibicao => "Modificar Velocidade";
    public override string Descricao => $"x{multiplicador} por {duracao}s";
    public override CategoriaAcao Categoria => CategoriaAcao.Buff;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (ctx.Atacante == null) yield break;

        // Aplica o multiplicador (Mon precisa expor velocidade base)
        float velocidadeOriginal = ctx.Atacante.velocidadeBase;
        ctx.Atacante.velocidadeBase *= multiplicador;

        float t = 0f;
        while (t < duracao && !ctx.CancelarSequencia)
        {
            t += Time.deltaTime;
            yield return null;
        }

        ctx.Atacante.velocidadeBase = velocidadeOriginal;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE TEMPO
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoEsperarTempo : AcaoAtaque
{
    public float tempoSegundos = 0.1f;

    public override string NomeExibicao => "Esperar";
    public override string Descricao => $"{tempoSegundos}s";
    public override CategoriaAcao Categoria => CategoriaAcao.Geral;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        float t = 0f;
        while (t < tempoSegundos)
        {
            if (ctx.CancelarSequencia) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE PROJÉTIL
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoDispararProjetil : AcaoAtaque
{
    [Tooltip("Prefab do projétil a ser instanciado")]
    public GameObject prefabProjetil;
    public float velocidade = 12f;
    public float dano = 10f;
    [Tooltip("Offset de spawn a partir do centro do atacante")]
    public float offsetSpawn = 0.5f;

    public override string NomeExibicao => "Disparar Projétil";
    public override string Descricao => $"Dano {dano} / Vel {velocidade}";
    public override CategoriaAcao Categoria => CategoriaAcao.Projetil;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (prefabProjetil == null || ctx.TransformRaiz == null) yield break;

        Vector3 posSpawn = ctx.TransformRaiz.position + (Vector3)(ctx.Direcao.normalized * offsetSpawn);
        GameObject obj = Object.Instantiate(prefabProjetil, posSpawn, Quaternion.identity);

        Disparo disparo = obj.GetComponent<Disparo>();
        disparo?.Initialize(ctx.Direcao.normalized, dano, velocidade, ctx.Atacante);

        yield return null;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE DANO
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoHitboxAtiva : AcaoAtaque
{
    [Tooltip("Nome do objeto filho que contém a hitbox")]
    public string nomeHitbox = "MonHitBox";
    public float duracaoAtiva = 0.15f;

    public override string NomeExibicao => "Ativar Hitbox";
    public override string Descricao => $"\"{nomeHitbox}\" por {duracaoAtiva}s";
    public override CategoriaAcao Categoria => CategoriaAcao.Dano;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (ctx.TransformRaiz == null) yield break;

        Transform hitboxT = ctx.TransformRaiz.Find(nomeHitbox);
        if (hitboxT == null) yield break;

        BoxCollider2D col = hitboxT.GetComponent<BoxCollider2D>();
        if (col == null) yield break;

        col.enabled = true;
        yield return new WaitForSeconds(duracaoAtiva);
        col.enabled = false;
    }
}

[System.Serializable]
public class AcaoDanoEmArea : AcaoAtaque
{
    public float raio = 2f;
    public float dano = 20f;
    public LayerMask camadaAlvos;

    public override string NomeExibicao => "Dano em Área (AoE)";
    public override string Descricao => $"Raio {raio} / Dano {dano}";
    public override CategoriaAcao Categoria => CategoriaAcao.Dano;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (ctx.TransformRaiz == null) yield break;

        Collider2D[] atingidos = Physics2D.OverlapCircleAll(ctx.TransformRaiz.position, raio, camadaAlvos);
        foreach (var col in atingidos)
        {
            if (col.transform.root == ctx.TransformRaiz.root) continue; // Ignora a si mesmo

            SaudePokemon saude = col.GetComponentInParent<SaudePokemon>();
            //saude?.ReceberDano(dano);
        }

        yield return null;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AÇÕES DE EFEITO VISUAL
// ═══════════════════════════════════════════════════════════════════════════════

[System.Serializable]
public class AcaoSpawnEfeito : AcaoAtaque
{
    public GameObject prefabEfeito;
    public bool seguirAtacante = false;
    [Tooltip("Destruir após X segundos. 0 = não destroi automaticamente")]
    public float tempoVida = 2f;

    public override string NomeExibicao => "Spawn de Efeito";
    public override string Descricao => prefabEfeito != null ? prefabEfeito.name : "Nenhum prefab";
    public override CategoriaAcao Categoria => CategoriaAcao.Efeito;

    public override IEnumerator Executar(ContextoAtaque ctx)
    {
        if (prefabEfeito == null || ctx.TransformRaiz == null) yield break;

        Transform pai = seguirAtacante ? ctx.TransformRaiz : null;
        GameObject obj = Object.Instantiate(prefabEfeito, ctx.TransformRaiz.position, Quaternion.identity, pai);

        if (tempoVida > 0f)
            Object.Destroy(obj, tempoVida);

        yield return null;
    }
}