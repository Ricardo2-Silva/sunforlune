using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public static class GestaoAnimador
{
    static int indiceSprite;
    /*public GestaoAnimador (Vector2 posicao, Animator anim, string stringX, string stringY)
    {

    }*/
    /*public static void Animar(Animator anim, string stringX, string stringY)
    {
        switch (indiceSprite)
        {

            case 0:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, 0f);
                break;

            case 1:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, 1f);
                break;

            case 2:
                anim.SetFloat(stringX, 0f);
                anim.SetFloat(stringY, 1f);
                break;

            case 3:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, 1f);
                break;

            case 4:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, 0f);
                break;

            case 5:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, -1f);
                break;

            case 6:
                anim.SetFloat(stringX, 0f);
                anim.SetFloat(stringY, -1f);
                break;

            case 7:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, -1f);
                break;
        }
    }*/
    public static void Animar(Vector2 posicao, Animator anim, string stringX, string stringY, bool isPlayer)
    {
        if (isPlayer)
        {
            Vector2 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direcao = mouse_pos - posicao;

            float angle = 90 - Mathf.Atan2(direcao.x, direcao.y) * Mathf.Rad2Deg;
            float bearing = (angle + 360) % 360;
            int snapAngle = (int)((((bearing + 22.5f) % 360) + 360) % 360) / 45;

            indiceSprite = snapAngle;
        }
        else
        {
            
            float angle = Mathf.Atan2(posicao.y, posicao.x) * Mathf.Rad2Deg;
            // Convert angle to 0-360 range
            if (angle < 0) angle += 360;
            // Get direction index (0-7 for 8 directions)
            indiceSprite = Mathf.RoundToInt(angle / 45) % 8;
        }




        switch (indiceSprite)
        {

            case 0:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, 0f);
                break;

            case 1:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, 1f);
                break;

            case 2:
                anim.SetFloat(stringX, 0f);
                anim.SetFloat(stringY, 1f);
                break;

            case 3:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, 1f);
                break;

            case 4:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, 0f);
                break;

            case 5:
                anim.SetFloat(stringX, -1f);
                anim.SetFloat(stringY, -1f);
                break;

            case 6:
                anim.SetFloat(stringX, 0f);
                anim.SetFloat(stringY, -1f);
                break;

            case 7:
                anim.SetFloat(stringX, 1f);
                anim.SetFloat(stringY, -1f);
                break;
        }


    }



}
