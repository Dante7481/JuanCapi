using UnityEngine;
using System.Collections;
using System;

public class Heart : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float duracionAnimacionDanio = 0.5f;

    private Coroutine animacionDanio;
    private Action alTerminarAnimacion;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void ReproducirDanio(Action alTerminar)
    {
        if (animator == null)
        {
            alTerminar?.Invoke();
            return;
        }

        if (animacionDanio != null)
        {
            StopCoroutine(animacionDanio);
        }

        alTerminarAnimacion = alTerminar;
        animator.SetBool("damageH", true);
        animacionDanio = StartCoroutine(RestaurarAnimacion());
    }

    private IEnumerator RestaurarAnimacion()
    {
        yield return new WaitForSeconds(duracionAnimacionDanio);
        animator.SetBool("damageH", false);
        animacionDanio = null;
        alTerminarAnimacion?.Invoke();
        alTerminarAnimacion = null;
    }
}
