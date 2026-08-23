using UnityEngine;
using System.Collections;

/// <summary>
/// Carpita pequeña con sombrero y pistola.
/// Spawneada por los huevos del boss.
/// Patrulla por la sala y dispara al jugador periódicamente.
/// Las balas son parryables por el jugador.
/// </summary>
public class CarpitaMinion : MonoBehaviour, IEnemigo
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadMax = 2f;
    [SerializeField] private float distanciaDeteccionPared = 1.5f;
    [SerializeField] private LayerMask capaParedes;

    [Header("Disparo")]
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private float tiempoEntreDisparos = 3f;
    [SerializeField] private float velocidadProyectil = 8f;
    [SerializeField] private Transform puntoDisparo;

    [Header("Aturdimiento")]
    [SerializeField] private float tiempoAturdimientoBase = 1f;

    private Rigidbody2D rb;
    private Transform jugador;
    private Vector2 direccion;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;

    private float tiempoAturdimientoAcumulado = 0f;
    private Coroutine crAturdimiento;
    private bool estaAturdido = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        GameObject objetoJugador = GameObject.FindWithTag("Player");
        if (objetoJugador != null)
        {
            jugador = objetoJugador.transform;
        }

        StartCoroutine(RutinaDisparo());
    }

    private void FixedUpdate()
    {
        if (estaAturdido)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f);
            return;
        }

        Patrulla();
    }

    private void Patrulla()
    {
        tiempoEnEstado += Time.fixedDeltaTime;

        if (tiempoEnEstado >= tiempoLimite)
        {
            tiempoEnEstado = 0f;
            estaMoviendose = !estaMoviendose;

            if (estaMoviendose)
            {
                // Intentar moverse hacia el jugador con algo de aleatoriedad
                if (jugador != null && Random.value > 0.3f)
                {
                    Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
                    direccion = (dirAlJugador + Random.insideUnitCircle * 0.5f).normalized;
                }
                else
                {
                    direccion = Random.insideUnitCircle.normalized;
                }
                tiempoLimite = Random.Range(1f, 3f);
            }
            else
            {
                tiempoLimite = Random.Range(0.5f, 1.5f);
            }
        }

        if (estaMoviendose)
        {
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.3f, direccion, distanciaDeteccionPared, capaParedes);
            if (hit.collider != null)
            {
                direccion = Random.insideUnitCircle.normalized;
            }
        }

        Vector2 velocidadDeseada = estaMoviendose ? (direccion * velocidadMax) : Vector2.zero;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * 4f);

        if (estaMoviendose && rb.linearVelocity.magnitude > 0.1f)
        {
            GirarSprite();
        }
    }

    private IEnumerator RutinaDisparo()
    {
        // Esperar un momento antes del primer disparo
        yield return new WaitForSeconds(tiempoEntreDisparos * 0.5f);

        while (true)
        {
            if (!estaAturdido && jugador != null && prefabProyectil != null)
            {
                Disparar();
            }

            yield return new WaitForSeconds(tiempoEntreDisparos);
        }
    }

    private void Disparar()
    {
        Vector2 posDisparo = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 dirAlJugador = ((Vector2)jugador.position - posDisparo).normalized;

        GameObject proyectilGO = Instantiate(prefabProyectil, posDisparo, Quaternion.identity);
        ProyectilCarpa proyectil = proyectilGO.GetComponent<ProyectilCarpa>();

        if (proyectil != null)
        {
            proyectil.Inicializar(dirAlJugador);
        }
    }

    private void GirarSprite()
    {
        float anguloZ = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (rb.linearVelocity.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (rb.linearVelocity.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    // ============================
    // IEnemigo — Aturdimiento
    // ============================

    public void SumarAturdimiento(float tiempoExtra)
    {
        if (!gameObject.activeInHierarchy) return;

        rb.linearVelocity = Vector2.zero;
        tiempoAturdimientoAcumulado += tiempoExtra;

        if (crAturdimiento == null)
        {
            crAturdimiento = StartCoroutine(Aturdimiento());
        }
    }

    private IEnumerator Aturdimiento()
    {
        estaAturdido = true;

        while (tiempoAturdimientoAcumulado > 0f)
        {
            tiempoAturdimientoAcumulado -= Time.deltaTime;
            yield return null;
        }

        tiempoAturdimientoAcumulado = 0f;
        estaAturdido = false;
        crAturdimiento = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
