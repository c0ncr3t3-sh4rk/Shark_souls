using UnityEngine;
using System.Collections;

public class CarpaBoss : MonoBehaviour, IEnemigo
{
    private enum EstadoBoss
    {
        Idle,
        AtaqueThompson,
        Aturdido,
        Muerto
    }

    [Header("Estado (Debug)")]
    [SerializeField] private EstadoBoss estadoActual = EstadoBoss.Idle;

    [Header("Movimiento")]
    [SerializeField] private float velocidadMax = 3.5f;
    [SerializeField] private float distanciaDeteccionPared = 1.5f;
    [SerializeField] private LayerMask capaParedes;

    [Header("Ataque Thompson")]
    [SerializeField] private ZonaThompson zonaThompson;
    [SerializeField] private float cooldownThompson = 10f;
    [Tooltip("Tiempo que tarda la animación de preparación antes de disparar")]
    [SerializeField] private float tiempoPreparacion = 0.8f;

    [Header("Sprites")]
    [Tooltip("Sprite normal del boss (idle/patrulla)")]
    [SerializeField] private Sprite spriteNormal;
    [Tooltip("Sprite de preparación cuando va a sacar la Thompson")]
    [SerializeField] private Sprite spritePreparacion;

    [Header("Tiempos entre Ataques")]
    [SerializeField] private float tiempoEntreAtaquesMin = 3f;
    [SerializeField] private float tiempoEntreAtaquesMax = 5f;

    [Header("Boss Bar")]
    [SerializeField] private BossBar bossBar;

    private Rigidbody2D rb;
    private Transform jugador;
    private VidaEnemigo vidaEnemigo;
    private SpriteRenderer spriteRenderer;

    private float ultimoAtaqueThompson = -999f;
    private float tiempoProximoAtaque = 0f;

    private Vector2 direccion;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;

    private float tiempoAturdimientoAcumulado = 0f;
    private Coroutine crAturdimiento;
    private bool ataqueEnCurso = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        vidaEnemigo = GetComponent<VidaEnemigo>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject objetoJugador = GameObject.FindWithTag("Player");
        if (objetoJugador != null)
            jugador = objetoJugador.transform;

        // Comprobación de seguridad para BossBar (por si es un prefab)
        if (bossBar != null && !bossBar.gameObject.scene.IsValid())
        {
            bossBar = Instantiate(bossBar);
        }

        if (bossBar == null)
        {
            bossBar = FindAnyObjectByType<BossBar>(FindObjectsInactive.Include);
            if (bossBar == null)
            {
                Debug.Log("[CarpaBoss] No se encontró BossBar en la escena. Creando una automáticamente...");
                GameObject bossBarGO = new GameObject("BossBar (Auto)");
                bossBar = bossBarGO.AddComponent<BossBar>();
            }
        }

        if (vidaEnemigo == null)
        {
            Debug.LogError("[CarpaBoss] No se encontró el componente VidaEnemigo en este objeto.");
        }

        if (bossBar != null && vidaEnemigo != null)
        {
            Debug.Log("[CarpaBoss] Vinculando BossBar...");
            bossBar.Vincular(vidaEnemigo);
        }
        else
        {
            Debug.LogWarning($"[CarpaBoss] No se pudo vincular. bossBar={bossBar != null}, vidaEnemigo={vidaEnemigo != null}");
        }

        if (vidaEnemigo != null)
            vidaEnemigo.OnMuerto += OnMuerte;

        if (spriteNormal == null && spriteRenderer != null)
            spriteNormal = spriteRenderer.sprite;

        // Comprobación de seguridad: Si el usuario arrastró un prefab en vez del objeto de la escena
        if (zonaThompson != null && !zonaThompson.gameObject.scene.IsValid())
        {
            zonaThompson = Instantiate(zonaThompson, transform);
            zonaThompson.transform.localPosition = Vector3.zero;
        }

