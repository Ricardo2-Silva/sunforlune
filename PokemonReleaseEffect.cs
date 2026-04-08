using System.Collections;
using UnityEngine;

public class PokemonReleaseEffect : MonoBehaviour
{
    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 200;

    [Header("Configurações")]
    public float flashDuration = 0.3f;
    public float particleDuration = 0.8f;
    public Color baseColor = new Color(0.4f, 0.7f, 1f, 1f);
    public Color flashColor = Color.white;

    [Header("Partículas")]
    public int particleCount = 24;
    public float particleSpeed = 3f;
    public float particleSize = 0.15f;

    public IEnumerator PlayReleaseEffect(Vector3 position, SpriteRenderer pokemonSprite, System.Action onRevealStart)
    {
        GameObject flashObj = CreateFlashObject(position);
        SpriteRenderer flashRenderer = flashObj.GetComponent<SpriteRenderer>();
        ApplySorting(flashRenderer, sortingOrder);

        GameObject[] particles = CreateEnergyParticles(position);
        for (int i = 0; i < particles.Length; i++)
        {
            var sr = particles[i].GetComponent<SpriteRenderer>();
            ApplySorting(sr, sortingOrder + 1);
        }

        float elapsed = 0f;
        float growTime = flashDuration * 0.5f;

        while (elapsed < growTime)
        {
            float t = elapsed / growTime;
            flashObj.transform.localScale = Vector3.one * Mathf.Lerp(0f, 2f, t);

            Color c = Color.Lerp(baseColor, flashColor, t);
            c.a = Mathf.Lerp(0f, 1f, t);
            flashRenderer.color = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        onRevealStart?.Invoke();

        if (pokemonSprite != null)
            pokemonSprite.color = new Color(0.5f, 0.8f, 1f, 0f);

        elapsed = 0f;
        float disperseTime = particleDuration;

        while (elapsed < disperseTime)
        {
            float t = elapsed / disperseTime;

            flashObj.transform.localScale = Vector3.one * Mathf.Lerp(2f, 0f, t);
            Color fc = flashColor;
            fc.a = 1f - t;
            flashRenderer.color = fc;

            for (int i = 0; i < particles.Length; i++)
            {
                float angle = (360f / particleCount) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

                float distance = particleSpeed * t + Mathf.Sin(t * 8f + i) * 0.2f;
                particles[i].transform.position = position + dir * distance;

                float particleAlpha = 1f - t;
                float particleScale = particleSize * (1f - t * 0.5f);
                particles[i].transform.localScale = Vector3.one * particleScale;

                var pr = particles[i].GetComponent<SpriteRenderer>();
                if (pr != null)
                {
                    Color pc = baseColor;
                    pc.a = particleAlpha;
                    pr.color = pc;
                }
            }

            if (pokemonSprite != null)
            {
                float pokemonAlpha = Mathf.Lerp(0f, 1f, t);
                Color pokemonColor = Color.Lerp(new Color(0.5f, 0.8f, 1f), Color.white, t);
                pokemonColor.a = pokemonAlpha;
                pokemonSprite.color = pokemonColor;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pokemonSprite != null)
            pokemonSprite.color = Color.white;

        Destroy(flashObj);
        foreach (var p in particles) Destroy(p);
    }

    private void ApplySorting(SpriteRenderer sr, int order)
    {
        if (sr == null) return;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;
    }

    private GameObject CreateFlashObject(Vector3 position)
    {
        GameObject flash = new GameObject("ReleaseFlash");
        flash.transform.position = position;

        SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = flashColor;
        sr.material = new Material(Shader.Find("Sprites/Default"));

        return flash;
    }

    private GameObject[] CreateEnergyParticles(Vector3 position)
    {
        GameObject[] particles = new GameObject[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"EnergyParticle_{i}");
            particle.transform.position = position;

            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = baseColor;
            sr.material = new Material(Shader.Find("Sprites/Default"));
            particle.transform.localScale = Vector3.one * particleSize;

            particles[i] = particle;
        }

        return particles;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size / 2f;
        float radius = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = 1f - (dist / radius);
                    alpha = alpha * alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}