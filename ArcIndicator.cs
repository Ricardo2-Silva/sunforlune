using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ArcIndicator : MonoBehaviour
{
    [Header("Configurações do Arco")]
    public int resolution = 30;
    public float arcHeight = 2.5f;
    public float maxLaunchRadius = 6.5f;

    [Header("Cores")]
    public Color validColor = new Color(0.3f, 0.8f, 1f, 0.65f);
    public Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.45f);

    [Header("Capture Mode Tint")]
    public Color idleCaptureColor = new Color(0.7f, 0.7f, 0.7f, 0.55f);
    public Color activeCaptureColor = new Color(1f, 0.2f, 0.2f, 0.70f);

    [Header("Tracejado")]
    public bool dashed = true;
    public float textureTilingScale = 2f;
    public Material lineMaterial;

    [Header("Marcador e Círculo")]
    public GameObject targetMarkerPrefab;
    public SpriteRenderer rangeCircle;

    private LineRenderer line;
    private GameObject markerInstance;
    private bool active;
    private Color forcedTint;
    private bool useForcedTint = false;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.startWidth = 0.08f;
        line.endWidth = 0.05f;
        line.enabled = false;

        if (dashed) line.textureMode = LineTextureMode.Tile;
        if (lineMaterial != null) line.material = lineMaterial;
        else if (line.material == null) line.material = new Material(Shader.Find("Sprites/Default"));

        if (targetMarkerPrefab != null)
        {
            markerInstance = Instantiate(targetMarkerPrefab, transform);
            markerInstance.SetActive(false);
        }

        if (rangeCircle != null) rangeCircle.gameObject.SetActive(false);
    }

    public void ShowIndicator()
    {
        active = true;
        line.enabled = true;
        if (markerInstance != null) markerInstance.SetActive(true);
        if (rangeCircle != null)
        {
            rangeCircle.gameObject.SetActive(true);
            ApplyRangeCircleScale();
        }
    }

    public void HideIndicator()
    {
        active = false;
        line.enabled = false;
        useForcedTint = false;
        if (markerInstance != null) markerInstance.SetActive(false);
        if (rangeCircle != null) rangeCircle.gameObject.SetActive(false);
    }

    public void SetIndicatorTint(Color c) { forcedTint = c; useForcedTint = true; }

    public void UpdateArc(Vector3 startPos, Vector3 desiredTargetPos, float customArcHeight = -1f)
    {
        if (!active) return;

        float height = customArcHeight > 0 ? customArcHeight : arcHeight;
        Vector3 clampedTarget = GetClampedTarget(startPos, desiredTargetPos);
        bool inRange = Vector3.Distance(startPos, desiredTargetPos) <= maxLaunchRadius;
        Color c = useForcedTint ? forcedTint : (inRange ? validColor : invalidColor);

        if (markerInstance != null)
        {
            markerInstance.transform.position = new Vector3(clampedTarget.x, clampedTarget.y, markerInstance.transform.position.z);
            var sr = markerInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = c;
        }

        if (rangeCircle != null)
        {
            rangeCircle.transform.position = new Vector3(startPos.x, startPos.y, rangeCircle.transform.position.z);
            rangeCircle.color = new Color(c.r, c.g, c.b, 0.18f);
            ApplyRangeCircleScale();
        }

        int samples = Mathf.Max(20, resolution);
        line.positionCount = samples;
        float distance = Vector3.Distance(startPos, clampedTarget);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            Vector3 linear = Vector3.Lerp(startPos, clampedTarget, t);
            float arcY = height * 4f * t * (1f - t);
            line.SetPosition(i, new Vector3(linear.x, linear.y + arcY, linear.z));
        }

        line.startColor = c;
        line.endColor = c;
        if (dashed) line.material.mainTextureScale = new Vector2(distance * textureTilingScale, 1f);
    }

    // ITEM 1.3: CÁLCULO EXATO DA ESCALA DA IMAGEM
    private void ApplyRangeCircleScale()
    {
        if (rangeCircle == null || rangeCircle.sprite == null) return;

        // Pega a largura real da imagem na Unity
        float spriteWidth = rangeCircle.sprite.bounds.size.x;
        if (spriteWidth <= 0) return;

        // O diâmetro desejado é o raio de lançamento vezes 2
        float targetDiameter = maxLaunchRadius * 2f;

        // Calcula a escala exata para atingir esse diâmetro
        float scale = targetDiameter / spriteWidth;
        rangeCircle.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public Vector3 GetClampedTarget(Vector3 startPos, Vector3 targetPos)
    {
        float dist = Vector3.Distance(startPos, targetPos);
        if (dist <= maxLaunchRadius) return targetPos;
        Vector3 dir = (targetPos - startPos).normalized;
        return startPos + dir * maxLaunchRadius;
    }
}