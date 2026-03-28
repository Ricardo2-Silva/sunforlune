using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "WaterGun", menuName = "Attacks/WaterGun")]
public class WaterGun : AttackData
{
    [Header("Configuração do Projétil")]
    public GameObject projectilePrefab; // Prefab do projétil
    public float projectileSpeed = 12f; // Velocidade do projétil
    public float spawnOffset = 0.5f;    // Distância inicial do disparo

    public override void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile prefab não definido!");
            return;
        }

        // Calcula a posição inicial do projétil com deslocamento na direção do ataque
        Vector2 offset = direction.normalized * spawnOffset;
        Vector3 spawnPosition = self.position + (Vector3)offset; // CRIAR ANCHOR POINT

        // Calcula a rotação visual para o projétil (sprite aponta para a direita por padrão)
        //float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Instancia o projétil já com a rotação correta
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity, self);
        Mon monComponent = self.GetComponentInParent<Mon>();
        Animator animator = self.GetComponentInParent<Animator>();
        animator.SetBool("Walk", false); // PADRONIZAR
        animator.SetBool("Run", false);   // PADRONIZAR
        animator.SetBool("Attack", true);    // PADRONIZAR
        animator.runtimeAnimatorController = monComponent.Base.longaDistancia;
        // Inicializa o projétil
        Disparo disparo = projectileObj.GetComponent<Disparo>();
        if (disparo != null)
        {
            disparo.Initialize(direction.normalized, damage, projectileSpeed, self.GetComponentInParent<Mon>());
        }
    }

    public override IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance)
    {
        yield break;
    }
}