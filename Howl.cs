using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Attacks/Howl")]
public class Howl : AttackData
{
    public override IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance)
    {
        Debug.Log("Howl Enter");
        Animator animator = self.GetComponentInParent<Animator>();

        // 1. Salvar o controller original
        var mon = self.GetComponentInParent<Mon>();
        var originalController = animator.runtimeAnimatorController;

        //animator.SetBool("ataqueSelvagem", true);
       // animator.runtimeAnimatorController = mon.Base.posturaContinua; // Postura especial

        yield return new WaitForSeconds(1f);

        //animator.SetBool("ataqueSelvagem", false);

        // 2. Restaurar o controller original
        animator.runtimeAnimatorController = originalController;
        Debug.Log("Howl Exit");
        var ai = self.GetComponentInParent<WildPokemonAI>();
       //if (ai != null) ai.OnAttackEnd();
        
    }
    public override void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance)
    {
        Debug.Log("Howl Enter");

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
