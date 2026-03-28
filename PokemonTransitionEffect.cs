using System.Collections;
using UnityEngine;

public class PokemonTransitionEffect : MonoBehaviour
{
    public enum TransitionType { Release, Recall }

    [Header("Cores do Efeito (Configure via Prefab)")]
    public Color outlineColor = new Color(1f, 0.1f, 0.1f, 1f);
    public Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);

    [Header("Configurações Visuais")]
    public float outlineThickness = 0.05f;
    public float pulseSpeed = 15f;

    [Header("Partículas Opcionais (Anexe no Prefab)")]
    public ParticleSystem effectParticles;

    private GameObject[] outlineObjects;
    private SpriteRenderer[] outlineRenderers;

    private static readonly Vector2[] outlineDirections = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1,1).normalized, new Vector2(1,-1).normalized,
        new Vector2(-1,1).normalized, new Vector2(-1,-1).normalized
    };

    public IEnumerator PlayTransition(SpriteRenderer targetSprite, TransitionType type, float duration, System.Action onComplete)
    {
        if (targetSprite == null) { onComplete?.Invoke(); yield break; }
        if (effectParticles != null) effectParticles.Play();

        CreateOutline(targetSprite);
        Vector3 originalScale = targetSprite.transform.localScale;
        if (originalScale == Vector3.zero) originalScale = Vector3.one;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float flashLerp = Mathf.PingPong(elapsed * pulseSpeed, 0.7f);
            targetSprite.color = Color.Lerp(Color.white, flashColor, flashLerp);

            if (outlineRenderers != null)
            {
                for (int i = 0; i < outlineRenderers.Length; i++)
                    if (outlineRenderers[i] != null) outlineRenderers[i].sprite = targetSprite.sprite;
            }

            if (type == TransitionType.Release)
            {
                float scaleCurve = 1f - Mathf.Pow(1f - t, 3f);
                targetSprite.transform.localScale = originalScale * scaleCurve;
            }
            else
            {
                float scaleCurve = 1f - (t * t * t);
                targetSprite.transform.localScale = originalScale * scaleCurve;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        targetSprite.transform.localScale = (type == TransitionType.Release) ? originalScale : Vector3.zero;
        targetSprite.color = Color.white;

        DestroyOutline();
        onComplete?.Invoke();
    }

    private void CreateOutline(SpriteRenderer target)
    {
        outlineObjects = new GameObject[outlineDirections.Length];
        outlineRenderers = new SpriteRenderer[outlineDirections.Length];
        Material solidColorMat = new Material(Shader.Find("GUI/Text Shader"));

        for (int i = 0; i < outlineDirections.Length; i++)
        {
            GameObject outline = new GameObject($"OutlineLayer_{i}");
            outline.transform.SetParent(target.transform);
            outline.transform.localPosition = (Vector3)(outlineDirections[i] * outlineThickness);
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one;

            SpriteRenderer sr = outline.AddComponent<SpriteRenderer>();
            sr.sprite = target.sprite;
            sr.color = outlineColor;
            sr.sortingLayerName = target.sortingLayerName;
            sr.sortingOrder = target.sortingOrder - 1;
            sr.material = solidColorMat;

            outlineObjects[i] = outline;
            outlineRenderers[i] = sr;
        }
    }

    private void DestroyOutline()
    {
        if (outlineObjects != null)
        {
            for (int i = 0; i < outlineObjects.Length; i++)
                if (outlineObjects[i] != null) Destroy(outlineObjects[i]);
            outlineObjects = null;
            outlineRenderers = null;
        }
    }
}