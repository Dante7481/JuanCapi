using JuanCapi;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidad = 7f;
    public Animator animator;
    public float fuerzaSalto = 7f;
    public float fuerzaRebote = 5f;
    public float longitudRaycast = .25f;
    public LayerMask layerSuelo;
    // Health is measured in quarter hearts. CampaignData owns the persistent value.
    public int health = 12;
    public bool isDead;
    public bool EnSuelo => enSuelo;
    public bool IsInvulnerable => Time.time < damageUntil;
    public int AttackSequence { get; private set; }
    public float Facing => facing;
    public bool SwingActive => Game!=null&&Game.CanPlay&&attackUntil-Time.time<.34f&&attackUntil-Time.time>.14f;
    public bool IsDashing => Time.time<dashUntil;
    public string VisualState => isDead ? "Death" : Time.time < damageUntil ? "Hurt" : Time.time < dashUntil ? "Dash" : Time.time < attackUntil ? "Melee" : !enSuelo ? (rb != null && rb.linearVelocity.y > .1f ? "Jump" : "Fall") : Mathf.Abs(inputX) > .01f ? "Run" : "Idle";
    public bool InDanger => damageUntil > Time.time || attackUntil > Time.time || dashUntil > Time.time;
    private bool enSuelo;
    private Rigidbody2D rb;
    private Collider2D[] weapons;
    private float inputX, groundedUntil, jumpUntil, damageUntil, attackUntil, dashUntil, dashReady;
    private float facing = 1;
    private GameRuntime Game => GameRuntime.Instance;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); if(animator==null)animator=GetComponent<Animator>();
        var list=new System.Collections.Generic.List<Collider2D>();
        foreach(var c in GetComponentsInChildren<Collider2D>(true)) if(c.CompareTag("weapon"))list.Add(c);
        weapons=list.ToArray();
    }
    private void Start() { ApplyCampaign(); }
    public void ApplyCampaign()
    {
        if(Game?.Data==null)return;
        health=Game.Data.healthQuarters;isDead=health<=0;
    }
    private void Update()
    {
        ApplyCampaign();
        var ground=Physics2D.BoxCast(transform.position+Vector3.up*.1f,new Vector2(.7f,.05f),0,Vector2.down,longitudRaycast+.05f,layerSuelo);
        enSuelo=ground.collider!=null&&ground.normal.y>.5f&&rb.linearVelocity.y<=.1f;
        if(enSuelo)groundedUntil=Time.time+.1f;
        bool allowed=Game!=null&&Game.InputAllowed&&!isDead;
        inputX=allowed&&Time.time>=damageUntil?(GameInput.Held("Derecha")?1:0)-(GameInput.Held("Izquierda")?1:0):0;
        if(allowed)
        {
            bool dashFromGround=enSuelo||groundedUntil>Time.time;
            if(enSuelo&&!InDanger)Game.CaptureSafePosition(transform.position);
            if(GameInput.Down("Saltar"))jumpUntil=Time.time+.12f;
            if(jumpUntil>Time.time&&groundedUntil>Time.time&&Time.time>=damageUntil)
            {rb.linearVelocity=new Vector2(rb.linearVelocity.x,fuerzaSalto);jumpUntil=groundedUntil=0;enSuelo=false;}
            if(GameInput.Down("Atacar")&&Game.Data.sword&&Game.Data.equippedWeapon=="sword")Attack();
            if(GameInput.Down("Dash")&&Game.Data.dash&&dashFromGround&&Time.time>=dashReady)
            {dashUntil=Time.time+.24f;dashReady=Time.time+1.1f;}
        }
        bool attacking=allowed&&Time.time<attackUntil;
        foreach(var weapon in weapons)if(weapon!=null)weapon.enabled=attacking&&attackUntil-Time.time<.34f&&attackUntil-Time.time>.14f;
        Movement();
        if(animator!=null)
        {animator.SetBool("enSuelo",enSuelo);animator.SetBool("damage",Time.time<damageUntil);animator.SetBool("Attack",attacking);animator.SetBool("Die",isDead);}
    }
    private void FixedUpdate()
    {
        if(Game==null||!Game.CanPlay||isDead)return;
        if(Time.time<damageUntil)return;
        rb.linearVelocity=new Vector2(Time.time<dashUntil?facing*17:inputX*velocidad,rb.linearVelocity.y);
    }
    public void Movement()
    {
        if(animator!=null)animator.SetFloat("Movement",inputX);
        if(inputX!=0){facing=Mathf.Sign(inputX);transform.localScale=new Vector3(facing*Mathf.Abs(transform.localScale.x),transform.localScale.y,transform.localScale.z);}
    }
    public void TakeDamage(Vector2 direction,int amount) { DamageQuarters(direction,amount*2); }
    public void DamageQuarters(Vector2 direction,int quarters)
    {
        if(Game==null||!Game.CanPlay||quarters<=0||Time.time<damageUntil||Time.time<dashUntil-.12f)return;
        Game.Data.healthQuarters=Mathf.Max(0,Game.Data.healthQuarters-quarters);damageUntil=Time.time+.7f;
        attackUntil=dashUntil=jumpUntil=0;
        rb.linearVelocity=Vector2.zero;
        if(Game.Data.healthQuarters>0)rb.AddForce(new Vector2(transform.position.x-direction.x,.5f).normalized*fuerzaRebote,ForceMode2D.Impulse);
        ApplyCampaign();Game.NotifyChanged();
    }
    public void StopDamage(){damageUntil=0;}
    public void Attack(){if(Game?.InputAllowed==true&&Game.Data.sword&&Game.Data.equippedWeapon=="sword"&&Time.time>=attackUntil&&Time.time>=damageUntil){attackUntil=Time.time+.48f;AttackSequence++;}}
    public void StopAttack(){attackUntil=0;}
    public void RespawnAt(Vector3 position)
    {
        if(Game?.Data==null||isDead)return;
        Game.Data.healthQuarters=Mathf.Max(0,Game.Data.healthQuarters-2);ApplyCampaign();
        damageUntil=Time.time+.7f;attackUntil=dashUntil=0;rb.linearVelocity=Vector2.zero;
        if(!isDead)transform.position=position;
        Game.NotifyChanged();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {if(collision.CompareTag("attackEnemy"))TakeDamage(collision.transform.position,1);}
    private void OnDrawGizmos(){Gizmos.color=Color.red;Gizmos.DrawLine(transform.position,transform.position+Vector3.down*longitudRaycast);}
}
