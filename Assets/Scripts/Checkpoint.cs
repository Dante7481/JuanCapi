using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public KeyCode teclaInteraccion = KeyCode.E;
    public bool activado;

    private PlayerController jugadorDentro;

    private void Update()
    {
        if (jugadorDentro != null && !jugadorDentro.isDead && Input.GetKeyDown(teclaInteraccion))
        {
            jugadorDentro.ActivarCheckpoint(transform.position);
            activado = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController jugador = collision.GetComponentInParent<PlayerController>();
        if (jugador != null)
        {
            jugadorDentro = jugador;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        PlayerController jugador = collision.GetComponentInParent<PlayerController>();
        if (jugador != null)
        {
            jugadorDentro = jugador;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController jugador = collision.GetComponentInParent<PlayerController>();
        if (jugador == jugadorDentro)
        {
            jugadorDentro = null;
        }
    }
}
