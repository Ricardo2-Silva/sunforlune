using UnityEngine;

/// <summary>
/// Controla a mão do treinador que segura a pokébola.
/// Troca o sprite da pokébola na mão baseado no pokémon sendo liberado/recolhido.
/// Integra com o Animator para disparar as animações corretas.
/// </summary>
public class TrainerHandController : MonoBehaviour
{
    [Header("Referências")]
    public Animator trainerAnimator;
    public SpriteRenderer handPokeballSprite;    // Sprite da pokébola na mão do treinador
    public Transform handTransform;               // Posição da mão (para spawn da pokébola)

    [Header("Posições da Mão por Direção")]
    public Transform handPositionUp;
    public Transform handPositionDown;
    public Transform handPositionLeft;
    public Transform handPositionRight;

    [Header("Pokébola Padrão")]
    public PokeballData defaultPokeballData;

    // Pokébola atual na mão
    private PokeballData currentPokeballInHand;

    private void Start()
    {
        currentPokeballInHand = defaultPokeballData;
        UpdateHandSprite();
        HideHandPokeball();
    }

    /// <summary>
    /// Define qual pokébola o treinador está segurando.
    /// Troca o sprite na mão.
    /// </summary>
    public void SetPokeballInHand(PokeballData data)
    {
        currentPokeballInHand = data ?? defaultPokeballData;
        UpdateHandSprite();
    }

    /// <summary>
    /// Atualiza o sprite da pokébola na mão.
    /// </summary>
    private void UpdateHandSprite()
    {
        if (handPokeballSprite != null && currentPokeballInHand != null)
        {
            handPokeballSprite.sprite = currentPokeballInHand.pokeballMiniSprite ?? currentPokeballInHand.pokeballSprite;
        }
    }

    /// <summary>
    /// Mostra a pokébola na mão (quando vai lançar ou recolher).
    /// </summary>
    public void ShowHandPokeball()
    {
        if (handPokeballSprite != null)
            handPokeballSprite.enabled = true;
    }

    /// <summary>
    /// Esconde a pokébola na mão.
    /// </summary>
    public void HideHandPokeball()
    {
        if (handPokeballSprite != null)
            handPokeballSprite.enabled = false;
    }

    /// <summary>
    /// Dispara a animação de lançamento do treinador.
    /// </summary>
    public void PlayThrowAnimation(Vector2 direction)
    {
        if (trainerAnimator == null) return;

        trainerAnimator.SetFloat("throwX", direction.x);
        trainerAnimator.SetFloat("throwY", direction.y);
        trainerAnimator.SetTrigger("Throw");
    }

    /// <summary>
    /// Dispara a animação de recall (levantar a mão com pokébola).
    /// </summary>
    public void PlayRecallAnimation(Vector2 direction)
    {
        if (trainerAnimator == null) return;

        trainerAnimator.SetFloat("recallX", direction.x);
        trainerAnimator.SetFloat("recallY", direction.y);
        trainerAnimator.SetTrigger("Recall");
    }

    /// <summary>
    /// Retorna a posição da mão baseada na direção que o treinador está olhando.
    /// </summary>
    public Vector3 GetHandPosition(Vector2 facingDirection)
    {
        if (handTransform != null)
            return handTransform.position;

        // Fallback: seleciona a posição de mão correta baseada na direção
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            return facingDirection.x > 0 ? handPositionRight.position : handPositionLeft.position;
        }
        else
        {
            return facingDirection.y > 0 ? handPositionUp.position : handPositionDown.position;
        }
    }

    /// <summary>
    /// Retorna o PokeballData atual na mão.
    /// </summary>
    public PokeballData GetCurrentPokeballData() => currentPokeballInHand;
}