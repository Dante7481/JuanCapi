using UnityEngine;
using JuanCapi;

public class Moneda : MonoBehaviour
{
    private string id;
    private void Start()
    {
        id = WorldInteraction.StableId(gameObject);
        if (GameRuntime.Instance?.Data?.collected.Contains(id) == true) gameObject.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var game = GameRuntime.Instance;
            if (game?.Data != null && game.Data.Collect(id, "gold", 10))
            {
                game.NotifyChanged(); game.UI.Toast("+10 oro recuperado"); gameObject.SetActive(false);
            }
        }
    }
}
