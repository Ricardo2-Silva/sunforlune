using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WildState { Idle, Alert, Fleeing, Combat, Defeated }

//[RequireComponent(typeof(Rigidbody2D))]
public class WildPokemonAI : MonoBehaviour
{
    [Header("Parâmetros Gerais")]
    public float moveSpeed = 2f;
    public float idleMoveDuration = 1f;
    public float idleWaitDuration = 2f;
    public float patrolObstacleRadius = 1f;
    public float avoidanceRadius = 0.65f;
    public LayerMask obstacleMask;

    [Header("Detecção e Estados")]
    public float alertRadius = 5f; // RAIO PRINCIPAL - onde toda batalha acontece
    public float targetDetectionRadius = 7f; // RAIO DE SEGURANÇA - manter target e voltar ao idle
    public LayerMask targetLayer;

    [Header("Fuga e Derrota")]
    public float fleeDuration = 2f;
    public float defeatedTime = 3f;
    public AudioClip fleeSound;
    public GameObject exclamarEffectPrefab;

    [Header("Perfil de Decisão")]
    [Range(0, 1)] public float courage = 0.5f; // 0=foge sempre, 1=luta sempre

    [Header("Debug - Estado Atual")]
    [SerializeField] private WildState state = WildState.Idle;
    [SerializeField] private bool aiActive = true;

    public Transform target;
    public Rigidbody2D rb;
    public Animator anim;
    public PerformCombat combatSystem;
    public PokemonEffectsHandler effectsHandler;

    private Coroutine stateRoutine;
    private bool wasDisabled = false;

    void Awake()
    {

    }

    void Start()
    {
        InitializeAI();
    }

    void OnEnable()
    {
        // Se foi reativado após desativação, reinicia a IA
        if (wasDisabled)
        {
            //Debug.Log($"[{gameObject.name}] WildPokemonAI reativado - reiniciando IA");
            RestartAI();
            wasDisabled = false;
        }
    }

    void OnDisable()
    {
        wasDisabled = true;
        StopAI();
    }

    /// <summary>
    /// Inicializa a IA pela primeira vez
    /// </summary>
    private void InitializeAI()
    {
        //Debug.Log($"[{gameObject.name}] Inicializando WildPokemonAI");
        aiActive = true;
        target = null;
        ChangeState(WildState.Idle);
    }

    /// <summary>
    /// Reinicia a IA após ter sido desativada
    /// </summary>
    public void RestartAI()
    {
        if (!gameObject.activeInHierarchy) return;

       // Debug.Log($"[{gameObject.name}] Reiniciando WildPokemonAI");

        // Para todas as corrotinas ativas
        StopAI();

        // Reinicializa completamente
        InitializeAI();
    }

    /// <summary>
    /// Para completamente a IA
    /// </summary>
    public void StopAI()
    {
        //Debug.Log($"[{gameObject.name}] Parando WildPokemonAI");

        aiActive = false;

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        // Para movimento
        if (rb != null)
            rb.velocity = Vector2.zero;

        // Reseta animações
        if (anim != null)
        {
            anim.SetBool("Chase", false);
            anim.SetBool("Attack", false);
            anim.SetBool("isMoving", false);
        }

        target = null;
    }

    void Update()
    {
        // Só executa se a IA estiver ativa
        if (!aiActive) return;

        // Primeira detecção de target
        if (target == null)
        {
            DetectNewTarget();
        }
        else
        {
            // Verifica se target ainda está no raio de segurança
            float distanceToTarget = Vector2.Distance(transform.position, target.position);
            if (distanceToTarget > targetDetectionRadius)
            {
                target = null;
                if (state != WildState.Idle)
                    ChangeState(WildState.Idle);
                return;
            }
        }

        // Transições de estado baseadas na presença e distância do target
        HandleStateTransitions();
    }

