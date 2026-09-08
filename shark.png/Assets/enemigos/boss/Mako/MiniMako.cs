using System.Collections;
using UnityEngine;

public class MiniMako : MonoBehaviour, IVidaBoss
{
    [Header("Configuración Minion")]
    public float dano = 1f;
    public float velocidadEmbestida = 30f;
    public float vidaMax = 3f;
    public float vida;
    public float tMuerte = 10f;
    public GameObject hitboxHijo;

    [Header("Referencias Visuales")]
    public LineRenderer lineaTelegrafiado;
    public GameObject EfectoMuerte;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D miCollider;
    private BossUtils bossUtils;
    private PuntoEmbestida[] puntosDeEntrada;

    private bool esIndependiente = false;
    private bool estaMuerto = false;
    private Coroutine CIA;
    private Coroutine embestidaActual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        miCollider = GetComponent<Collider2D>();

        vida = vidaMax;

        // Obtener la información de los puntos desde el BossUtils de la sala padre
        ObtenerPuntosReferencia();
    }

    private void ObtenerPuntosReferencia()
    {
        bossUtils = GetComponentInParent<BossUtils>();
        if (bossUtils != null)
        {
            puntosDeEntrada = bossUtils.GetPuntosEmbestida();
        }
    }

    // ATAQUES

    public void UnaEmbestida()
    {
        if (!esIndependiente && !estaMuerto)
            embestidaActual = StartCoroutine(Embestida());
    }

    // En la fase tres se desincronizan, en otras palabras cada mako va suelto
    public void Independizar() // A Puigdemont le fliparía este método
    {
        if (CIA == null)
        {
            esIndependiente = true;
            CIA = StartCoroutine(BucleEmbestidas());
        }
    }

    private IEnumerator BucleEmbestidas()
    {
        while (esIndependiente && !estaMuerto)
        {
            embestidaActual = StartCoroutine(Embestida());
            yield return embestidaActual;

            // Pausa aleatoria entre embestidas para crear caos
            float t = UnityEngine.Random.Range(1f, 2.5f);
            yield return new WaitForSeconds(t);
        }
    }

    private IEnumerator Embestida()
    {
        // Reintentar conseguir los puntos por seguridad
        if (puntosDeEntrada == null || puntosDeEntrada.Length == 0)
        {
            ObtenerPuntosReferencia();
            if (puntosDeEntrada == null || puntosDeEntrada.Length == 0) yield break;
        }

        // - Elegir un nodo
        PuntoEmbestida nodoElegido = puntosDeEntrada[UnityEngine.Random.Range(0, puntosDeEntrada.Length)];
        transform.position = nodoElegido.punto.position;

        // - Elegir una dirección
        Vector2 dirEmbestida = nodoElegido.direcciones[UnityEngine.Random.Range(0, nodoElegido.direcciones.Length)];
        GirarSprite(dirEmbestida);

        // - Telegrafiado
        if (lineaTelegrafiado != null)
        {
            lineaTelegrafiado.enabled = true;
            lineaTelegrafiado.SetPosition(0, transform.position);
            lineaTelegrafiado.SetPosition(1, (Vector2)transform.position + (dirEmbestida * 40f));
        }

        yield return new WaitForSeconds(0.4f);

        if (lineaTelegrafiado != null) lineaTelegrafiado.enabled = false;

        // - Ejecutar embestida
        rb.linearVelocity = dirEmbestida * velocidadEmbestida;

        float t = 0f;
        float tMax = 0.8f;
        while (t < tMax)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // - Frenar
        rb.linearVelocity = Vector2.zero;
    }

    // VIDA Y MUERTE
    public void RecibirDano(float d)
    {
        vida -= d;

        if (vida <= 0)
        {
            Morir();
        }
    }

    public void Morir()
    {
        if (estaMuerto) return;

        if (EfectoMuerte != null)
        {
            Instantiate(EfectoMuerte, transform.position, Quaternion.identity);
        }

        StartCoroutine(MuerteYRespawn());
    }

    private IEnumerator MuerteYRespawn()
    {
        estaMuerto = true;
        bool reanudarIndependiente = esIndependiente;
        esIndependiente = false;

        if (CIA != null) StopCoroutine(CIA);
        if (embestidaActual != null) StopCoroutine(embestidaActual);

        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (miCollider != null) miCollider.enabled = false;
        if (lineaTelegrafiado != null) lineaTelegrafiado.enabled = false;
        if (hitboxHijo != null) hitboxHijo.SetActive(false);

        yield return new WaitForSeconds(tMuerte);

        transform.position = new Vector2(-10f, -10f);
        vida = vidaMax;
        estaMuerto = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (miCollider != null) miCollider.enabled = true;
        if (hitboxHijo != null) hitboxHijo.SetActive(true);

        if (reanudarIndependiente)
        {
            Independizar();
        }
    }

    // SPRITE

    private void GirarSprite(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.001f) return;

        float anguloZ = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        Vector3 escala = transform.localScale;
        escala.x = (direccion.x < 0) ? Mathf.Abs(escala.x) : -Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (direccion.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }
}