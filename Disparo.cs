using UnityEngine;

/// <summary>
/// Projétil que anima sua sprite de acordo com a direção do disparo.
/// </summary>
public class Disparo : MonoBehaviour
{
    public Rigidbody2D rb;
    public Animator vfxAnimador;
    public float lifetime = 4f;
    public float velocidade = 12f;
    public float knockbackForce = 2f;
    public GameObject hitImpactPrefab;
    public float hitImpactDuration = 0.2f;

    private Vector2 _direction;
    [SerializeField] private Mon _attacker;
    [SerializeField] private float _damage;
    [SerializeField] private PerformCombat moveSender;

    // Chame essa função ao instanciar o projétil!
    private void Start()
    {
        moveSender = transform.parent.GetComponentInChildren<PerformCombat>();
    }
    public void Initialize(Vector2 direction, float damage, float speed, Mon attacker = null)
    {
        _direction = direction.normalized;
        _attacker = attacker;
        _damage = damage;
        velocidade = speed;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = _direction * velocidade;
        rb.freezeRotation = true;

        // Atualiza parâmetros da animação de acordo com a direção
        if (vfxAnimador != null)
        {
            vfxAnimador.SetFloat("longoX", _direction.x);
            vfxAnimador.SetFloat("longoY", _direction.y);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
       
        if (collision.CompareTag("OponenteHurtBox")) // Mudar para layer, mais especifico
        {
            MonHurtBox hurtBox = collision.GetComponentInParent<MonHurtBox>();
            if (hurtBox != null)
            {
                Vector2 kbDir = (transform.position - collision.transform.position).normalized;
                hurtBox.TakeDamage(knockbackForce, kbDir,_attacker,moveSender.LastUsedAttack.data);
                Debug.Log("Disparo atingiu HurtBox? SIM");
                if (hitImpactPrefab != null)
                {
                    GameObject impact = Instantiate(hitImpactPrefab, collision.transform.position, Quaternion.identity);
                    Destroy(impact, hitImpactDuration);
                }
            }
            else
            {
                Debug.Log("Disparo atingiu HurtBox? NÃO");
            }
            Destroy(gameObject);
        }
        //if (collision.CompareTag("OponenteHurtBox"))
        //{
        //    MonHurtBox hurtBox = collision.GetComponentInParent<MonHurtBox>();
        //    Transform paiTransform = collision.transform;
        //    Transform filhoA = paiTransform.GetChild(2);
        //    Transform netoA1 = filhoA.GetChild(2);
        //    //Transform hurtBoxTransform = collision.gameObject.transform.GetChild(3);
        //    Debug.Log("Disparo atingiu HurtBox: " + netoA1);
        //    //Debug.Log(paiTransform.name);
        //    if (hurtBox != null)
        //    {
        //        Vector2 kbDir = (transform.position - collision.transform.position).normalized;
        //        hurtBox.TakeDamage(_damage, knockbackForce, kbDir, _attacker, moveSender.LastUsedAttack.data);

        //        if (hitImpactPrefab != null)
        //        {
        //            GameObject impact = Instantiate(hitImpactPrefab, collision.transform.position, Quaternion.identity);
        //            Destroy(impact, hitImpactDuration);
        //        }
        //    }
        //    Destroy(gameObject);
        //}
    }
}