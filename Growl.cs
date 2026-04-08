using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Attacks/Growl")]
public class Growl : AttackData
{

    public override IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance)
    {

        Debug.Log("Growl Enter");
        Animator animator = self.GetComponentInParent<Animator>();
        animator.SetBool("Attack", true);
        //animator.runtimeAnimatorController = self.GetComponentInParent<Mon>().Base.posturaContinua;
        yield return new WaitForSeconds(5f);
        animator.SetBool("Attack", false);
    }

    public override void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance)
    {
        throw new System.NotImplementedException();
    }
}
