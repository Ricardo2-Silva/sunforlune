using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonCaptureSystem : MonoBehaviour
{
    public void CapturePokemon(RoleHandler wildPokemon)
    {
        if (wildPokemon.GetCurrentRole() != PokemonRole.Wild) return;

        // Adiciona ao time do jogador
        PokemonSwitchManager.Instance.AddPokemonToTeam(wildPokemon);

        // Muda o papel para aliado (não ativo imediatamente)
        //DEVE SER ACRESCENTADAS + FUNÇÕES!!!
        wildPokemon.ApplyRole(PokemonRole.AllyAI);

        //Debug.Log($"{wildPokemon.GetMon().Base.Nome} foi capturado!");
    }
}
