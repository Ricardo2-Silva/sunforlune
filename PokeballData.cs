using UnityEngine;

/// <summary>
/// ScriptableObject que define os tipos de Pokébola com suas propriedades visuais e de captura.
/// </summary>
[CreateAssetMenu(fileName = "New Pokeball", menuName = "Pokemon/Pokeball Data")]
public class PokeballData : ScriptableObject
{
    [Header("Identificação")]
    public string pokeballName = "Poké Ball";
    public int pokeballID;

    [Header("Sprites")]
    public Sprite pokeballSprite;           // Sprite da pokébola fechada
    public Sprite pokeballOpenSprite;       // Sprite da pokébola aberta
    public Sprite pokeballMiniSprite;       // Sprite mini para UI/mão do treinador

    [Header("Cores e Efeitos")]
    public Color releaseColor = Color.cyan;         // Cor da luz/partícula ao liberar
    public Color recallColor = Color.red;           // Cor do raio de retorno
    public Color captureGlowColor = Color.white;    // Cor do brilho durante captura

    [Header("Captura")]
    [Range(0.5f, 4f)]
    public float captureRateMultiplier = 1f;        // Multiplicador de taxa de captura
    [Range(0f, 1f)]
    public float criticalCaptureBonus = 0f;         // Bonus de captura crítica

    [Header("Física do Arco")]
    public float arcHeight = 2.5f;                  // Altura do arco de lançamento
    public float travelSpeed = 8f;                  // Velocidade de viagem
    public float spinSpeed = 720f;                  // Velocidade de rotação (graus/segundo)
    public float returnArcHeight = 1.5f;            // Altura do arco de retorno

    [Header("Áudio")]
    public AudioClip launchSound;
    public AudioClip openSound;
    public AudioClip captureSound;
    public AudioClip captureSuccessSound;
    public AudioClip captureFailSound;
    public AudioClip recallSound;
}