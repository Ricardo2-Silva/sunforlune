using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verifica colisão e aplica dano e efeito de impacto (sprite animada ou partículas).
/// </summary>
public class VerificadorColisao : MonoBehaviour
{

    public GameObject hitImpactPrefab; // Prefab do efeito de impacto (pode ser ParticleSystem ou animação)
    public float hitImpactDuration = 0.2f;
    [SerializeField] private float forcaKnockback = 2f;
    [SerializeField] private Mon attacker;
    [SerializeField] private PerformCombat moveSender;

    void Start()
    {
        attacker = GetComponentInParent<Mon>();
        moveSender = transform.parent.GetComponentInChildren<PerformCombat>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("MonHurtBox") || collision.gameObject.CompareTag("OponenteHurtBox"))
        {
            MonHurtBox hurtBox = collision.GetComponentInChildren<MonHurtBox>();

            if (hurtBox != null)
            {
                // Direção do ataque (de mim para o inimigo)
                Vector2 attackDirection = (transform.position - collision.transform.position).normalized;

                // Aplica dano e knockback
                hurtBox.TakeDamage(forcaKnockback, attackDirection, attacker, moveSender.LastUsedAttack.data);

                // Spawn de efeito de impacto universal (suporta partícula ou animação)
                if (hitImpactPrefab != null)
                {
                    GameObject impactEffect = Instantiate(hitImpactPrefab, collision.transform.position, Quaternion.identity, collision.transform);

                    float destroyDelay = hitImpactDuration; // padrão

                    // Caso seja uma ParticleSystem
                    ParticleSystem ps = impactEffect.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Play();
                        var main = ps.main;
                        destroyDelay = main.duration + main.startLifetime.constantMax;
                    }

                    // Caso seja uma animação
                    Animator animator = impactEffect.GetComponent<Animator>();
                    if (animator != null)
                    {
                        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
                        if (ac != null && ac.animationClips.Length > 0)
                        {
                            destroyDelay = Mathf.Max(destroyDelay, ac.animationClips[0].length);
                        }
                    }

                    Destroy(impactEffect, destroyDelay);
                }
            }
        }
    }
}