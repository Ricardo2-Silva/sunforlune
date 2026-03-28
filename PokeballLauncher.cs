using System.Collections;
using UnityEngine;

public class PokeballLauncher : MonoBehaviour
{
    [Header("Prefabs de Lançamento (Release)")]
    public GameObject pokeballProjectilePrefab;
    public GameObject releaseBeamPrefab;
    public GameObject releaseTransitionPrefab;

    [Header("Prefabs de Recolhimento (Recall)")]
    public GameObject recallBeamPrefab;
    public GameObject recallTransitionPrefab;

    [Header("Componentes do Treinador")]
    public TrainerHandController handController;
    public ArcIndicator arcIndicator;
    public CaptureRingSystem captureRingSystem;
    public Transform trainerTransform;

    // ITEM 1.1: TEMPOS INDEPENDENTES PARA RELEASE E RECALL
    [Header("Tempos - Lançamento (Release)")]
    public float launchRadius = 6.5f;
    public float releaseArcHeight = 2.5f;
    public float releaseArcDuration = 0.6f;
    public float releaseHoverHeight = 1.5f;
    public float releaseHoverBounceTime = 0.3f;
    public float releaseBeamExtendTime = 0.2f;
    public float releaseBeamRetractTime = 0.2f;
    public float releaseTransitionDuration = 0.6f; // Tempo do Pokemon crescendo
    public float returnArcDuration = 0.5f;

    [Header("Tempos - Recolhimento (Recall)")]
    public float recallBeamExtendTime = 0.25f;
    public float recallBeamRetractTime = 0.25f;
    public float recallTransitionDuration = 0.5f; // Tempo do Pokemon encolhendo

    [Header("Configurações do Modo Captura")]
    public KeyCode captureModeKey = KeyCode.C;
    public int captureBallCount = 10;
    public PokeballData capturePokeballData;
    public float maxCaptureInaccuracyOffset = 2.5f; // Quão longe a bola cai quando erro = 100%
    public float brokenPokeballDelay = 0.5f;        // Tempo antes de quebrar e sumir

    [Header("Inputs - Gerais")]
    public KeyCode quickReleaseKey = KeyCode.F;
    public KeyCode specialModifierKey = KeyCode.G;
    public KeyCode[] releaseSlotKeys = new KeyCode[] { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6 };
    public PokeballData defaultPokeball;

    [Header("Estado")]
    [SerializeField] private LauncherState currentState = LauncherState.Idle;
    [SerializeField] private bool isAimingRelease = false;
    [SerializeField] private int aimingTeamIndex = -1;

    private KeyCode aimingKey = KeyCode.None;
    private bool captureModeActive = false;
    private Camera mainCamera;

    public enum LauncherState
    {
        Idle, AimingRelease, Launching, Recalling, AimingCapture, WaitingCaptureResult
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        if (trainerTransform == null) trainerTransform = transform;
    }

    private void Update()
    {
        if (currentState == LauncherState.Launching ||
            currentState == LauncherState.Recalling ||
            currentState == LauncherState.WaitingCaptureResult)
            return;

        HandleQuickReleaseInput();
        HandleSlotReleaseHoldToAim();
        HandleAimUpdateAndCommitByKeyUp();

        HandleCaptureModeToggle();
        HandleCaptureModeLoop();
        HandleRecallInput();
    }

