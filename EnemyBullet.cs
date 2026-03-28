using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet: MonoBehaviour
{
    public Animator vfxAnimador;
    public Rigidbody2D rigidbody2d;
    public float valorDano;
    public float velocidade;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    public void ConfigurarDisparo(Vector2 direcao, float dano, float velocidade)
    {
        this.velocidade = velocidade;
        this.valorDano = dano;

        // Define a direção e a rotação do projétil
        transform.right = direcao;
        rigidbody2d.velocity = direcao * velocidade;

        // Ajusta a animação, se existir
        if (vfxAnimador != null)
        {
            GestaoAnimador.Animar(transform.position, vfxAnimador, "longoX", "longoY",false);
        }

        // Garante que o projétil seja destruído após um tempo para evitar sobrecarga na cena
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            

            // Ativa animação de impacto antes de destruir
            if (vfxAnimador != null)
            {
                //StartCoroutine(ExplodirProjétil());
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private IEnumerator ExplodirProjétil()
    {
        rigidbody2d.velocity = Vector2.zero; // Para o projétil
        vfxAnimador.SetTrigger("Impacto"); // Ativa animação de impacto
        yield return new WaitForSeconds(0.5f); // Aguarda a animação terminar
        Destroy(gameObject);
    }
}

