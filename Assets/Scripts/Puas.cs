using UnityEngine;

public class Puas : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController jugador = collision.GetComponentInParent<PlayerController>();

        if (jugador != null && !jugador.isDead)
        {
            jugador.RespawnEnCheckpoint();
        }
    }
}
