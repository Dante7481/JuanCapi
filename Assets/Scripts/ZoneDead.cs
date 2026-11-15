using UnityEngine;

public class ZoneDead : MonoBehaviour
{
    public PlayerController jugador;
    public Vector3 ultimaPosicionSegura;

    private void Start()
    {
        if (jugador == null)
        {
            jugador = FindFirstObjectByType<PlayerController>();
        }

        if (jugador != null)
        {
            ultimaPosicionSegura = jugador.transform.position;
        }
    }

    private void Update()
    {
        if (jugador != null && !jugador.isDead && jugador.EnSuelo)
        {
            ultimaPosicionSegura = jugador.transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController jugadorEncontrado = collision.GetComponent<PlayerController>();

            if (jugadorEncontrado != null)
            {
                jugador = jugadorEncontrado;
                jugador.RespawnAt(ultimaPosicionSegura);
            }
        }
    }
}
