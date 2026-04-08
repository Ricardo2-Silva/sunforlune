using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EstadoSelvagem { Idle, Alert, Fleeing, Combat, Defeated }

public class PkmnSelvagemAI : MonoBehaviour
{
    [Header("Parâmetros Gerais")]
    public float moveSpeed = 2f;
    public float idleMoveDuration = 1f;
    public float idleWaitDuration = 2f;
    public float patrolObstacleRadius = 1f;
    public float avoidanceRadius = 0.65f;
    public LayerMask obstacleMask;

    [Header("Detecção e Estados")]
    public float alertRadius = 5f;
    public float targetDetectionRadius = 7f;
    public LayerMask targetLayer;

    [Header("Fuga e Derrota")]
    public float fleeDuration = 2f;
    public float defeatedTime = 3f;
    public AudioClip fleeSound;
    public GameObject exclamarEffectPrefab;

    private float coragem = 0.5f;

    [Header("Debug — Estado Atual")]
    [SerializeField] private EstadoSelvagem state = EstadoSelvagem.Idle;
    [SerializeField] private bool aiActive = false;

    public Transform target;
    public Rigidbody2D rb;

    [Header("Dependências")]
    public PokemonAnimatorController animController;
    public ExecutorCombate combatSystem;
    public PokemonEffectsHandler effectsHandler;

    private Coroutine stateRoutine;
    private bool wasDisabled = false;

    void Start() { }

    void OnEnable()
    {
        if (wasDisabled)
        {
            RestartAI();
            wasDisabled = false;
        }
        else
        {
            InitializeAI();
        }
    }

    void OnDisable()
    {
        wasDisabled = true;
        StopAI();
    }

    private void InitializeAI()
    {
        Mon mon = GetComponentInParent<Mon>();
        if (mon == null) mon = transform.root.GetComponentInChildren<Mon>();

        coragem = mon != null ? NaturezaData.GetCourage(mon.NaturezaAtual) : 0.5f;

        aiActive = true;
        target = null;
        ChangeState(EstadoSelvagem.Idle);
    }

