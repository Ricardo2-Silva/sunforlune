using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    [Header("Teclas de Ataque")]
    [SerializeField] private KeyCode[] attackKeys;

    [Header("Ataques Disponíveis")]
    [SerializeField] private List<AssistantAttackClass> availableAttacks = new List<AssistantAttackClass>();

    [Header("Dependências")]
    public PerformCombat combatPerformer;
    public Animator animator;

    public SaudePokemon saudePokemon;

    private Dictionary<KeyCode, bool> keyHeldDown = new Dictionary<KeyCode, bool>();

    private void Awake()
    {

        foreach (KeyCode key in attackKeys)
        {
            keyHeldDown[key] = false;
        }
    }

    private void Update()
    {
        if (combatPerformer == null)
        {
            Debug.LogError("combatPerformer está nulo!");
            return;
        }
        for (int i = 0; i < attackKeys.Length; i++)
        {
            if (i >= availableAttacks.Count) continue;

            float custoPoder = availableAttacks[i].data != null ? availableAttacks[i].data.pontosPoder : 0f;
            if (saudePokemon != null && !saudePokemon.TemPontosPoderPara(custoPoder))
            {
                continue;
            }

            if (Input.GetKeyDown(attackKeys[i]))
            {
                keyHeldDown[attackKeys[i]] = true;

                Vector2 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 direction = (mouse_pos - (Vector2)transform.position).normalized;
                if (animator != null)
                    GestaoAnimador.Animar(transform.position, animator, "AttackX", "AttackY", true);
                TryUseAttack(i, direction);
            }
            else if (Input.GetKeyUp(attackKeys[i]))
            {
                keyHeldDown[attackKeys[i]] = false;

                if (availableAttacks[i].IsChanneled && availableAttacks[i].IsCasting)
                {
                    combatPerformer.CancelAttack(availableAttacks[i]);
                }
            }
            else if (
                i < availableAttacks.Count &&
                availableAttacks[i] != null &&
                availableAttacks[i].data != null &&
                keyHeldDown.ContainsKey(attackKeys[i]) &&
                keyHeldDown[attackKeys[i]] &&
                availableAttacks[i].IsChanneled &&
                availableAttacks[i].IsCasting
            )
            {
                if (availableAttacks[i].ActiveParticleObject != null)
                {
                    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 direction = mousePos - (Vector2)transform.position;
                    //UpdateChanneledDirection(availableAttacks[i], direction.normalized);
                }
            }
        }
    }

    public void TryUseAttack(int index, Vector2 direction)
    {
        if (index < 0 || index >= availableAttacks.Count) return;
        AssistantAttackClass attack = availableAttacks[index];
        if (attack == null) return;

        float custoPoder = attack.data != null ? attack.data.pontosPoder : 0f;
        if (saudePokemon != null && !saudePokemon.TemPontosPoderPara(custoPoder))
        {
            return;
        }

        combatPerformer.PerformManualAttack(attack, direction);
    }

    public void SetAvailableAttacks(List<AssistantAttackClass> newAttacks)
    {
        availableAttacks = newAttacks;
    }
}