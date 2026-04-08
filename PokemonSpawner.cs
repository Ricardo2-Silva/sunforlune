using UnityEngine;

/// <summary>
/// Responsável por instanciar Pokémons fechados (Prefabs prontos) no mundo,
/// sortear o nível e aplicar o papel de Selvagem (Wild).
/// </summary>
public class PokemonSpawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    [Tooltip("O Prefab fechado do Pokémon (já contém Mon, DadosPokemon, etc.)")]
    public GameObject pokemonPrefab;

    [Header("Configurações de Nível")]
    public int nivelMinimo = 2;
    public int nivelMaximo = 5;

    [Header("Área de Spawn")]
    [Tooltip("O raio ao redor deste ponto onde o Pokémon pode nascer")]
    public float raioDeSpawn = 3f;

    [Header("Debug")]
    [Tooltip("Se marcado, irá instanciar um Pokémon assim que o jogo começar")]
    public bool spawnNoStart = false;

    private void Start()
    {
        if (spawnNoStart)
        {
            SpawnarPokemon();
        }
    }

    /// <summary>
    /// Instancia um Pokémon na cena, sorteia seu nível usando a base já existente nele, e define seu papel como Selvagem.
    /// </summary>
    /// <returns>O GameObject do Pokémon recém-criado</returns>
    public GameObject SpawnarPokemon()
    {
        if (pokemonPrefab == null)
        {
            Debug.LogError("[PokemonSpawner] Prefab do Pokémon não foi atribuído no Inspector!");
            return null;
        }

        // 1. Escolhe uma posição aleatória dentro do raio definido
        Vector2 posicaoAleatoria = (Vector2)transform.position + (Random.insideUnitCircle * raioDeSpawn);

        // 2. Instancia o prefab na cena
        GameObject novoPokemonObj = Instantiate(pokemonPrefab, posicaoAleatoria, Quaternion.identity);

        // 3. Busca o script Mon para randomizar o nível e atualizar os stats baseados no Prefab Fechado
        Mon mon = novoPokemonObj.GetComponentInChildren<Mon>();
        if (mon != null && mon.Base != null)
        {
            int nivelSorteado = Random.Range(nivelMinimo, nivelMaximo + 1);
            // Re-alimenta o próprio dado base para que o Mon recalcule HP, Ataque, Natureza para o novo nível
            mon.SetarDados(mon.Base, nivelSorteado);

            // Renomeia na hierarquia para facilitar o debug
            novoPokemonObj.name = $"Wild_{mon.Base.Nome}_Lv{nivelSorteado}";
        }
        else
        {
            Debug.LogWarning("[PokemonSpawner] Script 'Mon' não encontrado no prefab instanciado ou 'Base' não está configurada no Inspector do Prefab.");
        }

        // 4. Busca o RoleHandler para definir o papel inicial como Wild (Selvagem)
        RoleHandler roleHandler = novoPokemonObj.GetComponentInChildren<RoleHandler>();
        if (roleHandler != null)
        {
            roleHandler.ApplyRole(PokemonRole.Wild);
        }

        return novoPokemonObj;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, raioDeSpawn);
    }
}