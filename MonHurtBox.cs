using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonHurtBox : MonoBehaviour
{
    [Header("Status do Personagem")]
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 1f;

    [Header("Efeitos de Dano")]
    public SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f;

    [Header("Knockback")]
    public float knockbackDuration = 0.2f;
    public Rigidbody2D rb;
    public int knockbackCount = 0;
    public int knockbackThreshold = 3;
    public float knockbackCountResetTime = 2f;
    private float knockbackCountTimer = 0f;
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;
    public bool isBeingKnockedBack = false;

    // Eventos para integração com PerformCombat e UI
    public event System.Action OnTookDamage;
    public event System.Action OnBecameInvulnerable;
    public event System.Action OnRecoveredFromHurt;

    [Header("Configurações de HitPause")]
    public float hitPauseDuration = 0.1f;

    [Header("Screen Effects")]
    // CORREÇÃO: Removido "public Camera mainCamera" — agora usa CameraFollowManager
    public float hitShakeMagnitude = 0.1f;
    public float hitShakeDuration = 0.2f;

    [Header("Hurt")]
    public bool isHurting;
    private float hurtDuration = 0.5f;
    private float hurtTimer = 0.0f;

    private bool debilitado;
    public SaudePokemon vigor;

    public StatusEffectManager targetStatusManager;

    [Header("Animator")]
    public Animator anim;

    public Transform parentTransform;
    public int indiceSprite;

    void Awake()
    {
        parentTransform = transform.parent;
        FindSaudePokemon();
    }

    void Start()
    {

    }

    private void FindSaudePokemon()
    {
        vigor = GetComponent<SaudePokemon>();

        if (vigor == null && parentTransform != null)
        {
            vigor = parentTransform.GetComponent<SaudePokemon>();

            if (vigor == null)
            {
                vigor = parentTransform.GetComponentInChildren<SaudePokemon>();
            }
        }

        if (vigor == null)
        {
            Transform rootTransform = transform;
            while (rootTransform.parent != null)
            {
                rootTransform = rootTransform.parent;
            }
            vigor = rootTransform.GetComponentInChildren<SaudePokemon>();
        }

        if (vigor == null)
        {
            Debug.LogWarning("SaudePokemon não encontrado para " + gameObject.name);
        }
    }

    void Update()
    {
        Ferido(parentTransform.position);

        if (knockbackCount > 0)
        {
            knockbackCountTimer -= Time.deltaTime;
            if (knockbackCountTimer <= 0)
                knockbackCount = 0;
        }
    }

    void FixedUpdate()
    {
        if (knockbackTimer > 0)
        {
            isBeingKnockedBack = true;
            rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            knockbackTimer -= Time.fixedDeltaTime;

            if (knockbackTimer <= 0)
            {
                knockbackVelocity = Vector2.zero;
                isBeingKnockedBack = false;
            }
        }
    }

    #region Sistema de Dano e Cura

    public void TakeDamage(float intensidade, Vector2 attackDirection, Mon attacker = null, AttackData skill = null, bool canKnockback = true)
    {
        if (debilitado) return;
        if (isInvulnerable) return;

        OnTookDamage?.Invoke();
        Debug.Log("OnTookDamage invocado");

        Debug.Log("Dano Recebido");
        if (attacker != null && skill != null)
            vigor.DanoReal(attacker, skill);

        foreach (var effect in skill.statusEffectsToApply)
        {
            if (Random.value <= skill.effectChance)
            {
                if (targetStatusManager != null)
                {
                    Debug.Log("Target Status Manager é VÁLIDO");
                    targetStatusManager.ApplyEffect(effect);
                }
            }
        }
        rb.velocity = Vector2.zero;

        if (vigor.pontosSaude > 0)
            EnterHurtState(attackDirection);

        StartCoroutine(HitFlashQuick(0.15f));

        bool ataqueAplicaKnockback = skill != null && skill.aplicaKnockback;

        if (ataqueAplicaKnockback || (knockbackCount >= knockbackThreshold))
        {
            if (!isInvulnerable)
            {
                Knockback(attackDirection, intensidade);
                StartCoroutine(InvulnerabilityFlash(invulnerabilityTime, flashDuration));
                isInvulnerable = true;
                OnBecameInvulnerable?.Invoke();
                knockbackCount = 0;
            }
        }
        else
        {
            knockbackCount++;
            knockbackCountTimer = knockbackCountResetTime;
        }

        // CORREÇÃO: Shake do sprite local + shake da câmera via CameraFollowManager
        StartCoroutine(HitShake());

        if (vigor.pontosSaude <= 0)
            Debilitar();
    }

    private IEnumerator HitFlashQuick(float flashTime)
    {
        if (spriteRenderer == null || spriteRenderer.material == null) yield break;
        spriteRenderer.material.SetInt("_Flash", 1);
        yield return new WaitForSeconds(flashTime);
        spriteRenderer.material.SetInt("_Flash", 0);
    }

    private IEnumerator HitFlash(float invulnerableTime = 1.0f, float flashInterval = 0.1f)
    {
        if (spriteRenderer == null) yield break;

        isInvulnerable = true;
        float timer = 0f;
        Color originalColor = spriteRenderer.color;
        Color transparent = new Color(1, 1, 1, 0.15f);

        bool showWhite = true;
        while (timer < invulnerableTime)
        {
            if (showWhite)
                spriteRenderer.color = new Color(1, 1, 0.8f, 1);
            else
                spriteRenderer.color = transparent;

            showWhite = !showWhite;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    private IEnumerator InvulnerabilityFlash(float invulnerableTime, float flashInterval)
    {
        isInvulnerable = true;
        float timer = 0f;
        Color originalColor = spriteRenderer.color;
        while (timer < invulnerableTime)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(flashInterval * 0.5f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashInterval * 0.5f);
            timer += flashInterval;
        }
        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    private IEnumerator HitPause()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitPauseDuration);
        Time.timeScale = originalTimeScale;
    }

    /// <summary>
    /// Shake local no sprite do Pokémon + dispara shake na câmera via CameraFollowManager.
    /// CORRIGIDO: Não manipula mais a câmera diretamente.
    /// </summary>
    IEnumerator HitShake()
    {
        // Shake local no sprite (mantido — funciona em localPosition sem conflito)
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < hitShakeDuration)
        {
            float x = Random.Range(-hitShakeMagnitude, hitShakeMagnitude);
            float y = Random.Range(-hitShakeMagnitude, hitShakeMagnitude);

            transform.localPosition = new Vector3(
                originalPosition.x + x,
                originalPosition.y + y,
                originalPosition.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;

        // Shake na câmera — agora centralizado via CameraFollowManager
        if (CameraFollowManager.Instance != null)
        {
            CameraFollowManager.Instance.TriggerShake(hitShakeMagnitude, hitShakeDuration);
        }
    }

    // REMOVIDO: método CameraShake() antigo — agora usa CameraFollowManager.TriggerShake()

    #endregion

    #region Sistema de Knockback

    public void Knockback(Vector2 attackDirection, float intensidade)
    {
        if (rb == null) return;

        knockbackVelocity = (-attackDirection) * intensidade;
        knockbackTimer = knockbackDuration;
    }

    #endregion

    #region Sistema de Hurt

    private void EnterHurtState(Vector2 attackDirection)
    {
        if (!isHurting)
        {
            hurtTimer = hurtDuration;
            isHurting = true;
            anim.SetBool("hurt", true);
            DirecaoAtaque(attackDirection);
        }
        else
        {
            hurtTimer = hurtDuration;
        }
    }

    private void Ferido(Vector2 attackDirection)
    {
        if (!isHurting) return;

        hurtTimer -= Time.deltaTime;
        if (hurtTimer <= 0)
        {
            isHurting = false;
            anim.SetBool("hurt", false);
            OnRecoveredFromHurt?.Invoke();
        }
    }

    private void Debilitar()
    {
        rb.velocity = Vector2.zero;
        debilitado = true;
        vigor.pontosSaude = 0;

        anim.SetBool("hurt", false);
        anim.SetBool("derrotado", true);

        Destroy(gameObject, 10f);
    }

    void DirecaoAtaque(Vector2 attackDirection)
    {
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;

        if (angle < 0) angle += 360;

        indiceSprite = Mathf.RoundToInt(angle / 45) % 8;

        if (anim.GetBool("hurt") == true)
        {
            switch (indiceSprite)
            {
                case 0:
                    anim.SetFloat("hurtX", 1f);
                    anim.SetFloat("hurtY", 0f);
                    break;
                case 1:
                    anim.SetFloat("hurtX", 1f);
                    anim.SetFloat("hurtY", 1f);
                    break;
                case 2:
                    anim.SetFloat("hurtX", 0f);
                    anim.SetFloat("hurtY", 1f);
                    break;
                case 3:
                    anim.SetFloat("hurtX", -1f);
                    anim.SetFloat("hurtY", 1f);
                    break;
                case 4:
                    anim.SetFloat("hurtX", -1f);
                    anim.SetFloat("hurtY", 0f);
                    break;
                case 5:
                    anim.SetFloat("hurtX", -1f);
                    anim.SetFloat("hurtY", -1f);
                    break;
                case 6:
                    anim.SetFloat("hurtX", 0f);
                    anim.SetFloat("hurtY", -1f);
                    break;
                case 7:
                    anim.SetFloat("hurtX", 1f);
                    anim.SetFloat("hurtY", -1f);
                    break;
            }
        }
    }

    #endregion
}