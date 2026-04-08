using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfeitosVFX : MonoBehaviour
{
    public Controles vfxPkmn;
    public GameObject efeitosVisuais;
    public Animator vfxAnimador;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (efeitosVisuais.activeSelf)
        {
            //efeitosVisuais.SetActive(true);
            GestaoAnimador.Animar(this.transform.position, vfxAnimador, "vfxX", "vfxY", false);
           /* switch (vfxPkmn.indiceSprite)
            {

                case 0:
                    vfxAnimador.SetFloat("vfxX", 1f);
                    vfxAnimador.SetFloat("vfxY", 0f);
                    break;

                case 1:
                    vfxAnimador.SetFloat("vfxX", 1f);
                    vfxAnimador.SetFloat("vfxY", 1f);
                    break;

                case 2:
                    vfxAnimador.SetFloat("vfxX", 0f);
                    vfxAnimador.SetFloat("vfxY", 1f);
                    break;

                case 3:
                    vfxAnimador.SetFloat("vfxX", -1f);
                    vfxAnimador.SetFloat("vfxY", 1f);
                    break;

                case 4:
                    vfxAnimador.SetFloat("vfxX", -1f);
                    vfxAnimador.SetFloat("vfxY", 0f);
                    break;

                case 5:
                    vfxAnimador.SetFloat("vfxX", -1f);
                    vfxAnimador.SetFloat("vfxY", -1f);
                    break;

                case 6:
                    vfxAnimador.SetFloat("vfxX", 0f);
                    vfxAnimador.SetFloat("vfxY", -1f);
                    break;

                case 7:
                    vfxAnimador.SetFloat("vfxX", 1f);
                    vfxAnimador.SetFloat("vfxY", -1f);
                    break;
            }
           */
        }
    }

    public void StopAttack()
    {
        //vfxAnimador.StopPlayback();
    }
}
