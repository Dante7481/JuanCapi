using UnityEngine;

public class Moneda : MonoBehaviour
{
    private bool recogida;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (recogida || collision.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        recogida = true;
        ContCoins.Instance?.AgregarMoneda();
        Destroy(gameObject);
    }
}
