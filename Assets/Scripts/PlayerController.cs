using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidad = 10f;
    public Animator animator;
    public float fuerzaSalto = 5f;
    public float fuerzaRebote = 5f;
    public float longitudRaycast = 0.1f;
    public LayerMask layerSuelo;
    public int health = 5;
    public bool EnSuelo => enSuelo;


    private bool enSuelo;
    private Rigidbody2D rb;
    private bool damage;
    private bool attack;
    private float inputX;
    public bool isDead;

    void Start()    
    {

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Solo se lee la entrada aquí; el movimiento se aplica en FixedUpdate (física).
        inputX = (isDead || damage) ? 0f : Input.GetAxisRaw("Horizontal");
        Movement();

        if (!isDead)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, longitudRaycast, layerSuelo);
            enSuelo = hit.collider != null;

            if (!damage)
            {
                if (enSuelo && Input.GetKeyDown(KeyCode.Space))
                {
                    // Velocidad directa: un impulso se dividía entre la masa (3) y el salto salía diminuto.
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
                }

                if (Input.GetKeyDown(KeyCode.Z))
                {
                    Attack();
                }
            }
        }

        animator.SetBool("enSuelo", enSuelo);
        animator.SetBool("damage", damage);
        animator.SetBool("Attack", attack);
        animator.SetBool("Die", isDead);
    }
    void FixedUpdate()
    {
        // Durante el rebote por daño no se toca la velocidad para respetar el empujón.
        if (damage) return;

        rb.linearVelocity = new Vector2(inputX * velocidad, rb.linearVelocity.y);
    }

    public void Movement()
    {
        animator.SetFloat("Movement", inputX);

        if (inputX != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(inputX) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
    public void TakeDamage(Vector2 direccion, int cantDamage)
    {
        if (!damage)
        {
            damage = true;
            health -= cantDamage;
            if (health <= 0)
            {
                isDead = true;

            }
            if (!isDead)
            {
                Vector2 rebote = new Vector2(transform.position.x - direccion.x, 0.5f).normalized;
                rb.AddForce(rebote * fuerzaRebote, ForceMode2D.Impulse);
                Invoke(nameof(StopDamage), 0.5f);
            }
            
        }
    }
    public void StopDamage()
    {
        damage = false;
        rb.linearVelocity = Vector2.zero;
    }
    public void Attack()
    {
        attack = true;
        
    }
    public void StopAttack()
    {
        attack = false;
    }

    public void RespawnAt(Vector3 posicion)
    {
        CancelInvoke(nameof(StopDamage));
        health--;
        damage = false;
        attack = false;
        rb.linearVelocity = Vector2.zero;

        if (health <= 0)
        {
            isDead = true;
            return;
        }

        isDead = false;
        transform.position = posicion;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("attackEnemy"))
        {
            Vector2 directionDamage = new Vector2(collision.gameObject.transform.position.x, 0);
            TakeDamage(directionDamage, 1);
        }
    }
    void OnDrawGizmos()
    {
       Gizmos.color = Color.red;
       Gizmos.DrawLine(transform.position, transform.position + Vector3.down * longitudRaycast);
    }
}
