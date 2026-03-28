using System;
using System.Collections;
using UnityEngine;

public class PokeballProjectile : MonoBehaviour
{
    [Header("Componentes")]
    public SpriteRenderer spriteRenderer;
    public GameObject breakParticlesPrefab; // Partículas de estilhaço/quebra

    [Header("Configurações de Rotação")]
    public float defaultSpinSpeed = 720f;

    private PokeballData data;
    private bool isSpinning = false;
    private float currentSpinSpeed;
    private Action onArrival;

    public void Initialize(PokeballData pokeballData)
    {
        data = pokeballData;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (data != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = data.pokeballSprite;
            currentSpinSpeed = data.spinSpeed;
        }
        else currentSpinSpeed = defaultSpinSpeed;
    }

    private void Update()
    {
        if (isSpinning) transform.Rotate(0f, 0f, -currentSpinSpeed * Time.deltaTime);
    }

    public void LaunchArc(Vector3 startPos, Vector3 endPos, float arcHeight, float duration, Action onComplete)
    {
        onArrival = onComplete;
        StartCoroutine(ArcTravelRoutine(startPos, endPos, arcHeight, duration));
    }

    private IEnumerator ArcTravelRoutine(Vector3 start, Vector3 end, float height, float duration)
    {
        isSpinning = true;
        float elapsed = 0f;
        transform.position = start;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 linearPos = Vector3.Lerp(start, end, t);
            float arcY = height * 4f * t * (1f - t);
            transform.position = new Vector3(linearPos.x, linearPos.y + arcY, linearPos.z);

            if (t > 0.8f)
            {
                float slowdownT = (t - 0.8f) / 0.2f;
                currentSpinSpeed = Mathf.Lerp(data != null ? data.spinSpeed : defaultSpinSpeed, 90f, slowdownT);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        isSpinning = false;
        transform.rotation = Quaternion.identity;
        currentSpinSpeed = data != null ? data.spinSpeed : defaultSpinSpeed;
        onArrival?.Invoke();
    }

    public IEnumerator BounceAndHover(Vector3 groundPoint, float hoverHeight, float bounceTime)
    {
        transform.position = groundPoint;

        Vector3 targetHover = groundPoint + Vector3.up * hoverHeight;
        float t = 0f;
        isSpinning = true;

        while (t < bounceTime)
        {
            float k = t / bounceTime;
            float y = 1f - Mathf.Pow(1f - k, 2f);
            transform.position = Vector3.Lerp(groundPoint, targetHover, y);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = targetHover;
        isSpinning = false;
        transform.rotation = Quaternion.identity;
        OpenPokeball();
    }

    public IEnumerator CloseAndReturnArc(Vector3 startPos, Vector3 returnPos, float arcHeight, float duration)
    {
        if (data != null && spriteRenderer != null) spriteRenderer.sprite = data.pokeballSprite;
        yield return ArcTravelRoutine(startPos, returnPos, arcHeight, duration);
    }

    public void OpenPokeball()
    {
        isSpinning = false;
        transform.rotation = Quaternion.identity;
        if (data != null && spriteRenderer != null) spriteRenderer.sprite = data.pokeballOpenSprite;
    }

    // ITEM 2.3: Espera no chão, solta partícula e destrói
    public IEnumerator BreakPokeballWithDelay(float delayBeforeBreak)
    {
        yield return new WaitForSeconds(delayBeforeBreak);
        if (breakParticlesPrefab != null) Instantiate(breakParticlesPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void DestroySelf(float delay = 0f) { Destroy(gameObject, delay); }
}