        tiempoProximoAtaque = Time.time + tiempoEntreAtaquesMin;
    }

    private void OnDestroy()
    {
        if (vidaEnemigo != null)
            vidaEnemigo.OnMuerto -= OnMuerte;
    }

    private void Update()
    {
        if (jugador == null) return;
        if (estadoActual == EstadoBoss.Muerto) return;
        if (estadoActual == EstadoBoss.Aturdido) return;

        if (estadoActual == EstadoBoss.Idle && !ataqueEnCurso && Time.time >= tiempoProximoAtaque)
        {
            if (Time.time - ultimoAtaqueThompson > cooldownThompson)
                IniciarAtaqueThompson();
            else
                tiempoProximoAtaque = Time.time + 1f;
        }
    }

    private void FixedUpdate()
    {
        if (estadoActual == EstadoBoss.Muerto) return;

        switch (estadoActual)
        {
            case EstadoBoss.Idle:
                Patrulla();
                break;

            case EstadoBoss.Aturdido:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f);
                break;

            case EstadoBoss.AtaqueThompson:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f);
                break;
        }
    }

    // ===================== THOMPSON =====================

    private void IniciarAtaqueThompson()
    {
        estadoActual = EstadoBoss.AtaqueThompson;
        ataqueEnCurso = true;
        ultimoAtaqueThompson = Time.time;
        StartCoroutine(SecuenciaThompson());
    }

    private IEnumerator SecuenciaThompson()
    {
        if (zonaThompson == null || jugador == null)
        {
            FinalizarAtaque();
            yield break;
        }

        Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        GirarSprite(dirAlJugador);

        // --- Animación de preparación: cambiar sprite y hacer un pequeño "temblor" ---
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null && spritePreparacion != null)
            spriteRenderer.sprite = spritePreparacion;

        // Esperar el tiempo de preparación (la animación/sprite ya nos da el feedback visual)
        yield return new WaitForSeconds(tiempoPreparacion);

        // --- Lanzar el ataque Thompson ---
        zonaThompson.IniciarAtaque(dirAlJugador, () =>
        {
            // Restaurar sprite normal al terminar
            if (spriteRenderer != null && spriteNormal != null)
                spriteRenderer.sprite = spriteNormal;

            FinalizarAtaque();
        });
    }

    // ===================== PATRULLA =====================

    private void Patrulla()
    {
        tiempoEnEstado += Time.fixedDeltaTime;

        if (tiempoEnEstado >= tiempoLimite)
        {
            tiempoEnEstado = 0f;
            estaMoviendose = !estaMoviendose;

            if (estaMoviendose)
            {
                if (jugador != null && Random.value > 0.4f)
                {
                    Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
                    direccion = (dirAlJugador + Random.insideUnitCircle * 0.8f).normalized;
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
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, direccion, distanciaDeteccionPared, capaParedes);
            if (hit.collider != null)
                direccion = Random.insideUnitCircle.normalized;
        }

        Vector2 velocidadDeseada = estaMoviendose ? (direccion * velocidadMax) : Vector2.zero;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * 4f);

        if (jugador != null)
        {
            Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
            GirarSprite(dirAlJugador);
        }
        else if (estaMoviendose && rb.linearVelocity.magnitude > 0.1f)
        {
            GirarSprite(rb.linearVelocity);
        }
    }

    // ===================== UTILIDADES =====================

    private void GirarSprite(Vector2 dir)
    {
        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (dir.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (dir.x < 0) anguloZ += 180f;
        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    private void FinalizarAtaque()
    {
        ataqueEnCurso = false;
        estadoActual = EstadoBoss.Idle;
        tiempoProximoAtaque = Time.time + Random.Range(tiempoEntreAtaquesMin, tiempoEntreAtaquesMax);
    }

    // ===================== ATURDIMIENTO =====================

    public void SumarAturdimiento(float tiempoExtra)
    {
        if (!gameObject.activeInHierarchy) return;
        if (estadoActual == EstadoBoss.Muerto) return;

        if (ataqueEnCurso)
        {
            StopCoroutine(nameof(SecuenciaThompson));

            if (zonaThompson != null)
                zonaThompson.DetenerAtaque();

            // Restaurar sprite normal si estaba en preparación
            if (spriteRenderer != null && spriteNormal != null)
                spriteRenderer.sprite = spriteNormal;

            ataqueEnCurso = false;
        }

        rb.linearVelocity = Vector2.zero;
        tiempoAturdimientoAcumulado += tiempoExtra;

        if (crAturdimiento == null)
            crAturdimiento = StartCoroutine(RutinaAturdimiento());
    }

    private IEnumerator RutinaAturdimiento()
    {
        estadoActual = EstadoBoss.Aturdido;

        while (tiempoAturdimientoAcumulado > 0f)
        {
            tiempoAturdimientoAcumulado -= Time.deltaTime;
            yield return null;
        }

        tiempoAturdimientoAcumulado = 0f;
        crAturdimiento = null;
        FinalizarAtaque();
    }

    // ===================== MUERTE =====================

    private void OnMuerte()
    {
        estadoActual = EstadoBoss.Muerto;
        ataqueEnCurso = false;
        StopAllCoroutines();

        if (zonaThompson != null)
            zonaThompson.DetenerAtaque();

        rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccionPared);
    }
}
