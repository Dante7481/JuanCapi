using UnityEngine;
using TMPro;

public class ContCoins : MonoBehaviour
{
    public static ContCoins Instance { get; private set; }
    public TMP_Text ContM;
    public int monedas { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ActualizarTexto();
    }

    public void AgregarMoneda()
    {
        monedas++;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (ContM != null)
        {
            ContM.text = $" {monedas}";
        }
    }
}