    public void RestartAI()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAI();
        InitializeAI();
    }

    public void StopAI()
    {
        aiActive = false;

        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (animController != null)
            animController.CancelarAtaque();

        target = null;
    }

    void Update()
    {
        if (!aiActive) return;

        if (target == null)
            DetectNewTarget();
        else
        {
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > targetDetectionRadius)
            {
                target = null;
                if (state != EstadoSelvagem.Idle)
                    ChangeState(EstadoSelvagem.Idle);
                return;
            }
        }

        HandleStateTransitions();
    }

    void HandleStateTransitions()
    {
        if (target == null) return;
        float dist = Vector2.Distance(transform.position, target.position);
        bool inAlertRange = dist <= alertRadius;

        switch (state)
        {
            case EstadoSelvagem.Idle:
                if (inAlertRange) ChangeState(EstadoSelvagem.Alert);
                break;
            case EstadoSelvagem.Alert:
                if (!inAlertRange) ChangeState(EstadoSelvagem.Idle);
                break;
            case EstadoSelvagem.Combat:
                if (!inAlertRange) ChangeState(EstadoSelvagem.Idle);
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
            if (dist < closest) { closest = dist; found = hit.transform; }
        }

        target = found;
    }

    void ChangeState(EstadoSelvagem newState)
    {
        if (!aiActive) return;
        if (stateRoutine != null) StopCoroutine(stateRoutine);

        state = newState;

        switch (state)
        {
            case EstadoSelvagem.Idle:
                stateRoutine = StartCoroutine(PatrolRoutine());
                break;
            case EstadoSelvagem.Alert:
                stateRoutine = StartCoroutine(AlertRoutine());
                break;
            case EstadoSelvagem.Fleeing:
                stateRoutine = StartCoroutine(FleeRoutine());
                break;
            case EstadoSelvagem.Combat:
                //stateRoutine = StartCoroutine(CombatRoutine());
                break;
            case EstadoSelvagem.Defeated:
                stateRoutine = StartCoroutine(DefeatedRoutine());
                break;
        }
    }

    IEnumerator PatrolRoutine()
    {
        if (animController != null)
            animController.TocarIdle(RetroSpriteAnimator.Direcao8.Baixo);

        while (state == EstadoSelvagem.Idle && aiActive)
        {
            Vector2 randDir = Random.insideUnitCircle.normalized;
            float timer = 0f;

            Vector2 safeDir = ObstacleDetectionUtils.GetAvoidanceDirection(
                transform, randDir, patrolObstacleRadius, avoidanceRadius, obstacleMask);

            if (animController != null) animController.TocarMovimento(safeDir);

            while (timer < idleMoveDuration && state == EstadoSelvagem.Idle && aiActive)
            {
                if (rb != null) rb.velocity = safeDir * moveSpeed;
                timer += Time.deltaTime;
                yield return null;
            }

            if (rb != null) rb.velocity = Vector2.zero;

            if (animController != null) animController.TocarIdle(safeDir);

            if (state != EstadoSelvagem.Idle || !aiActive) yield break;

            yield return new WaitForSeconds(idleWaitDuration);
        }
    }

    IEnumerator AlertRoutine()
    {
        if (rb != null) rb.velocity = Vector2.zero;

        if (animController != null && target != null)
        {
            Vector2 dirParaAlvo = (target.position - transform.position).normalized;
            animController.TocarIdle(dirParaAlvo);
        }

        if (effectsHandler && exclamarEffectPrefab)
            effectsHandler.ShowAlertEffect(exclamarEffectPrefab);

        float decisionTime = 2f;
        float timer = 0f;

        while (timer < decisionTime && state == EstadoSelvagem.Alert && aiActive)
        {
            FaceTarget();
            timer += Time.deltaTime;
            yield return null;
        }

        if (state == EstadoSelvagem.Alert && aiActive)
        {
            if (Random.value > coragem)
                ChangeState(EstadoSelvagem.Fleeing);
            else
                ChangeState(EstadoSelvagem.Combat);
        }
    }

    IEnumerator FleeRoutine()
    {
        if (effectsHandler) effectsHandler.PlayFleeSound(fleeSound);
        if (effectsHandler) effectsHandler.StartFade();

        float timer = 0f;

        while (timer < fleeDuration && state == EstadoSelvagem.Fleeing && aiActive)
        {
            if (target)
            {
                Vector2 fleeDir = (transform.position - target.position).normalized;
                Vector2 safeDir = ObstacleDetectionUtils.GetAvoidanceDirection(
                    transform, fleeDir, patrolObstacleRadius, avoidanceRadius, obstacleMask);

                if (rb != null) rb.velocity = safeDir * moveSpeed * 1.5f;
                if (animController != null) animController.TocarMovimento(safeDir);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.velocity = Vector2.zero;
        if (animController != null) animController.TocarIdle(RetroSpriteAnimator.Direcao8.Baixo);

        if (aiActive) ChangeState(EstadoSelvagem.Idle);
    }

   /* IEnumerator CombatRoutine()
    {
        while (state == EstadoSelvagem.Combat && aiActive)
        {
            if (target == null) { ChangeState(EstadoSelvagem.Idle); yield break; }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            if (combatSystem != null && combatSystem.CooldownGlobalDisponivel())
            {
                var attacks = combatSystem.ObterAtaques();
                var chosenAttack = SelectBestAttack(attacks, distanceToTarget);

                if (chosenAttack != null && chosenAttack.EstaDisponivel())
                {
                    Vector2 dirToTarget = (target.position - transform.position).normalized;
                    float idealDistance = chosenAttack.dados != null ? chosenAttack.dados.distanciaIdeal : 1.5f;
                    float distanceDifference = Mathf.Abs(distanceToTarget - idealDistance);
                    float tolerancia = 0.5f;

                    if (distanceDifference > tolerancia)
                    {
                        bool precisaAproximar = distanceToTarget > idealDistance;
                        PerseguirAlvo(target, moveSpeed, precisaAproximar);

                        if (rb != null && rb.velocity.magnitude > 0.1f)
                        {
                            if (animController != null) animController.TocarMovimento(rb.velocity.normalized);
                        }
                        else
                        {
                            if (animController != null) animController.TocarIdle(dirToTarget);
                        }
                    }

                    if (distanceDifference <= tolerancia)
                    {
                        if (rb != null) rb.velocity = Vector2.zero;

                        if (animController != null && chosenAttack.dados != null)
                            animController.TocarAtaque(chosenAttack.dados.nomeAtaque, dirToTarget);

                        combatSystem.ExecutarAtaqueManual(chosenAttack, dirToTarget, target);

                        yield return new WaitUntil(() => combatSystem.CooldownGlobalDisponivel() || !aiActive);
                    }
                }
                else
                {
                    if (rb != null) rb.velocity = Vector2.zero;
                    FaceTarget();
                }
            }

            if (IsDefeated()) { ChangeState(EstadoSelvagem.Defeated); yield break; }

            yield return null;
        }
    }*/

    IEnumerator DefeatedRoutine()
    {
        if (rb != null) rb.velocity = Vector2.zero;
        if (animController != null) animController.TocarDerrota();

        float timer = 0f;
        while (timer < defeatedTime && state == EstadoSelvagem.Defeated && aiActive)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (aiActive) ChangeState(EstadoSelvagem.Fleeing);
    }

    void FaceTarget()
    {
        if (target == null || animController == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        animController.SetDirecao(dir);
    }

    void PerseguirAlvo(Transform alvo, float speed, bool aproximar)
    {
        Vector2 direction = (alvo.position - transform.position).normalized;
        Vector2 safeDirection = ObstacleDetectionUtils.GetAvoidanceDirection(
            transform, direction, patrolObstacleRadius, avoidanceRadius, obstacleMask);

        if (rb != null) rb.velocity = safeDirection * speed;
    }

    ClasseAtaque SelectBestAttack(List<ClasseAtaque> attacks, float distance)
    {
        if (attacks == null || attacks.Count == 0) return null;

        List<ClasseAtaque> validAttacks = new List<ClasseAtaque>();

        foreach (var atk in attacks)
        {
            var data = atk.dados;
            float custoPoder = data != null ? data.pontosPoder : 0f;

            bool canUse = atk.EstaDisponivel() &&
                (combatSystem.saudePokemon == null ||
                 combatSystem.saudePokemon.TemPontosPoderPara(custoPoder));

            if (canUse) validAttacks.Add(atk);
        }

        if (validAttacks.Count == 0) return null;

        validAttacks.Sort((a, b) =>
        {
            int priorityCompare = (b.dados != null ? b.dados.prioridade : 0)
                                .CompareTo(a.dados != null ? a.dados.prioridade : 0);
            if (priorityCompare != 0) return priorityCompare;
            float diffA = Mathf.Abs(distance - (a.dados != null ? a.dados.distanciaIdeal : 1.5f));
            float diffB = Mathf.Abs(distance - (b.dados != null ? b.dados.distanciaIdeal : 1.5f));
            return diffA.CompareTo(diffB);
        });

        if (Random.value < 0.8f) return validAttacks[0];
        return validAttacks[Random.Range(0, Mathf.Min(3, validAttacks.Count))];
    }

    bool IsDefeated()
    {
        var stats = GetComponentInParent<SaudePokemon>();
        if (stats == null) stats = transform.root.GetComponentInChildren<SaudePokemon>();

        return stats != null && stats.GetSaudeAtual() <= 0;
    }
}