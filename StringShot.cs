using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Attacks/StringShot")]
public class StringShot : AttackData
{
    public override IEnumerator AttackRoutine(Transform self, Vector2 direction, AttackInstance instance)
    {
        Debug.Log("String Shot");
        yield return null;
        Debug.Log("EXIT String Shot");
    }

    public override void ExecuteAttack(Transform self, Vector2 direction, AttackInstance instance)
    {
        Debug.Log("String Shot");
        
        Debug.Log("EXIT String Shot");
    }

}
