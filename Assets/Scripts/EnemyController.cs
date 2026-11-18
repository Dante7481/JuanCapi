using System.Collections;
using UnityEngine;
using JuanCapi;

public class EnemyController : MonoBehaviour
{
    public Transform player;
    public Animator animator;
    public float detectionRadius = 5.0f;
    public float speed = 2.0f;
    public int health = 3;
    public float fuerzaRebote = 5f;
    public float duracionAtaque = 1.05f;
    
    private bool enMovement;
    private bool damage;
    private bool attack;
    private bool isDead;
    private bool playerLive;

    private Rigidbody2D rb;
    private Vector2 movement;
    private string persistentId;
    public float longitudRaycast = 0.1f;

    void Start()
    {
        persistentId = WorldInteraction.StableId(gameObject);
        if (GameRuntime.Instance?.Data?.defeated.Contains(persistentId) == true) { gameObject.SetActive(false); return; }
        playerLive = true;
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (player == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) player = jugador.transform;
        }
    }
    void FixedUpdate()
    {
        // La física se mueve en FixedUpdate: antes iba en Update y la velocidad dependía de los FPS.
        if (player != null && playerLive && !isDead)
        {
            Movement();
        }
    }
    void Update()
    {

        animator.SetBool("movement", enMovement);
        animator.SetBool("takedamage", damage);
        animator.SetBool("attack", attack);
        animator.SetBool("Died", isDead);

    }
    public void Movement()
    {
        if (!damage && !isDead && playerLive)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRadius)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                if (direction.x != 0)
                {
                    transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

                }
                movement = new Vector2(direction.x, 0);
                enMovement = true;
            }
            else
            {
                movement = Vector2.zero;
                enMovement = false;
            }
            if (!damage)
            {
                rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
            }
        }
    }
    public void TakeDamage(Vector2 direccion, int cantDamage)
    {
        if (!damage && !isDead && GameRuntime.Instance?.CanPlay == true)
        {
            damage = true;
            health -= cantDamage;
            if (health <= 0)
            {
                isDead = true;
                enMovement = false;
                if (!GameRuntime.Instance.Data.defeated.Contains(persistentId)) GameRuntime.Instance.Data.defeated.Add(persistentId);
                foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;

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

                Attack();

                Vector2 directionDamage = new Vector2(transform.position.x, 0);
                playerController.TakeDamage(directionDamage, 1);
                playerLive = !playerController.isDead;
                if (!playerLive)
                {
                    enMovement = false;
                }
            }
        }
    }

    private void Attack()
    {
        if (attack || isDead || !playerLive)
        {
            return;
        }

        attack = true;
        StartCoroutine(StopAttack());
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
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * longitudRaycast);
    }
}
