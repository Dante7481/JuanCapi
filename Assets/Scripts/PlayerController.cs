using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidad = 10f;
    public Animator animator;
    public float fuerzaSalto = 5f;
    public int saltosMaximos = 2;
    public float fuerzaRebote = 5f;
    public KeyCode teclaDash = KeyCode.LeftShift;
    public float velocidadDash = 18f;
    public float duracionDash = 0.2f;
    public float enfriamientoDash = 0.5f;
    public float longitudRaycast = 0.1f;
    public LayerMask layerSuelo;
    public int health = 5;
    public int saludMaxima = 5;
    public bool EnSuelo => enSuelo;
    public Vector3 PosicionCheckpoint => posicionCheckpoint;


    private bool enSuelo;
    private Rigidbody2D rb;
    private bool damage;
    private bool attack;
    private bool isDashing;
    private float inputX;
    private float dashDirection;
    private float dashTimer;
    private float dashCooldownTimer;
    private int saltosDisponibles;
    private Vector3 posicionCheckpoint;
    public bool isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        if (saludMaxima <= 0)
        {
            saludMaxima = health;
        }

        health = Mathf.Clamp(health, 0, saludMaxima);
        posicionCheckpoint = transform.position;

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        saltosDisponibles = saltosMaximos;

        foreach (Collider2D collider in GetComponents<Collider2D>())
        {
            PhysicsMaterial2D materialSinFriccion = new PhysicsMaterial2D("PlayerSinFriccion")
            {
                friction = 0f,
                bounciness = 0f
            };
            collider.sharedMaterial = materialSinFriccion;
        }
    }

    void Update()
    {
        inputX = (isDead || damage || isDashing) ? 0f : Input.GetAxisRaw("Horizontal");
        Movement();

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        if (!isDead)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, longitudRaycast, layerSuelo);
            enSuelo = hit.collider != null;

            if (enSuelo && rb.linearVelocity.y <= 0f)
            {
                saltosDisponibles = saltosMaximos;
            }

            if (!damage && !isDashing)
            {
                if (Input.GetKeyDown(teclaDash) && dashCooldownTimer <= 0f)
                {
                    StartDash();
                }
                else if (Input.GetKeyDown(KeyCode.Space) && saltosDisponibles > 0)
                {
                    saltosDisponibles--;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
                }

                if (Input.GetKeyDown(KeyCode.Z) && !isDashing)
                {
                    Attack();
                }
            }
        }

        if (animator != null)
        {
            animator.SetBool("enSuelo", enSuelo);
            animator.SetBool("damage", damage);
            animator.SetBool("Attack", attack);
            animator.SetBool("Die", isDead);
            animator.SetBool("IsDash", isDashing);
        }
    }
    void FixedUpdate()
    {
        if (rb == null) return;

        if (damage) return;

        if (isDashing)
        {
            rb.linearVelocity = new Vector2(dashDirection * velocidadDash, 0f);
            return;
        }

        rb.linearVelocity = new Vector2(inputX * velocidad, rb.linearVelocity.y);
    }

    public void Movement()
    {
        if (animator != null)
        {
            animator.SetFloat("Movement", inputX);
        }

        if (inputX != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(inputX) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
    public void TakeDamage(Vector2 direccion, int cantDamage)
    {
        if (!damage)
        {
            isDashing = false;
            dashTimer = 0f;
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
        health = Mathf.Max(0, health - 1);
        damage = false;
        attack = false;
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        saltosDisponibles = saltosMaximos;
        rb.linearVelocity = Vector2.zero;

        if (health <= 0)
        {
            isDead = true;
            return;
        }

        isDead = false;
        transform.position = posicion;
    }

    public void ActivarCheckpoint(Vector3 posicion)
    {
        posicionCheckpoint = posicion;
        health = saludMaxima;
        damage = false;
        attack = false;
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    public void RespawnEnCheckpoint()
    {
        RespawnAt(posicionCheckpoint);
    }

    private void StartDash()
    {
        dashDirection = inputX != 0f ? Mathf.Sign(inputX) : Mathf.Sign(transform.localScale.x);
        if (dashDirection == 0f)
        {
            dashDirection = 1f;
        }

        isDashing = true;
        dashTimer = duracionDash;
        dashCooldownTimer = duracionDash + enfriamientoDash;
        rb.linearVelocity = new Vector2(dashDirection * velocidadDash, 0f);
        transform.localScale = new Vector3(
            dashDirection * Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z);
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
