using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Attacks/Tackle")]
public class Tackle : AttackData
{
    public override IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance)
    {
        Debug.Log("Tackle Enter");
        //bool canDash = true;

        if (trailRendererPrefab != null)
        {
            GameObject trailObj = GameObject.Instantiate(trailRendererPrefab, self.position, Quaternion.identity,self);
            instance.activeTrail = trailObj.GetComponentInChildren<TrailRenderer>();
            instance.activeTrail.emitting = true;
        }

        Animator animator = self.GetComponentInParent<Animator>();
        //animator.SetBool("Attack", true);
        var originalController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = self.GetComponentInParent<Mon>().Base.posturaNormal;
        
        Rigidbody2D rb = self.GetComponentInParent<Rigidbody2D>();

        // --- Ativar HurtBox ---
        BoxCollider2D hitBox = FindHitBox(self);

        if (hitBox != null)
            hitBox.enabled = true;
        //Debug.Log(hitBox);

        Vector2 startPosition = rb.position;
        Vector2 endPosition = startPosition + direction.normalized * moveSpeed;

        float elapsedTime = 0f;
        //Debug.Log("Tackle - Antes do while");
        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            t = t * t * (3f - 2f * t);

            rb.MovePosition(Vector2.Lerp(startPosition, endPosition, t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Tackle - Depois do while");

        rb.MovePosition(endPosition);
        //canDash = false;

        if (instance.activeTrail != null)
        {
            instance.activeTrail.transform.parent = null;
            instance.activeTrail.emitting = false;
            Object.Destroy(instance.activeTrail.gameObject, instance.activeTrail.time);
            instance.activeTrail = null;
        }
        

        rb.velocity = Vector2.zero;
        

        // --- Desativar HurtBox ---
        if (hitBox != null)
            hitBox.enabled = false;
        //animator.SetBool("Attack", false);
        animator.runtimeAnimatorController = originalController;

        //yield return new WaitForSeconds(1f);
        Debug.Log("Tackle - Fim da rotina (antes de OnAttackEnd)");
        var ai = self.GetComponentInParent<WildPokemonAI>();
        //Debug.Log("Buscando IA a partir de: " + self.name + " Encontrado? " + (ai != null));
        if (ai != null) ai.OnAttackEnd();

    }

    public override void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance)
    {
        throw new System.NotImplementedException();
    }
    private BoxCollider2D FindHitBox(Transform self)
    {
        if (self == null) return null;

        Transform parent = self;
        if (parent == null)
        {
            Debug.LogWarning($"[{self.name}] não tem pai para procurar HitBox.");
            return null;
        }

        // Primeiro tenta "MonHitBox"
        Transform hitBoxTransform = parent.Find("MonHitBox");

        // Se não achou, tenta "OponenteHitBox"
        if (hitBoxTransform == null)
            hitBoxTransform = parent.Find("OponenteHitBox");

        if (hitBoxTransform == null)
        {
            Debug.LogWarning($"Nem MonHitBox nem OponenteHitBox encontrados para [{parent.name}].");
            return null;
        }

        BoxCollider2D collider = hitBoxTransform.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"HitBox [{hitBoxTransform.name}] encontrado, mas sem BoxCollider2D!");
            return null;
        }

        return collider;
    }


    void PlayEffect(Transform self)
    {
        if (attackEffectPrefab != null)
        {
            GameObject effect = GameObject.Instantiate(attackEffectPrefab, self.position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }
    }

}
