using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class Controles : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;       // centro da elipse
    public Transform arrow;        // seta que orbita
    public Camera cam;

    [Header("Parâmetros da Elipse")]
    public float radiusX = 1f;     // semi-eixo horizontal
    public float radiusY = 0.5f;   // semi-eixo vertical

    [Header("Efeito de Profundidade")]
    public float scaleDepth = 0.3f;    // intensidade da variação de escala
    public float depthOffset = 0.05f;  // deslocamento vertical (opcional)

    [Header("Ordem de Renderização")]
    public SpriteRenderer arrowRenderer;
    public int orderFront = 5;     // frente do jogador
    public int orderBack = 3;      // atrás do jogador

    void Reset() => cam = Camera.main;
    #region Variáveis
    public float acelerar;
    public float correr;
    public Rigidbody2D rigidBody;
    public Animator animador;

    [HideInInspector] public Vector2 movimento;
    [HideInInspector] public Vector2 dirOciosa;
    bool correndo = false;

    public Transform mira;

    [HideInInspector] public Vector2 raycastPos;
    public RaycastHit2D hit;
    public Transform unidadeSelecionada;

    private bool canDash = true;
    private bool isDashing;
    public float dashingPower = 24f;
    public float dashingTime = 0.2f;
    public float dashingCooldown = 1f;

    [SerializeField] private TrailRenderer tr;

    #endregion

    #region Métodos MonoBehaviour
    // Start is called before the first frame update
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (isDashing)
        {
            return;
        }
        Comandos();
        Eixos();
        //MiraPosMouse();
        SelecionarAlvo();
        if (!player || !arrow) return;
        if (!cam) cam = Camera.main;

        // Direção do mouse
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        Vector3 dir = (mouse - player.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x);

        // Posição na elipse
        float x = radiusX * Mathf.Cos(angle);
        float y = radiusY * Mathf.Sin(angle);

        Vector3 pos = player.position + new Vector3(x, y, 0);
        arrow.position = pos;

        // Rotação da seta
        float arrowAngle = angle * Mathf.Rad2Deg;
        arrow.rotation = Quaternion.Euler(0, 0, arrowAngle);

        // --- Simulação de profundidade ---
        float sin = Mathf.Sin(angle);

        // Ajuste de escala (menor quando em cima, maior quando embaixo)
        float depthScale = 1f - (sin * scaleDepth);
        arrow.localScale = new Vector3(depthScale, depthScale, 1f);

        // Deslocamento vertical (faz parecer que sobe/desce)
        arrow.localPosition += new Vector3(0, -sin * depthOffset, 0);

        // Ordem de renderização (atrás ou na frente do jogador)
        if (arrowRenderer)
        {
            arrowRenderer.sortingOrder = sin > 0 ? orderBack : orderFront;
        }
    }
    private void FixedUpdate()
    {


        Evasiva();
        MoverJogador();
        Correr();

    }
    #endregion

    public void MiraPosMouse()// Pode ir para um novo script
    {
        Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);


        Vector3 playerToCursor = mouse_pos - transform.position;

        var angulo = 90 - Mathf.Atan2(playerToCursor.x, playerToCursor.y) * Mathf.Rad2Deg;
        //mira.rotation = Quaternion.Euler(0f, 0f, angulo);
        mira.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
    }
    void MoverJogador()// Criar um script PlayerController para no futuro mudar os controles
    {
        MonHurtBox hurtBox = GetComponentInChildren<MonHurtBox>(); // ou GetComponent<MonHurtBox>() dependendo de onde está

        if (hurtBox != null && hurtBox.isBeingKnockedBack)
        {
            // Se está levando knockback, não move
            return;
        }

        if (animador.GetBool("Attack") == false)
        {
            rigidBody.velocity = (acelerar * Time.deltaTime * movimento);
        }

    }
    void Comandos()
    {
        movimento.x = Input.GetAxisRaw("Horizontal");
        movimento.y = Input.GetAxisRaw("Vertical");
        movimento = Vector2.ClampMagnitude(movimento, 1f);

    }
    void Eixos()//parece correto
    {

        if (correndo == true && movimento != Vector2.zero && animador.GetBool("Attack") == false)
        {

            animador.SetBool("Walk", false);
            animador.SetBool("Run", true);
            animador.SetFloat("WalkX", movimento.x);
            animador.SetFloat("WalkY", movimento.y);
        }
        else if (animador.GetBool("Walk") == true && correndo == true)
        {
            animador.SetBool("Walk", false);
            animador.SetBool("Run", true);
            animador.SetFloat("WalkX", movimento.x);
            animador.SetFloat("WalkY", movimento.y);
        }
        else
        {

            animador.SetBool("Run", false);

            if (animador.GetBool("Run") == false)
            {
                animador.SetFloat("IdleX", dirOciosa.x);
                animador.SetFloat("IdleY", dirOciosa.y);

            }
        }

        if (movimento != Vector2.zero && animador.GetBool("Run") == false && animador.GetBool("Attack") == false)
        {

            animador.SetBool("Walk", true);
            animador.SetFloat("WalkX", movimento.x);
            animador.SetFloat("WalkY", movimento.y);
        }
        else
        {
            animador.SetBool("Walk", false);
            rigidBody.velocity = Vector2.zero;

            if (animador.GetBool("Walk") == false)
            {
                animador.SetFloat("IdleX", dirOciosa.x);
                animador.SetFloat("IdleY", dirOciosa.y);

            }
        }
        if (animador.GetBool("Walk") || animador.GetBool("Run"))
        {
            dirOciosa = movimento;
        }
    }
    void Correr()
    {

        if (Input.GetKey(KeyCode.LeftShift))
        {
            correndo = true;
            if (animador.GetBool("Attack") == false)
            {

                rigidBody.velocity = (correr * Time.deltaTime * movimento);
            }
            else
            {
                movimento = Vector2.zero;
                rigidBody.velocity = Vector2.zero;
            }

        }
        else
        {
            correndo = false;

        }
    }
    void Evasiva()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            StartCoroutine(Dash());
        }

    }
    private IEnumerator Dash()
    {
        // Desativa a capacidade de Dashar novamente até que o Dash atual seja concluído
        canDash = false;
        isDashing = true;

        // Pega a posição inicial do jogador
        Vector2 startPosition = rigidBody.position;

        // Calcula a posição final baseada na direção e na força do Dash
        Vector2 endPosition = startPosition + movimento.normalized * dashingPower;

        // Tempo inicial do Dash
        float elapsedTime = 0f;

        // Ativa o efeito visual do Dash
        tr.emitting = true;

        // Realiza o Dash suavemente ao longo do tempo
        while (elapsedTime < dashingTime)
        {
            // Interpola a posição do jogador do ponto inicial ao ponto final
            rigidBody.MovePosition(Vector2.Lerp(startPosition, endPosition, elapsedTime / dashingTime));

            // Incrementa o tempo passado
            elapsedTime += Time.deltaTime;

            // Aguarda o próximo frame
            yield return null;
        }

        // Garante que o jogador atinja a posição final ao final do Dash
        rigidBody.MovePosition(endPosition);

        // Desativa o efeito visual do Dash
        tr.emitting = false;
        isDashing = false;

        // Restaura a capacidade de Dashar após o cooldown
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }
    void SelecionarAlvo()
    {

        if (Input.GetMouseButtonDown(0))
        {

            raycastPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hit = Physics2D.Raycast(raycastPos, Vector2.zero);
            //alvo = hit.collider.GetComponent<TipoAlvo>();
            //Debug.Log(hit);

            if (hit && hit.collider.transform != null)
            {

                unidadeSelecionada = hit.collider.transform;

            }
            else
            {

                //Debug.Log("nada");
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            unidadeSelecionada = null;
        }
    }
    public void StopAttack()
    {

        animador.SetBool("Attack", false);

        if (animador.GetBool("Attack") == false && movimento != Vector2.zero)
        {
            animador.SetBool("Attack", false);
            animador.SetBool("Walk", true);

        }
        if (animador.GetBool("Attack") == false && movimento == Vector2.zero)
        {

            animador.SetBool("Walk", false);

        }
    }
    void OnDrawGizmos()
    {
    }
}

