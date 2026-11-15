using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public PlayerController jugador;
    public Transform contenedorCorazones;
    public Heart[] corazones;

    private CanvasGroup[] gruposCorazones;
    private int saludAnterior = int.MinValue;

    private void Awake()
    {
        if (jugador == null)
        {
            jugador = FindFirstObjectByType<PlayerController>();
        }

        PrepararCorazones();
    }

    private void Start()
    {
        ActualizarCorazones(true);
    }

    private void Update()
    {
        ActualizarCorazones(false);
    }

    private void PrepararCorazones()
    {
        if ((corazones == null || corazones.Length == 0) && contenedorCorazones != null)
        {
            corazones = new Heart[contenedorCorazones.childCount];

            for (int i = 0; i < corazones.Length; i++)
            {
                corazones[i] = contenedorCorazones.GetChild(i).GetComponent<Heart>();

                if (corazones[i] == null)
                {
                    corazones[i] = contenedorCorazones.GetChild(i).gameObject.AddComponent<Heart>();
                }
            }
        }

        gruposCorazones = new CanvasGroup[corazones?.Length ?? 0];

        for (int i = 0; i < gruposCorazones.Length; i++)
        {
            gruposCorazones[i] = corazones[i].GetComponent<CanvasGroup>();

            if (gruposCorazones[i] == null)
            {
                gruposCorazones[i] = corazones[i].gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void ActualizarCorazones(bool forzarActualizacion)
    {
        if (jugador == null || gruposCorazones == null)
        {
            return;
        }

        int salud = Mathf.Clamp(jugador.health, 0, gruposCorazones.Length);

        if (!forzarActualizacion && salud == saludAnterior)
        {
            return;
        }

        for (int i = 0; i < gruposCorazones.Length; i++)
        {
            if (salud < saludAnterior && i == salud)
            {
                int corazonDanado = i;
                gruposCorazones[i].alpha = 1f;
                corazones[i].ReproducirDanio(() => OcultarCorazon(corazonDanado));
                continue;
            }

            // CanvasGroup oculta el corazón sin desactivar su GameObject ni su Animator.
            gruposCorazones[i].alpha = i < salud ? 1f : 0f;
        }

        saludAnterior = salud;
    }

    private void OcultarCorazon(int indice)
    {
        if (indice >= 0 && indice < gruposCorazones.Length)
        {
            gruposCorazones[indice].alpha = 0f;
        }
    }
}