    void HandleStateTransitions()
    {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        bool inAlertRange = distanceToTarget <= alertRadius;

        switch (state)
        {
            case WildState.Idle:
                if (inAlertRange)
                    ChangeState(WildState.Alert);
                break;

            case WildState.Alert:
                if (!inAlertRange)
                    ChangeState(WildState.Idle);
                // Alert naturalmente progride para Combat ou Fleeing após decisão
                break;

            case WildState.Combat:
                if (!inAlertRange)
                    ChangeState(WildState.Idle);
                break;
        }
    }

    void DetectNewTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, alertRadius, targetLayer);
        float closest = float.MaxValue;
        Transform found = null;

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                found = hit.transform;
            }
        }
        target = found;
    }

    // ---------- STATE MACHINE ----------
    void ChangeState(WildState newState)
    {
        // Só muda estado se a IA estiver ativa
        if (!aiActive) return;

        //Debug.Log($"[{gameObject.name}] Mudando estado: {state} → {newState}");

        if (stateRoutine != null) StopCoroutine(stateRoutine);
        state = newState;

        switch (state)
        {
            case WildState.Idle:
                stateRoutine = StartCoroutine(PatrolRoutine());
                break;
            case WildState.Alert:
                stateRoutine = StartCoroutine(AlertRoutine());
                break;
            case WildState.Fleeing:
                stateRoutine = StartCoroutine(FleeRoutine());
                break;
            case WildState.Combat:
                stateRoutine = StartCoroutine(CombatRoutine());
                // --- NOVO: Ativa UI do inimigo ao entrar em combate com o jogador ---
                if (target != null && target.CompareTag("Player"))
                {
                    TargetableEntity selfTargetable = GetComponent<TargetableEntity>();
                    if (selfTargetable != null && TargetSelectionManager.Instance != null)
                    {
                        selfTargetable.EnterCombatWithPlayer();
                        TargetSelectionManager.Instance.AutoSelectTarget(selfTargetable);
                    }
                }
                // -----------------------------------------------------------
                break;
            case WildState.Defeated:
                stateRoutine = StartCoroutine(DefeatedRoutine());
                break;
        }
    }

    // ---------- IDLE (patrulha aleatória com desvio de obstáculos) ----------
    IEnumerator PatrolRoutine()
    {
       // Debug.Log($"[{gameObject.name}] Entrada na Patrol Routine");
        if (anim != null)
        {
            anim.SetBool("Chase", false);
            anim.SetBool("Attack", false);
        }

        while (state == WildState.Idle && aiActive)
        {
            // Anda em direção aleatória por um tempo
            Vector2 randDir = Random.insideUnitCircle.normalized;
            float timer = 0f;

            if (anim != null)
                anim.SetBool("isMoving", true);

            Vector2 safeDir = ObstacleDetectionUtils.GetAvoidanceDirection(
                    transform, randDir, patrolObstacleRadius, avoidanceRadius, obstacleMask
                );

            while (timer < idleMoveDuration && state == WildState.Idle && aiActive)
            {
                if (rb != null)
                    rb.velocity = safeDir * moveSpeed;

                if (anim != null)
                {
                    anim.SetFloat("andarX", safeDir.x);
                    anim.SetFloat("andarY", safeDir.y);
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // CORREÇÃO: Para movimento e vai para IDLE quando detecta target
            if (rb != null)
                rb.velocity = Vector2.zero;

            if (anim != null)
            {
                anim.SetBool("isMoving", false);
                anim.SetFloat("dirOciosaX", safeDir.x);
                anim.SetFloat("dirOciosaY", safeDir.y);
            }

            // Se detectou target durante movimento, para imediatamente
            if (state != WildState.Idle || !aiActive) yield break;

            // Espera parado por um tempo
            yield return new WaitForSeconds(idleWaitDuration);
        }
    }

    // ---------- ALERT (exclamação, decisão entre fugir/lutar) ----------
    IEnumerator AlertRoutine()
    {
       // Debug.Log($"[{gameObject.name}] Entrada na Alert Routine");

        // CORREÇÃO: Para completamente e vai para animação idle
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool("isMoving", false);
            anim.SetBool("Chase", false);
            anim.SetBool("Attack", false);
        }

        // Efeito de exclamação
        if (effectsHandler && exclamarEffectPrefab)
            effectsHandler.ShowAlertEffect(exclamarEffectPrefab);

        float decisionTime = 2f;
        float timer = 0f;

        while (timer < decisionTime && state == WildState.Alert && aiActive)
        {
            // Observa o adversário
            FaceTarget();
            timer += Time.deltaTime;
            yield return null;
        }

        if (state == WildState.Alert && aiActive) // Só decide se ainda está em Alert
        {
            // Decide: fugir ou lutar
            if (Random.value > courage)
                ChangeState(WildState.Fleeing);
            else
                ChangeState(WildState.Combat);
        }
    }

    // ---------- FUGA (direção oposta ao alvo, fade, som) ----------
    IEnumerator FleeRoutine()
    {
        if (effectsHandler) effectsHandler.PlayFleeSound(fleeSound);
        if (effectsHandler) effectsHandler.StartFade();

        float timer = 0f;
        while (timer < fleeDuration && state == WildState.Fleeing && aiActive)
        {
            if (target)
            {
                Vector2 fleeDir = (transform.position - target.position).normalized;
                Vector2 safeDir = ObstacleDetectionUtils.GetAvoidanceDirection(
                    transform, fleeDir, patrolObstacleRadius, avoidanceRadius, obstacleMask
                );

                if (rb != null)
                    rb.velocity = safeDir * moveSpeed * 1.5f;

                if (anim != null)
                {
                    anim.SetFloat("andarX", safeDir.x);
                    anim.SetFloat("andarY", safeDir.y);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (aiActive)
            ChangeState(WildState.Idle);
    }

    // ---------- COMBATE (usando apenas Global Cooldown) ----------
    IEnumerator CombatRoutine()
    {
        while (state == WildState.Combat && aiActive)
        {
            if (target == null)
            {
                ChangeState(WildState.Idle);
                yield break;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            // Usar APENAS o sistema de global cooldown
            if (combatSystem != null && combatSystem.IsGlobalCooldownReady())
            {
                Debug.Log("Perform Combat ENCONTRADO!");
                var attacks = combatSystem.GetAttackInstances();
                var chosenAttack = SelectBestAttack(attacks, distanceToTarget);

                if (chosenAttack != null && chosenAttack.IsOffCooldown())
                {
                    Vector2 dirToTarget = (target.position - transform.position).normalized;

                    if (anim != null)
                    {
                        anim.SetFloat("ataqueX", dirToTarget.x);
                        anim.SetFloat("ataqueY", dirToTarget.y);
                    }

                    // Verifica se precisa se aproximar baseado na distância ideal do ataque
                    float idealDistance = chosenAttack.data.idealDistance;
                    float distanceDifference = Mathf.Abs(distanceToTarget - idealDistance);

                    // Define uma margem de tolerância para a distância ideal (por exemplo, 0.5 unidades)
                    float tolerancia = 0.5f;

                    if (distanceDifference > tolerancia)
                    {
                        // Se estiver muito longe ou muito perto, ajusta a posição
                        bool precisaAproximar = distanceToTarget > idealDistance;

                        PerseguirAlvo(target, moveSpeed, precisaAproximar);
                        if (rb != null && rb.velocity.magnitude > 0.1f && anim != null && !anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
                        {
                            anim.SetBool("Chase", true);
                        }
                        else if (anim != null)
                        {
                            anim.SetBool("Chase", false);
                        }

                        if (anim != null)
                            anim.SetBool("Attack", false);
                    }
                    if (distanceDifference <= tolerancia)
                    {
                        if (rb != null)
                            rb.velocity = Vector2.zero;

                        if (anim != null)
                        {
                            anim.SetBool("Chase", false);
                            anim.SetBool("Attack", true);
                        }

                        combatSystem.PerformManualAttack(chosenAttack, dirToTarget);

                        // Aguarda o fim da animação de ataque antes de permitir perseguição ou outras ações
                        if (anim != null)
                            yield return new WaitUntil(() => !anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));
                    }
                }
                else
                {
                    // Nenhum ataque disponível: fica parado esperando cooldown
                    if (rb != null)
                        rb.velocity = Vector2.zero;

                    if (anim != null)
                    {
                        anim.SetBool("Chase", false);
                        anim.SetBool("Attack", false);
                    }

                    FaceTarget();
                }
            }

            if (IsDefeated())
            {
                ChangeState(WildState.Defeated);
                yield break;
            }

            yield return null;
        }
    }

    public void OnAttackEnd()
    {
        // Apenas controle de animação
        //Debug.Log($"[{gameObject.name}] OnAttackEnd chamado!");
    }

    // ---------- DERROTADO ----------
    IEnumerator DefeatedRoutine()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool("Attack", false);
            anim.SetBool("Chase", false);
            anim.SetTrigger("debilitado");
        }

        float timer = 0f;
        while (timer < defeatedTime && state == WildState.Defeated && aiActive)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (aiActive)
            ChangeState(WildState.Fleeing);
    }

    // ---------- AUXILIARES ----------
    void FaceTarget()
    {
        if (target && anim != null)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            anim.SetFloat("dirOciosaX", dir.x);
            anim.SetFloat("dirOciosaY", dir.y);
        }
    }

    void PerseguirAlvo(Transform alvo, float speed, bool aproximar)
    {
        Vector2 direction = (alvo.position - transform.position).normalized;

        Vector2 safeDirection = ObstacleDetectionUtils.GetAvoidanceDirection(
            transform, direction, patrolObstacleRadius, avoidanceRadius, obstacleMask
        );

        if (anim != null)
        {
            anim.SetFloat("andarX", safeDirection.x);
            anim.SetFloat("andarY", safeDirection.y);
        }

        if (rb != null)
            rb.velocity = safeDirection * speed;
    }

    AssistantAttackClass SelectBestAttack(List<AssistantAttackClass> attacks, float distance)
    {
        List<AssistantAttackClass> validAttacks = new List<AssistantAttackClass>();

        foreach (var atk in attacks)
        {
            var data = atk.data;
            float custoPoder = data != null ? data.pontosPoder : 0f;

            // Verifica apenas cooldown e PP
            bool canUse = atk.IsOffCooldown() &&
                         (combatSystem.saudePokemon == null ||
                          combatSystem.saudePokemon.TemPontosPoderPara(custoPoder));

            if (canUse)
            {
                validAttacks.Add(atk);
            }
        }

        if (validAttacks.Count == 0) return null;

        // Ordena por prioridade e proximidade à distância ideal
        validAttacks.Sort((a, b) => {
            // Primeiro critério: prioridade
            int priorityCompare = b.data.priority.CompareTo(a.data.priority);
            if (priorityCompare != 0) return priorityCompare;

            // Segundo critério: proximidade à distância ideal
            float diffA = Mathf.Abs(distance - a.data.idealDistance);
            float diffB = Mathf.Abs(distance - b.data.idealDistance);
            return diffA.CompareTo(diffB);
        });

        // Retorna o melhor ataque com 80% de chance, ou um aleatório dos top 3
        if (Random.value < 0.8f)
            return validAttacks[0];

        int randomIndex = Random.Range(0, Mathf.Min(3, validAttacks.Count));
        return validAttacks[randomIndex];
    }

    bool IsDefeated()
    {
        var stats = GetComponentInChildren<SaudePokemon>();
        return stats && stats.pontosSaude <= 0;
    }

    public void StopAttack()
    {
        if (anim != null)
            anim.SetBool("Attack", false);

        if (rb != null && rb.velocity == Vector2.zero && anim != null)
        {
            anim.SetBool("Chase", false);
        }
    }

    // ---------- GETTERS PARA DEBUG ----------
    public WildState GetCurrentState() => state;
    public bool IsAIActive() => aiActive;

    private void OnDrawGizmosSelected()
    {
        // Raio de alerta (PRINCIPAL - onde batalha acontece)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRadius);

        // Raio de detecção/segurança
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

        // Linha para target se existir
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }

        // Indicador de estado atual
        Gizmos.color = aiActive ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
    }
}