    // ============================================================
    // RECALL SEQUENCE
    // ============================================================
    private void HandleRecallInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && currentState == LauncherState.Idle && !captureModeActive && !isAimingRelease)
        {
            RoleHandler toRecall = null;
            if (TargetSelectionManager.Instance != null && TargetSelectionManager.Instance.HasSelectedTarget())
            {
                TargetableEntity target = TargetSelectionManager.Instance.GetSelectedTarget();
                if (target != null && target.roleHandler != null &&
                    PokemonSwitchManager.Instance.GetTeamMembers().Contains(target.roleHandler))
                {
                    toRecall = target.roleHandler;
                }
            }

            if (toRecall != null)
            {
                TargetSelectionManager.Instance.DeselectTarget();
                StartCoroutine(RecallSequenceRoutine(toRecall));
            }
        }
    }

    private IEnumerator RecallSequenceRoutine(RoleHandler pokemon)
    {
        currentState = LauncherState.Recalling;

        Transform pokemonRoot = pokemon.transform.parent != null ? pokemon.transform.parent : pokemon.transform;
        Vector3 pokemonPos = pokemonRoot.position;
        PokeballData pb = GetPokeballForPokemon(pokemon);

        Vector3 handPos = trainerTransform.position;
        if (handController != null)
        {
            Vector2 dir = (pokemonPos - trainerTransform.position).normalized;
            handPos = handController.GetHandPosition(dir);
            handController.SetPokeballInHand(pb);
            handController.ShowHandPokeball();
            handController.PlayRecallAnimation(dir);
        }

        EnergyBeamRenderer beam = null;
        if (recallBeamPrefab != null)
        {
            GameObject beamObj = Instantiate(recallBeamPrefab, handPos, Quaternion.identity);
            beam = beamObj.GetComponent<EnergyBeamRenderer>();
            if (beam != null)
            {
                beam.SetBeamEnabled(true);
                beam.SetEndpoints(handPos, handPos);
                // Usa o tempo exclusivo de Recall
                yield return StartCoroutine(beam.ExtendTo(handPos, pokemonPos, recallBeamExtendTime));
            }
        }

        SpriteRenderer pokemonSprite = pokemonRoot.GetComponentInChildren<SpriteRenderer>();
        GameObject transitionObj = null;

        if (recallTransitionPrefab != null && pokemonSprite != null)
        {
            transitionObj = Instantiate(recallTransitionPrefab, pokemonPos, Quaternion.identity);
            var transition = transitionObj.GetComponent<PokemonTransitionEffect>();
            if (transition != null)
            {
                // Usa o tempo exclusivo de Recall
                yield return StartCoroutine(transition.PlayTransition(pokemonSprite, PokemonTransitionEffect.TransitionType.Recall, recallTransitionDuration, null));
            }
        }

        PokemonSwitchManager.Instance.RecallPokemonToPokeballImmediate(pokemon);

        if (beam != null)
        {
            // Usa o tempo exclusivo de Recall
            yield return StartCoroutine(beam.RetractEndToStart(handPos, recallBeamRetractTime));
            Destroy(beam.gameObject);
        }

        if (transitionObj != null) Destroy(transitionObj);
        if (handController != null) handController.HideHandPokeball();

        currentState = LauncherState.Idle;
    }

    // ============================================================
    // RELEASE SEQUENCE
    // ============================================================
    private IEnumerator ReleasePokemonSequence(RoleHandler pokemon, Vector3 targetPos)
    {
        currentState = LauncherState.Launching;
        PokeballData pb = GetPokeballForPokemon(pokemon);
        Vector2 direction = (targetPos - trainerTransform.position).normalized;

        if (handController != null)
        {
            handController.SetPokeballInHand(pb);
            handController.ShowHandPokeball();
            handController.PlayThrowAnimation(direction);
            yield return new WaitForSeconds(0.15f);
            handController.HideHandPokeball();
        }

        Vector3 startPos = (handController != null) ? handController.GetHandPosition(direction) : trainerTransform.position + (Vector3)(direction * 0.5f);

        GameObject pokeballObj = Instantiate(pokeballProjectilePrefab, startPos, Quaternion.identity);
        PokeballProjectile projectile = pokeballObj.GetComponent<PokeballProjectile>();
        projectile.Initialize(pb);

        bool arrived = false;
        projectile.LaunchArc(startPos, targetPos, releaseArcHeight, releaseArcDuration, () => arrived = true);
        while (!arrived) yield return null;

        yield return StartCoroutine(projectile.BounceAndHover(targetPos, releaseHoverHeight, releaseHoverBounceTime));

        EnergyBeamRenderer beam = null;
        if (releaseBeamPrefab != null)
        {
            GameObject beamObj = Instantiate(releaseBeamPrefab, projectile.transform.position, Quaternion.identity);
            beam = beamObj.GetComponent<EnergyBeamRenderer>();
            if (beam != null)
            {
                beam.SetBeamEnabled(true);
                beam.SetEndpoints(projectile.transform.position, projectile.transform.position);
                // Usa o tempo exclusivo do Release
                yield return StartCoroutine(beam.ExtendTo(projectile.transform.position, targetPos, releaseBeamExtendTime));
            }
        }

        GameObject pokemonRoot = pokemon.transform.parent != null ? pokemon.transform.parent.gameObject : pokemon.gameObject;
        pokemonRoot.transform.position = targetPos;
        pokemonRoot.SetActive(true);
        pokemonRoot.transform.localScale = Vector3.zero;

        // ITEM 1.2: CONGELA A FÍSICA E MOVIMENTO DO POKEMON AO NASCER
        Rigidbody2D rb = pokemonRoot.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true; // Impede gravidade e inércia de mexer nele
        }

        SpriteRenderer pokemonSprite = pokemonRoot.GetComponentInChildren<SpriteRenderer>();
        GameObject transitionObj = null;

        if (releaseTransitionPrefab != null && pokemonSprite != null)
        {
            transitionObj = Instantiate(releaseTransitionPrefab, targetPos, Quaternion.identity);
            var transition = transitionObj.GetComponent<PokemonTransitionEffect>();
            if (transition != null)
            {
                // Usa o tempo exclusivo do Release
                yield return StartCoroutine(transition.PlayTransition(pokemonSprite, PokemonTransitionEffect.TransitionType.Release, releaseTransitionDuration, null));
            }
        }
        else
        {
            pokemonRoot.transform.localScale = Vector3.one;
        }

        // ITEM 1.2: LIBERA A FÍSICA DE VOLTA AGORA QUE CRESCEU
        if (rb != null) rb.isKinematic = false;

        // E só agora dá o controle para o PSM (que vai ligar a IA ou Input do jogador)
        if (MemberManager.Instance != null) MemberManager.Instance.SetPokemonOnField(pokemon, true);
        PokemonSwitchManager.Instance.SwitchControlToPokemon(PokemonSwitchManager.Instance.GetTeamMembers().IndexOf(pokemon));

        if (beam != null)
        {
            yield return StartCoroutine(beam.RetractEndToStart(projectile.transform.position, releaseBeamRetractTime));
            Destroy(beam.gameObject);
        }

        if (transitionObj != null) Destroy(transitionObj);

        yield return StartCoroutine(projectile.CloseAndReturnArc(projectile.transform.position, trainerTransform.position, releaseArcHeight * 0.5f, returnArcDuration));

        Destroy(pokeballObj);
        currentState = captureModeActive ? LauncherState.AimingCapture : LauncherState.Idle;
    }

    // ============================================================
    // CAPTURE SEQUENCE
    // ============================================================
    private void HandleCaptureModeToggle()
    {
        if (Input.GetKeyDown(captureModeKey) && currentState == LauncherState.Idle && !isAimingRelease)
        {
            captureModeActive = !captureModeActive;
            if (captureModeActive)
            {
                currentState = LauncherState.AimingCapture;
                if (arcIndicator != null)
                {
                    arcIndicator.maxLaunchRadius = launchRadius;
                    arcIndicator.arcHeight = releaseArcHeight;
                    arcIndicator.ShowIndicator();
                    arcIndicator.SetIndicatorTint(arcIndicator.idleCaptureColor);
                }
                if (captureRingSystem != null) captureRingSystem.ShowRing();
            }
            else
            {
                currentState = LauncherState.Idle;
                if (arcIndicator != null) arcIndicator.HideIndicator();
                if (captureRingSystem != null) captureRingSystem.StopCaptureRing();
            }
        }
        if (captureModeActive && Input.GetKeyDown(KeyCode.Escape))
        {
            captureModeActive = false;
            currentState = LauncherState.Idle;
            if (arcIndicator != null) arcIndicator.HideIndicator();
            if (captureRingSystem != null) captureRingSystem.StopCaptureRing();
        }
    }

    private void HandleCaptureModeLoop()
    {
        if (!captureModeActive || currentState != LauncherState.AimingCapture) return;

        Vector3 start = trainerTransform.position;
        Vector3 mouseWorld = GetMouseWorldPosition();

        Vector3 clampedMousePos = arcIndicator != null ? arcIndicator.GetClampedTarget(start, mouseWorld) : ClampToRadius(start, mouseWorld, launchRadius);

        if (arcIndicator != null) arcIndicator.UpdateArc(start, clampedMousePos, releaseArcHeight);

        // Passa a posição pro anel
        if (captureRingSystem != null) captureRingSystem.SetCursorWorldPosition(clampedMousePos);

        if (Input.GetMouseButtonDown(0))
        {
            if (captureBallCount <= 0 || capturePokeballData == null) return;

            // ITEM 2.5: Consome o item imediatamente
            captureBallCount--;

            // ITEM 2.4: Calcula a precisão exata daquele milissegundo do clique
            float inaccuracy = captureRingSystem != null ? captureRingSystem.GetInaccuracyMultiplier() : 1f;

            StartCoroutine(CaptureThrowRoutine(clampedMousePos, inaccuracy));
        }
    }

    private IEnumerator CaptureThrowRoutine(Vector3 aimedPos, float inaccuracy)
    {
        currentState = LauncherState.Launching;

        // ITEM 2.4: Calcula o erro de rota. 0 = Cai perfeito. 1 = Cai até maxCaptureInaccuracyOffset pro lado.
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * (maxCaptureInaccuracyOffset * inaccuracy);
        Vector3 finalImpactPos = aimedPos + new Vector3(randomOffset.x, randomOffset.y, 0f);

        if (handController != null)
        {
            Vector2 dir = (finalImpactPos - trainerTransform.position).normalized;
            handController.SetPokeballInHand(capturePokeballData);
            handController.ShowHandPokeball();
            handController.PlayThrowAnimation(dir);
            yield return new WaitForSeconds(0.15f);
            handController.HideHandPokeball();
        }

        Vector3 startPos = trainerTransform.position;
        Vector2 d2 = (finalImpactPos - trainerTransform.position).normalized;
        if (handController != null) startPos = handController.GetHandPosition(d2);

        GameObject pokeballObj = Instantiate(pokeballProjectilePrefab, startPos, Quaternion.identity);
        PokeballProjectile proj = pokeballObj.GetComponent<PokeballProjectile>();
        proj.Initialize(capturePokeballData);

        bool arrived = false;
        proj.LaunchArc(startPos, finalImpactPos, releaseArcHeight, releaseArcDuration, () => arrived = true);
        while (!arrived) yield return null;

        // ITEM 2.5: SOBERANIA DA FÍSICA. Confere se a coordenada desviada BATEU no pokémon
        Collider2D hitCollider = null;
        if (captureRingSystem != null)
        {
            hitCollider = Physics2D.OverlapPoint(finalImpactPos, captureRingSystem.capturableMask);
        }

        if (hitCollider != null)
        {
            // Bateu! Sucesso (Entra estado de Captura)
            proj.OpenPokeball();
            yield return new WaitForSeconds(0.25f);
            Destroy(pokeballObj);
            Debug.Log("[Capture] Acertou o alvo! Iniciando Captura...");
        }
        else
        {
            // ITEM 2.3: Errou/Chutou no vazio. Aguarda e Quebra a pokébola.
            Debug.Log("[Capture] Errou o arremesso!");
            yield return StartCoroutine(proj.BreakPokeballWithDelay(brokenPokeballDelay));
        }

        currentState = LauncherState.AimingCapture;
    }

    // ============================================================
    // UTILIDADES
    // ============================================================
    public bool RequestReleasePokemon(RoleHandler rh, Vector3 targetPos)
    {
        if (rh == null || PokemonSwitchManager.Instance == null) return false;
        if (MemberManager.Instance != null && MemberManager.Instance.IsPokemonOnField(rh)) return false;
        Vector3 start = trainerTransform.position;
        Vector3 clamped = ClampToRadius(start, targetPos, launchRadius);
        StartCoroutine(ReleasePokemonSequence(rh, clamped));
        return true;
    }

    private void HandleQuickReleaseInput()
    {
        if (Input.GetKeyDown(quickReleaseKey) && currentState == LauncherState.Idle && !captureModeActive)
        {
            if (PokemonSwitchManager.Instance == null) return;
            var team = PokemonSwitchManager.Instance.GetTeamMembers();
            if (team.Count == 0 || team[0] == null) return;

            RoleHandler rh = team[0];
            if (MemberManager.Instance != null && MemberManager.Instance.IsPokemonOnField(rh)) return;

            Vector3 target = ClampToRadius(trainerTransform.position, GetMouseWorldPosition(), launchRadius);
            StartCoroutine(ReleasePokemonSequence(rh, target));
        }
    }

    private void HandleSlotReleaseHoldToAim()
    {
        if (captureModeActive || (currentState != LauncherState.Idle && currentState != LauncherState.AimingRelease)) return;
        if (!Input.GetKey(specialModifierKey)) return;

        for (int i = 0; i < releaseSlotKeys.Length; i++)
        {
            if (Input.GetKeyDown(releaseSlotKeys[i]))
            {
                BeginAimRelease(i, releaseSlotKeys[i]);
                break;
            }
        }
    }

    private void BeginAimRelease(int teamIndex, KeyCode keyThatStartedAim)
    {
        if (PokemonSwitchManager.Instance == null) return;
        var team = PokemonSwitchManager.Instance.GetTeamMembers();
        if (teamIndex < 0 || teamIndex >= team.Count) return;

        RoleHandler rh = team[teamIndex];
        if (rh == null || (MemberManager.Instance != null && MemberManager.Instance.IsPokemonOnField(rh))) return;

        aimingTeamIndex = teamIndex;
        aimingKey = keyThatStartedAim;
        isAimingRelease = true;
        currentState = LauncherState.AimingRelease;

        var pb = GetPokeballForPokemon(rh);
        if (handController != null) { handController.SetPokeballInHand(pb); handController.ShowHandPokeball(); }
        if (arcIndicator != null) { arcIndicator.maxLaunchRadius = launchRadius; arcIndicator.arcHeight = releaseArcHeight; arcIndicator.ShowIndicator(); }
        if (MemberManager.Instance != null) { MemberManager.Instance.summonRadius = launchRadius; MemberManager.Instance.ShowSummonRadius(true); }
    }

    private void HandleAimUpdateAndCommitByKeyUp()
    {
        if (!isAimingRelease || currentState != LauncherState.AimingRelease) return;
        Vector3 start = trainerTransform.position;
        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 clamped = arcIndicator != null ? arcIndicator.GetClampedTarget(start, mouseWorld) : ClampToRadius(start, mouseWorld, launchRadius);

        if (arcIndicator != null) arcIndicator.UpdateArc(start, mouseWorld, releaseArcHeight);

        if (aimingKey != KeyCode.None && Input.GetKeyUp(aimingKey)) CommitAimRelease(clamped);
        if (!Input.GetKey(specialModifierKey) || Input.GetKeyDown(KeyCode.Escape)) CancelAim();
    }

    private void CommitAimRelease(Vector3 targetPos)
    {
        if (PokemonSwitchManager.Instance == null) { CancelAim(); return; }
        var team = PokemonSwitchManager.Instance.GetTeamMembers();
        if (aimingTeamIndex < 0 || aimingTeamIndex >= team.Count) { CancelAim(); return; }

        RoleHandler rh = team[aimingTeamIndex];
        if (rh == null) { CancelAim(); return; }

        isAimingRelease = false;
        aimingKey = KeyCode.None;
        currentState = LauncherState.Launching;

        if (arcIndicator != null) arcIndicator.HideIndicator();
        if (MemberManager.Instance != null) MemberManager.Instance.ShowSummonRadius(false);
        if (handController != null) handController.HideHandPokeball();

        StartCoroutine(ReleasePokemonSequence(rh, targetPos));
    }

    private void CancelAim()
    {
        isAimingRelease = false;
        aimingTeamIndex = -1;
        aimingKey = KeyCode.None;
        currentState = LauncherState.Idle;

        if (arcIndicator != null) arcIndicator.HideIndicator();
        if (MemberManager.Instance != null) MemberManager.Instance.ShowSummonRadius(false);
        if (handController != null) handController.HideHandPokeball();
    }

    private PokeballData GetPokeballForPokemon(RoleHandler pokemon)
    {
        if (pokemon == null) return defaultPokeball;
        Mon mon = pokemon.GetMon();
        if (mon != null && mon.IsCaptured && mon.CapturedPokeball != null) return mon.CapturedPokeball;
        return defaultPokeball;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0f;
        Vector3 world = mainCamera.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        return world;
    }

    private Vector3 ClampToRadius(Vector3 startPos, Vector3 targetPos, float radius)
    {
        float dist = Vector3.Distance(startPos, targetPos);
        if (dist <= radius) return targetPos;
        Vector3 dir = (targetPos - startPos).normalized;
        return startPos + dir * radius;
    }
}