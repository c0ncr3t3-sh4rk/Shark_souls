using UnityEngine;
using System.Collections;

public class AsfixiaTiburon : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float tiempoLimiteQuieto = 4f;
    [SerializeField] private int danoPorAsfixia = 1; 

    [Header("Visual")]
    [SerializeField] private Color colorAsfixia = new Color(0.2f, 0.5f, 1f, 1f); 
    [SerializeField] private float velocidadParpadeo = 0.15f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SaludTiburon saludTiburon;
    private Coroutine procesoAsfixia;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        saludTiburon = GetComponent<SaludTiburon>();
    }

    private void Update()
    {
        bool estaQuieto = rb.linearVelocity.sqrMagnitude < 0.005f;

        if (estaQuieto && procesoAsfixia == null)
        {
            procesoAsfixia = StartCoroutine(Asfixia());
        }
        else if (!estaQuieto && procesoAsfixia != null)
        {
            StopCoroutine(procesoAsfixia);
            procesoAsfixia = null;
            spriteRenderer.color = Color.white;
        }
    }

    private IEnumerator Asfixia()
    {
        float espera = tiempoLimiteQuieto;

        while (true)
        {
            yield return new WaitForSeconds(espera * 0.5f);

            float tiempoParpadeando = 0f;
            float ritmo = (espera == tiempoLimiteQuieto) ? velocidadParpadeo : (velocidadParpadeo / 2f);

            while (tiempoParpadeando < (espera * 0.5f))
            {
                spriteRenderer.color = (spriteRenderer.color == Color.white) ? colorAsfixia : Color.white;
                yield return new WaitForSeconds(ritmo);
                tiempoParpadeando += ritmo;
            }

            saludTiburon.RecibirDano(danoPorAsfixia);
            
            espera = tiempoLimiteQuieto / 4f;
        }
    }
}
