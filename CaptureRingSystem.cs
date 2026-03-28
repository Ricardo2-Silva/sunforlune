using UnityEngine;

public class CaptureRingSystem : MonoBehaviour
{
    [Header("Renderização (Linha ou Sprite)")]
    public bool useSpriteRenderer = false;
    public Sprite ringSprite;

    [Header("Render Layer")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 250;

    [Header("Cores e Detecção (ITEM 2.1)")]
    public Color perfectColor = Color.green;
    public Color badColor = Color.red;
    public Color idleColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
    public LayerMask capturableMask; // Selecione a Layer do Pokémon Selvagem aqui!

    [Header("Mecânica de Nível (ITEM 2.4)")]
    [Range(1, 50)] public int trainerLevel = 1;
    public float baseMaxRingRadius = 1.8f;
    public float minRingRadius = 0.3f;
    public float pulseSpeed = 2f;

    private LineRenderer lineRing;
    private SpriteRenderer spriteRing;
    private Vector3 cursorWorldPos;

    private bool hasCapturableUnderCursor;
    private float currentRadius;
    private float pingPongT = 0f;

    private void Awake()
    {
        if (useSpriteRenderer) SetupSpriteRenderer();
        else SetupLineRenderer();

        currentRadius = GetEffectiveMaxRadius();
    }

    private void SetupLineRenderer()
    {
        GameObject obj = new GameObject("RingLine");
        obj.transform.SetParent(transform);
        lineRing = obj.AddComponent<LineRenderer>();
        lineRing.positionCount = 65;
        lineRing.loop = true;
        lineRing.useWorldSpace = true;
        lineRing.startWidth = 0.05f;
        lineRing.endWidth = 0.05f;
        lineRing.material = new Material(Shader.Find("Sprites/Default"));
        lineRing.sortingLayerName = sortingLayerName;
        lineRing.sortingOrder = sortingOrder;
    }

    private void SetupSpriteRenderer()
    {
        GameObject obj = new GameObject("RingSprite");
        obj.transform.SetParent(transform);
        spriteRing = obj.AddComponent<SpriteRenderer>();
        spriteRing.sprite = ringSprite;
        spriteRing.sortingLayerName = sortingLayerName;
        spriteRing.sortingOrder = sortingOrder;
    }

    private void Update()
    {
        // ITEM 2.2: Detecção Física Independente
        Collider2D hit = Physics2D.OverlapPoint(cursorWorldPos, capturableMask);
        bool wasCapturable = hasCapturableUnderCursor;
        hasCapturableUnderCursor = (hit != null);

        // Se acabou de focar no alvo, começa do vermelho (maior)
        if (hasCapturableUnderCursor && !wasCapturable) pingPongT = 1f;

        if (!hasCapturableUnderCursor)
        {
            pingPongT = 0f;
            currentRadius = GetEffectiveMaxRadius();
            DrawRing(cursorWorldPos, currentRadius, idleColor);
            return;
        }

        // ITEM 2.2: O anel só pulsa e muda de cor se achou o Pokémon
        pingPongT += Time.deltaTime * pulseSpeed;
        float t = Mathf.PingPong(pingPongT, 1f);

        float effectiveMax = GetEffectiveMaxRadius();
        currentRadius = Mathf.Lerp(minRingRadius, effectiveMax, t);

        Color currentColor = Color.Lerp(perfectColor, badColor, t);
        DrawRing(cursorWorldPos, currentRadius, currentColor);
    }

    private float GetEffectiveMaxRadius()
    {
        // Lv 1 = Anel abre muito. Lv 50 = Anel abre pouco.
        float levelFactor = (trainerLevel - 1f) / 49f;
        return Mathf.Lerp(baseMaxRingRadius, minRingRadius + 0.3f, levelFactor);
    }

    private void DrawRing(Vector3 center, float radius, Color color)
    {
        if (useSpriteRenderer && spriteRing != null)
        {
            spriteRing.transform.position = center;
            spriteRing.color = color;
            spriteRing.transform.localScale = Vector3.one * (radius * 2f);
        }
        else if (lineRing != null)
        {
            lineRing.startColor = color;
            lineRing.endColor = color;
            for (int i = 0; i <= 64; i++)
            {
                float angle = ((float)i / 64) * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;
                lineRing.SetPosition(i, new Vector3(x, y, center.z - 0.01f));
            }
        }
    }

    public void SetCursorWorldPosition(Vector3 clampedPos) => cursorWorldPos = clampedPos;

    public void StopCaptureRing()
    {
        hasCapturableUnderCursor = false;
        if (lineRing != null) lineRing.enabled = false;
        if (spriteRing != null) spriteRing.enabled = false;
    }

    public void ShowRing()
    {
        if (lineRing != null) lineRing.enabled = true;
        if (spriteRing != null) spriteRing.enabled = true;
    }

    // ITEM 2.4: Retorna um multiplicador (0 a 1) do erro do arremesso baseado no anel
    public float GetInaccuracyMultiplier()
    {
        if (!hasCapturableUnderCursor) return 1f; // Chutar no vazio dá erro total
        return Mathf.InverseLerp(minRingRadius, GetEffectiveMaxRadius(), currentRadius);
    }
}