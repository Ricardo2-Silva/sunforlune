using UnityEngine;

/// <summary>
/// VERSÃO DEFINITIVA — Fusão V1 + V10 (Corrigida)
/// 
/// - Movimentação V10 (sem jitter/tremedeira)
/// - OrthoSize fixo em 14.0625 no Editor (forçado via OnValidate + Reset)
/// - Em runtime, usa o orthoSize calculado pelo PPC como base real (garante harmonia com cameraPixelsPerUnit)
/// - Zoom In por ratio inteiro (divide o base)
/// - Zoom Out por multiplicador (multiplica o base)
/// - Não chama adjustCameraFOV() durante transições de zoom
/// - Restaura targetCameraHalfHeight do PPC ao sair do Play Mode
/// 
/// SETUP:
/// 1. Câmera como root na hierarquia (não filha de nenhum objeto).
/// 2. Adicione este script + PixelPerfectCamera ao GameObject da câmera.
/// 3. Arraste a referência do PixelPerfectCamera no Inspector.
/// 4. retroSnap = true no PixelPerfectCamera.
/// 5. Sprites móveis devem ter PixelSnap.
/// 6. defaultOrthoSize = 14.0625 (ou o valor desejado).
/// 7. No PPC, configure targetCameraHalfHeight = 14.0625 também.
/// </summary>
[ExecuteInEditMode]
public class CameraFollowManager : MonoBehaviour
{
    public static CameraFollowManager Instance { get; private set; }

    [Header("Follow")]
    [Tooltip("Transform que a câmera vai seguir.")]
    public Transform followTarget;

    [Tooltip("Tempo de suavização quando a câmera está se movendo.")]
    public float smoothTime = 0.15f;

    [Tooltip("Distância mínima para snap direto (evita micro-movimentos).")]
    public float snapThreshold = 0.5f;

    [Tooltip("Offset fixo da câmera em relação ao alvo.")]
    public Vector2 offset = Vector2.zero;

    [Header("Pixel Perfect Camera")]
    [Tooltip("Referência ao PixelPerfectCamera (para leitura de dados e PixelSnap).")]
    public PixelPerfectCamera pixelPerfectCamera;

    [Header("Configurações de Tamanho")]
    [Tooltip("OrthoSize padrão. Forçado no Editor. Em runtime, o PPC calcula o valor real.")]
    public float defaultOrthoSize = 14.0625f;

    [Header("Zoom In (Scroll para cima — aproxima)")]
    [Tooltip("Ratio máximo para zoom in. Ex: 3 = 3x mais perto.")]
    public int maxZoomInRatio = 3;

    [Header("Zoom Out (Scroll para baixo — afasta)")]
    [Tooltip("Níveis máximos de zoom out. Ex: 3 = até 4x mais área visível.")]
    public int maxZoomOutLevels = 3;

    [Header("Zoom Geral")]
    [Tooltip("Velocidade da transição suave de zoom.")]
    public float zoomSmoothSpeed = 8f;

    // ─── Shake internos ───
    private float shakeDuration;
    private float shakeTimer;
    private float currentShakeIntensity;

    // ─── Zoom internos ───
    private int currentZoomInRatio = 1;
    private int currentZoomOutLevel = 0;
    private bool isZoomedOut = false;
    private float targetOrthoSize;
    private bool isZoomTransitioning = false;
    private float runtimeBaseOrthoSize; // Calculado pelo PPC no Start — harmonia perfeita
    private Camera cam;

    // ─── Follow internos ───
    private Vector3 followVelocity = Vector3.zero;
    private Vector3 currentLogicalPosition;
    private bool hasInitializedPosition = false;

    // ═══════════════════════════════════════════════════════
    // EDITOR: Força defaultOrthoSize sem dar Play
    // e restaura targetCameraHalfHeight do PPC
    // ═══════════════════════════════════════════════════════

    private void Reset()
    {
        ForceEditorDefaults();
    }

    private void OnValidate()
    {
        ForceEditorDefaults();
    }

    private void ForceEditorDefaults()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;

        Camera c = GetComponent<Camera>();
        if (c != null && c.orthographic)
        {
            if (!Mathf.Approximately(c.orthographicSize, defaultOrthoSize))
            {
                c.orthographicSize = defaultOrthoSize;
            }
        }

