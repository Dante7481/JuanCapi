using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    public Animator animator;
    public float detectionRadius = 5.0f;
    public float speed = 2.0f;
    public float distanciaPatrulla = 3.0f;
    public float suavizadoMovimiento = 0.15f;
    public int health = 3;
    public float fuerzaRebote = 5f;
    public float duracionAtaque = 1.05f;
    public LayerMask layerSuelo;
    public float distanciaComprobacionBorde = 0.25f;
    public float alturaComprobacionSuelo = 0.5f;
    public string parametroAtaque = "attack";
    
    private bool enMovement;
    private bool damage;
    private bool attack;
    private bool isDead;
    private bool playerLive;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 movementVelocity;
    private float posicionInicialX;
    private float direccionPatrulla = 1.0f;
    private Collider2D[] attackColliders;
    private Collider2D bodyCollider;
    private Vector2 ultimaPosicionSegura;
    public float longitudRaycast = 0.1f;

    void Start()
    {
        playerLive = true;
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        ultimaPosicionSegura = rb.position;

        PhysicsMaterial2D materialSinFriccion = new PhysicsMaterial2D("EnemySinFriccion")
        {
            friction = 0f,
            bounciness = 0f
        };
        foreach (Collider2D collider in GetComponents<Collider2D>())
        {
            collider.sharedMaterial = materialSinFriccion;
        }

        posicionInicialX = transform.position.x;
        direccionPatrulla = Mathf.Sign(transform.localScale.x);
        if (direccionPatrulla == 0)
        {
            direccionPatrulla = 1.0f;
        }
        if (animator == null) animator = GetComponent<Animator>();
        attackColliders = GetComponentsInChildren<Collider2D>();
        if (player == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) player = jugador.transform;
        }
    }
    void FixedUpdate()
    {
        // La física se mueve en FixedUpdate: antes iba en Update y la velocidad dependía de los FPS.
        if (playerLive && !isDead)
        {
            LimitarAlUltimoPuntoSeguro();
            Movement();
        }
    }
    void Update()
    {

        if (animator != null)
        {
            animator.SetBool("movement", enMovement);
            animator.SetBool("takedamage", damage);
            if (!string.IsNullOrEmpty(parametroAtaque))
            {
                animator.SetBool(parametroAtaque, attack);
            }
            animator.SetBool("Died", isDead);
        }

    }
    public void Movement()
    {
        if (damage || isDead || !playerLive || rb == null)
        {
            return;
        }

        Vector2 targetMovement = Vector2.zero;
        bool playerDetected = player != null &&
            Vector2.Distance(rb.position, player.position) <= detectionRadius;

        if (playerDetected)
        {
            float directionX = Mathf.Sign(player.position.x - rb.position.x);
            targetMovement = new Vector2(directionX, 0.0f);
        }
        else
        {
            float limitePatrulla = posicionInicialX + (direccionPatrulla * distanciaPatrulla);
            if (Mathf.Abs(rb.position.x - limitePatrulla) <= 0.05f ||
                Mathf.Sign(limitePatrulla - rb.position.x) != direccionPatrulla)
            {
                direccionPatrulla *= -1.0f;
            }

            targetMovement = new Vector2(direccionPatrulla, 0.0f);
        }

        if (targetMovement.x != 0f && layerSuelo.value != 0 && !HaySueloDelante(targetMovement.x))
        {
            if (!playerDetected)
            {
                direccionPatrulla *= -1.0f;
                targetMovement = new Vector2(direccionPatrulla, 0.0f);
            }
            else
            {
                targetMovement = Vector2.zero;
            }
        }

        Vector2 targetVelocity = targetMovement * speed;
        movement = Vector2.SmoothDamp(
            movement,
            targetVelocity,
            ref movementVelocity,
            suavizadoMovimiento,
            Mathf.Infinity,
            Time.fixedDeltaTime);

        if (Mathf.Abs(movement.x) > 0.01f)
        {
            float facingDirection = Mathf.Sign(movement.x);
            transform.localScale = new Vector3(
                facingDirection * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }

        enMovement = Mathf.Abs(movement.x) > 0.01f;
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    private bool HaySueloDelante(float direccion)
    {
        if (bodyCollider == null)
        {
            return true;
        }

        Bounds bounds = bodyCollider.bounds;
        float origenX = direccion > 0f ? bounds.max.x : bounds.min.x;
        Vector2 origen = new Vector2(origenX + direccion * distanciaComprobacionBorde, bounds.min.y + 0.05f);
        return Physics2D.Raycast(origen, Vector2.down, alturaComprobacionSuelo, layerSuelo).collider != null;
    }

    private bool HaySueloDebajo()
    {
        if (bodyCollider == null || layerSuelo.value == 0)
        {
            return true;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 origen = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
        return Physics2D.Raycast(origen, Vector2.down, alturaComprobacionSuelo, layerSuelo).collider != null;
    }

    private void LimitarAlUltimoPuntoSeguro()
    {
        if (layerSuelo.value == 0 || HaySueloDebajo())
        {
            ultimaPosicionSegura = rb.position;
            return;
        }

        if (rb.linearVelocity.y > 0.05f)
        {
            return;
        }

        rb.position = ultimaPosicionSegura;
        rb.linearVelocity = Vector2.zero;
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
                enMovement = false;

            }
            if (!isDead)
            {
                Vector2 rebote = new Vector2(transform.position.x - direccion.x, 0.5f).normalized;
                rb.AddForce(rebote * fuerzaRebote, ForceMode2D.Impulse);
                StartCoroutine(StopDamage());
            }
            
        }
    }
    IEnumerator StopDamage()
    {
        yield return new WaitForSeconds(0.5f);
        damage = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                if (!playerLive || isDead)
                {
                    return;
                }

                TryAttack(playerController);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        PlayerController playerController = collision.GetComponentInParent<PlayerController>();
        if (playerController == null || !IsAttackRangeTouching(collision))
        {
            return;
        }

        TryAttack(playerController);
    }

    private bool IsAttackRangeTouching(Collider2D playerCollider)
    {
        if (attackColliders == null)
        {
            return false;
        }

        foreach (Collider2D attackCollider in attackColliders)
        {
            if (attackCollider != null && attackCollider.CompareTag("attackEnemy") &&
                attackCollider.isTrigger && attackCollider.IsTouching(playerCollider))
            {
                return true;
            }
        }

        return false;
    }

    private void TryAttack(PlayerController playerController)
    {
        if (!Attack())
        {
            return;
        }

        Vector2 directionDamage = new Vector2(transform.position.x, 0);
        playerController.TakeDamage(directionDamage, 1);
        playerLive = !playerController.isDead;
        if (!playerLive)
        {
            enMovement = false;
        }
    }

    private bool Attack()
    {
        if (attack || isDead || !playerLive)
        {
            return false;
        }

        attack = true;
        StartCoroutine(StopAttack());
        return true;
    }

    private IEnumerator StopAttack()
    {
        yield return new WaitForSeconds(duracionAtaque);
        attack = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("weapon"))
        {
            Vector2 directionDamage = new Vector2(collision.gameObject.transform.position.x, 0);
            TakeDamage(directionDamage, 1);
        }
    }
    public void DestroyBody ()
    {
        Destroy(gameObject);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * longitudRaycast);
    }
}
