using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EnergyBeamRenderer : MonoBehaviour
{
    [Header("Renderização da Linha")]
    [Tooltip("Quantidade de vértices. Quanto maior, mais curvo e detalhado será o zig-zag.")]
    public int segmentCount = 50;

    [Header("Distorção")]
    public bool useDistortion = true;
    public float distortionAmplitude = 0.5f;
    public float distortionFrequency = 5f;
    public float distortionSpeed = 15f;
    public float noiseScale = 2f;

    private LineRenderer lr;
    private Vector3 beamStart;
    private Vector3 beamEnd;
    private float tAnim;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.enabled = false;

        // Define a quantidade de quebras que a linha tem para poder dobrar
        lr.positionCount = segmentCount;
    }

    private void Update()
    {
        if (!lr.enabled) return;

        tAnim += Time.deltaTime;
        UpdateBeamPositions(beamStart, beamEnd);
    }

    public void SetBeamEnabled(bool enabled)
    {
        lr.enabled = enabled;
        if (enabled) tAnim = 0f;
    }

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        beamStart = start;
        beamEnd = end;
        UpdateBeamPositions(start, end);
    }

    public IEnumerator ExtendTo(Vector3 start, Vector3 end, float duration)
    {
        beamStart = start;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            beamEnd = Vector3.Lerp(start, end, 1f - Mathf.Pow(1f - t, 2f)); // Ease out
            UpdateBeamPositions(beamStart, beamEnd);
            elapsed += Time.deltaTime;
            yield return null;
        }
        beamEnd = end;
        UpdateBeamPositions(beamStart, beamEnd);
    }

    public IEnumerator RetractEndToStart(Vector3 startFixed, float duration)
    {
        beamStart = startFixed;
        Vector3 initialEnd = beamEnd;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            beamEnd = Vector3.Lerp(initialEnd, beamStart, t * t); // Ease in
            UpdateBeamPositions(beamStart, beamEnd);
            elapsed += Time.deltaTime;
            yield return null;
        }

        beamEnd = beamStart;
        UpdateBeamPositions(beamStart, beamEnd);
    }

    private void UpdateBeamPositions(Vector3 start, Vector3 end)
    {
        if (segmentCount < 2) return;

        lr.positionCount = segmentCount;
        Vector3 dir = (end - start);
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            if (useDistortion && i > 0 && i < segmentCount - 1)
            {
                // Edge factor garante que a linha saia e chegue reta nas duas pontas e bagunce só no meio
                float edge = Mathf.Sin(t * Mathf.PI);

                float noise = Mathf.PerlinNoise(i * noiseScale + tAnim * distortionSpeed, tAnim * distortionSpeed * 0.7f) * 2f - 1f;
                float wave = Mathf.Sin(t * distortionFrequency * Mathf.PI + tAnim * distortionSpeed);

                float offset = (wave * 0.6f + noise * 0.4f) * distortionAmplitude * edge;
                pos += perp * offset;
            }
            lr.SetPosition(i, pos);
        }
    }
}