        // Restaura o targetCameraHalfHeight do PPC para que ele recalcule
        // corretamente ao sair do Play Mode (evita size = 1.4375)
        if (pixelPerfectCamera != null)
        {
            if (!Mathf.Approximately(pixelPerfectCamera.targetCameraHalfHeight, defaultOrthoSize))
            {
                pixelPerfectCamera.targetCameraHalfHeight = defaultOrthoSize;
            }
        }
#endif
    }

    // ═══════════════════════════════════════════════════════
    // RUNTIME
    // ═══════════════════════════════════════════════════════

    private void Awake()
    {
        if (!Application.isPlaying) return;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cam = GetComponent<Camera>();
        currentZoomInRatio = 1;
        currentZoomOutLevel = 0;
        isZoomedOut = false;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        // Integração com PokemonSwitchManager
        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnControlledMemberChanged.AddListener(OnControlledChanged);
            RoleHandler current = PokemonSwitchManager.Instance.GetControlledMember();
            if (current != null) SetFollowTarget(current);
        }

        // Posição lógica inicial
        currentLogicalPosition = transform.position;
        hasInitializedPosition = true;

        // PASSO CRÍTICO ANTI-JITTER:
        // 1. Seta o targetCameraHalfHeight do PPC para nosso valor desejado
        // 2. Deixa o PPC calcular (adjustCameraFOV)
        // 3. Lê de volta o orthoSize que o PPC calculou
        // 4. Usa ESSE valor como base real em runtime
        // Isso garante harmonia perfeita entre orthographicSize e cameraPixelsPerUnit
        if (pixelPerfectCamera != null && cam != null)
        {
            pixelPerfectCamera.targetCameraHalfHeight = defaultOrthoSize;
            pixelPerfectCamera.adjustCameraFOV();

            // O PPC pode ter ajustado o orthoSize para um valor pixel-perfect
            // ligeiramente diferente do defaultOrthoSize. Usamos o valor DELE.
            runtimeBaseOrthoSize = cam.orthographicSize;
        }
        else
        {
            // Fallback se PPC não estiver configurado
            if (cam != null) cam.orthographicSize = defaultOrthoSize;
            runtimeBaseOrthoSize = defaultOrthoSize;
        }

        targetOrthoSize = runtimeBaseOrthoSize;
        isZoomTransitioning = false;

        Debug.Log($"[CameraFollow] defaultOrthoSize: {defaultOrthoSize}, " +
                  $"runtimeBaseOrthoSize (do PPC): {runtimeBaseOrthoSize}, " +
                  $"cameraPixelsPerUnit: {(pixelPerfectCamera != null ? pixelPerfectCamera.cameraPixelsPerUnit : 0f)}");
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;

        // Restaura o PPC para o valor padrão antes de destruir
        // Isso ajuda ao sair do Play Mode
        if (pixelPerfectCamera != null)
        {
            pixelPerfectCamera.targetCameraHalfHeight = defaultOrthoSize;
        }

        if (PokemonSwitchManager.Instance != null)
        {
            PokemonSwitchManager.Instance.OnControlledMemberChanged.RemoveListener(OnControlledChanged);
        }

        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Chamado ao sair do Play Mode. Restaura valores do Editor.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (pixelPerfectCamera != null)
        {
            pixelPerfectCamera.targetCameraHalfHeight = defaultOrthoSize;
        }
        if (cam != null)
        {
            cam.orthographicSize = defaultOrthoSize;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        HandleFollow();
        HandleScrollZoom();
        HandleZoomExecution();
    }

    // ═══════════════════════════════════════════════════════
    // FOLLOW (Lógica V10 — Anti Jitter)
    // ═════════════════════════════════════════════════��═════

    #region Follow

    private void HandleFollow()
    {
        if (followTarget == null) return;

        if (!hasInitializedPosition)
        {
            currentLogicalPosition = transform.position;
            hasInitializedPosition = true;
        }

        Vector3 desiredPos = new Vector3(
            followTarget.position.x + offset.x,
            followTarget.position.y + offset.y,
            transform.position.z
        );

        float distanceToTarget = Vector2.Distance(
            new Vector2(currentLogicalPosition.x, currentLogicalPosition.y),
            new Vector2(desiredPos.x, desiredPos.y)
        );

        if (distanceToTarget <= snapThreshold)
        {
            currentLogicalPosition = desiredPos;
            followVelocity = Vector3.zero;
            currentLogicalPosition = SnapToPixelGrid(currentLogicalPosition);
        }
        else
        {
            currentLogicalPosition = Vector3.SmoothDamp(
                currentLogicalPosition, desiredPos, ref followVelocity, smoothTime);
        }

        currentLogicalPosition.z = transform.position.z;

        Vector3 shakeOffset = CalculateShakeOffset();
        transform.position = currentLogicalPosition + shakeOffset;
    }

    /// <summary>
    /// Snap ao pixel grid da câmera usando cameraPixelsPerUnit do PPC.
    /// Essencial para evitar 'shimmering' em pixel art estático.
    /// </summary>
    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        if (pixelPerfectCamera == null || !pixelPerfectCamera.isInitialized) return position;

        float cameraPPU = pixelPerfectCamera.cameraPixelsPerUnit;
        if (cameraPPU <= 0) return position;

        position.x = Mathf.Round(position.x * cameraPPU) / cameraPPU;
        position.y = Mathf.Round(position.y * cameraPPU) / cameraPPU;

        return position;
    }

    private void SetFollowTarget(RoleHandler roleHandler)
    {
        if (roleHandler == null) return;
        Transform root = roleHandler.transform.parent != null
            ? roleHandler.transform.parent
            : roleHandler.transform;
        followTarget = root;
    }

    private void OnControlledChanged(RoleHandler newControlled)
    {
        SetFollowTarget(newControlled);
    }

    #endregion

    // ═══════════════════════════════════════════════════════
    // SHAKE
    // ═══════════════════════════════════════════════════════

    #region Shake

    public void TriggerShake(float intensity, float duration)
    {
        if (shakeTimer > 0 && currentShakeIntensity > intensity) return;
        currentShakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    private Vector3 CalculateShakeOffset()
    {
        if (shakeTimer <= 0) return Vector3.zero;

        shakeTimer -= Time.deltaTime;
        float progress = Mathf.Clamp01(shakeTimer / shakeDuration);
        float intensity = currentShakeIntensity * progress;
        Vector2 randomOffset = Random.insideUnitCircle * intensity;

        return new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    #endregion

    // ═══════════════════════════════════════════════════════
    // ZOOM
    // ═══════════════════════════════════════════════════════

    #region Zoom

    /// <summary>
    /// Lê input do scroll e ajusta nível/ratio de zoom.
    /// Scroll ↑ = zoom in. Scroll ↓ = zoom out.
    /// </summary>
    private void HandleScrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;

        if (scroll > 0f)
        {
            // SCROLL PARA CIMA — Zoom In
            if (isZoomedOut)
            {
                currentZoomOutLevel--;
                if (currentZoomOutLevel <= 0)
                {
                    currentZoomOutLevel = 0;
                    isZoomedOut = false;
                    currentZoomInRatio = 1;
                }
            }
            else
            {
                currentZoomInRatio = Mathf.Min(currentZoomInRatio + 1, maxZoomInRatio);
            }
        }
        else
        {
            // SCROLL PARA BAIXO — Zoom Out
            if (!isZoomedOut && currentZoomInRatio > 1)
            {
                currentZoomInRatio--;
            }
            else
            {
                isZoomedOut = true;
                currentZoomInRatio = 1;
                currentZoomOutLevel = Mathf.Min(currentZoomOutLevel + 1, maxZoomOutLevels);
            }
        }

        targetOrthoSize = CalculateTargetOrthoSize();
        isZoomTransitioning = true;
    }

    /// <summary>
    /// Calcula o orthoSize alvo baseado no runtimeBaseOrthoSize (calculado pelo PPC).
    /// Garante harmonia perfeita com cameraPixelsPerUnit.
    /// </summary>
    private float CalculateTargetOrthoSize()
    {
        if (isZoomedOut && currentZoomOutLevel > 0)
        {
            // Zoom out: base × (1 + nível)
            return runtimeBaseOrthoSize * (1 + currentZoomOutLevel);
        }
        else
        {
            // Zoom in ou padrão: base / ratio
            return runtimeBaseOrthoSize / currentZoomInRatio;
        }
    }

    /// <summary>
    /// Aplica o zoom com Lerp suave.
    /// NÃO chama adjustCameraFOV() durante a transição.
    /// Sincroniza PPC completamente APENAS quando estabiliza.
    /// </summary>
    private void HandleZoomExecution()
    {
        if (cam == null) return;
        if (!isZoomTransitioning) return;

        float currentSize = cam.orthographicSize;

        // Já chegou ao alvo?
        if (Mathf.Abs(currentSize - targetOrthoSize) < 0.001f)
        {
            cam.orthographicSize = targetOrthoSize;
            isZoomTransitioning = false;

            // Zoom estabilizou — sincroniza PPC completamente
            SyncPixelPerfectCameraFull(targetOrthoSize);
            return;
        }

        // Interpolação suave
        float newSize = Mathf.Lerp(currentSize, targetOrthoSize, Time.deltaTime * zoomSmoothSpeed);

        // Snap final se muito perto
        if (Mathf.Abs(newSize - targetOrthoSize) < 0.01f)
        {
            newSize = targetOrthoSize;
            isZoomTransitioning = false;
        }

        // Aplica na câmera
        cam.orthographicSize = newSize;

        // Durante transição: atualiza APENAS cameraPixelsPerUnit (para PixelSnap)
        UpdateCameraPixelsPerUnitOnly(newSize);

        // Se estabilizou neste frame, sincroniza PPC
        if (!isZoomTransitioning)
        {
            SyncPixelPerfectCameraFull(newSize);
        }
    }

    /// <summary>
    /// Atualiza apenas cameraPixelsPerUnit do PPC durante transição suave.
    /// Fórmula: cameraPixelsPerUnit = pixelHeight / (2 * orthographicSize)
    /// Não chama adjustCameraFOV() — evita conflito.
    /// </summary>
    private void UpdateCameraPixelsPerUnitOnly(float orthoSize)
    {
        if (pixelPerfectCamera == null || cam == null) return;
        if (orthoSize <= 0) return;

        pixelPerfectCamera.cameraPixelsPerUnit = (float)cam.pixelHeight / (2f * orthoSize);
    }

    /// <summary>
    /// Sincronização completa do PPC. Chamada APENAS quando o zoom estabiliza.
    /// Deixa o PPC recalcular todos os dados internos (ratio, nativeRes, etc.)
    /// e depois garante que nosso valor final prevalece.
    /// </summary>
    private void SyncPixelPerfectCameraFull(float finalOrthoSize)
    {
        if (pixelPerfectCamera == null || cam == null) return;

        pixelPerfectCamera.targetCameraHalfHeight = finalOrthoSize;
        pixelPerfectCamera.adjustCameraFOV();

        // Garante que nosso valor prevalece sobre qualquer arredondamento do PPC
        cam.orthographicSize = finalOrthoSize;
    }

    #endregion

    // ═══════════════════════════════════════════════════════
    // API PÚBLICA
    // ═══════════════════════════════════════════════════════

    #region Public API

    /// <summary>
    /// Reseta zoom para o padrão (1x).
    /// </summary>
    public void ResetZoom()
    {
        currentZoomInRatio = 1;
        currentZoomOutLevel = 0;
        isZoomedOut = false;
        targetOrthoSize = runtimeBaseOrthoSize;
        isZoomTransitioning = true;
    }

    /// <summary>
    /// Define zoom in por ratio. 1 = padrão, 2 = 2x mais perto, etc.
    /// </summary>
    public void SetZoomRatio(int ratio)
    {
        currentZoomInRatio = Mathf.Clamp(ratio, 1, maxZoomInRatio);
        currentZoomOutLevel = 0;
        isZoomedOut = false;
        targetOrthoSize = CalculateTargetOrthoSize();
        isZoomTransitioning = true;
    }

    /// <summary>
    /// Define zoom out por nível. 0 = padrão, 1 = 2x mais longe, etc.
    /// </summary>
    public void SetZoomOutLevel(int level)
    {
        currentZoomOutLevel = Mathf.Clamp(level, 0, maxZoomOutLevels);
        if (currentZoomOutLevel > 0)
        {
            isZoomedOut = true;
            currentZoomInRatio = 1;
        }
        else
        {
            isZoomedOut = false;
        }
        targetOrthoSize = CalculateTargetOrthoSize();
        isZoomTransitioning = true;
    }

    // ─── Getters ───

    public Transform GetFollowTarget() => followTarget;
    public bool IsShaking() => shakeTimer > 0;
    public float GetBaseOrthoSize() => runtimeBaseOrthoSize;
    public int GetCurrentZoomInRatio() => currentZoomInRatio;
    public int GetCurrentZoomOutLevel() => currentZoomOutLevel;
    public bool IsZoomedOut() => isZoomedOut;
    public bool IsZoomTransitioning() => isZoomTransitioning;

    public float GetCurrentZoomRatio()
    {
        return pixelPerfectCamera != null ? pixelPerfectCamera.ratio : 1f;
    }

    #endregion
}