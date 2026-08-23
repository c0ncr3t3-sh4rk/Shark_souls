using UnityEngine;
using System.Collections;

public class NubeHumo : MonoBehaviour
{
    [Header("Expansión")]
    [SerializeField] private float escalaMaxima = 5f;
    [SerializeField] private float tiempoExpansion = 3f;
    [SerializeField] private float tiempoVida = 6f;

    [Header("Daño")]
    [SerializeField] private int dano = 1;

    [Header("Ceguera")]
    [SerializeField] private float duracionCeguera = 2f;

    private float tiempoCreacion;
    private Vector3 escalaInicial;
    private bool jugadorDanado = false;

    private void Start()
    {
        tiempoCreacion = Time.time;
        escalaInicial = transform.localScale;
        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        // Expandir progresivamente
        float t = Mathf.Clamp01((Time.time - tiempoCreacion) / tiempoExpansion);
        float escalaActual = Mathf.Lerp(0.5f, escalaMaxima, t);
        transform.localScale = escalaInicial * escalaActual;

        // Fade out en los últimos 2 segundos
        float tiempoRestante = tiempoVida - (Time.time - tiempoCreacion);
        if (tiempoRestante < 2f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, 1f, tiempoRestante / 2f);
                sr.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (jugadorDanado) return;

        jugadorDanado = true;

        // Aplicar daño
        SaludTiburon salud = collision.GetComponent<SaludTiburon>();
        if (salud != null && !salud.esInvencible)
        {
            salud.RecibirDano(dano);
        }

        // Aplicar ceguera
        EfectoCeguera ceguera = collision.GetComponent<EfectoCeguera>();
        if (ceguera == null)
        {
            ceguera = collision.gameObject.AddComponent<EfectoCeguera>();
        }
        ceguera.AplicarCeguera(duracionCeguera);
    }
}
