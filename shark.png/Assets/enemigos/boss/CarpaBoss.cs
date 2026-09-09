using UnityEngine;
using System.Collections;

public class CarpaBoss : MonoBehaviour, IEnemigo, IBoss
{
    private enum EstadoBoss
    {
        Idle,
        AtaqueThompson,
        AtaqueTe,
        Aturdido,
        Muerto
    }

    [Header("Estado (Debug)")]
    [SerializeField] private EstadoBoss estadoActual = EstadoBoss.Idle;

    [Header("Movimiento")]
    [SerializeField] private float velocidadMax = 3.5f;
    [SerializeField] private float velocidadHuidaThompson = 2.8f;
    [SerializeField] private float distanciaDeteccionPared = 1.5f;
    [SerializeField] private LayerMask capaParedes;

    [Header("Ataque Thompson")]
    [SerializeField] private ZonaThompson zonaThompson;
    [SerializeField] private float cooldownThompson = 10f;
    [Tooltip("Tiempo que tarda la animación de preparación antes de disparar")]
    [SerializeField] private float tiempoPreparacion = 0.5f;

    [Header("Ataque Té (Curación)")]
    [Tooltip("Sprite que se muestra mientras el boss bebe el té")]
    [SerializeField] private Sprite spriteTe;
    [Tooltip("Cantidad de vida que recupera al beber el té")]
    [SerializeField] private int curacionTe = 10;
    [Tooltip("Duración de la animación de beber el té")]
    [SerializeField] private float duracionTe = 3f;
    [Tooltip("Porcentaje de vida (0-1) por debajo del cual se activa el ataque de té")]
    [SerializeField] private float umbralVidaTe = 0.30f;

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

    // ---- IBoss ----
    public event System.Action OnBossMuerto;

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
    private float tiempoCooldownRebote = 0f;

    // ---- Ataque Thompson ----
    private bool enAtaqueThompson = false;
    private bool disparandoThompson = false;

    // ---- Ataque Té ----
    private bool teUsado = false;
    private bool aturdimientoPendienteThompson = false; // aturdimiento acumulado durante el Thompson

    private bool combateIniciado = false;  // bloqueado hasta que SalaBoss llame a IniciarCombate

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

        if (zonaThompson == null)
            zonaThompson = GetComponentInChildren<ZonaThompson>(true);

        // Comprobación de seguridad: Si el usuario arrastró un prefab en vez del objeto de la escena
        if (zonaThompson != null && !zonaThompson.gameObject.scene.IsValid())
        {
            zonaThompson = Instantiate(zonaThompson, transform);
            zonaThompson.transform.localPosition = Vector3.zero;
        }

        if (zonaThompson != null)
        {
            zonaThompson.Configurar(transform);
        }

        tiempoProximoAtaque = Time.time + tiempoEntreAtaquesMin;
    }

    // ===================== IBOSS =====================

    /// <summary>Activa el boss e inicia la IA. Llamado por SalaBoss.</summary>
    public void IniciarCombate()
    {
        combateIniciado = true;
        tiempoProximoAtaque = Time.time + tiempoEntreAtaquesMin;
    }

    private void OnDestroy()
    {
        if (vidaEnemigo != null)
            vidaEnemigo.OnMuerto -= OnMuerte;

        if (zonaThompson != null && zonaThompson.gameObject != null)
            Destroy(zonaThompson.gameObject);
    }

    private void Update()
    {
        if (!combateIniciado) return;

        if (jugador == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) jugador = p.transform;
            if (jugador == null) return;
        }

        if (estadoActual == EstadoBoss.Muerto) return;
        if (estadoActual == EstadoBoss.Aturdido) return;
        if (estadoActual == EstadoBoss.AtaqueTe) return;
        if (estadoActual == EstadoBoss.AtaqueThompson) return;

        if (estadoActual == EstadoBoss.Idle && !ataqueEnCurso && Time.time >= tiempoProximoAtaque)
        {
            // Comprobar si puede usar el té (una sola vez, bajo el umbral de vida)
            if (!teUsado && vidaEnemigo != null &&
                (float)vidaEnemigo.VidaActual / vidaEnemigo.VidaMaxima <= umbralVidaTe)
            {
                IniciarAtaqueTe();
            }
            else if (Time.time - ultimoAtaqueThompson > cooldownThompson)
            {
                IniciarAtaqueThompson();
            }
            else
            {
                tiempoProximoAtaque = Time.time + 1f;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!combateIniciado)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (estadoActual == EstadoBoss.Muerto) return;

        switch (estadoActual)
        {
            case EstadoBoss.Idle:
                Patrulla();
                break;

            case EstadoBoss.Aturdido:
            case EstadoBoss.AtaqueTe:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f);
                break;

            case EstadoBoss.AtaqueThompson:
                if (!enAtaqueThompson)
                {
                    // Preparación previa (0.5s): retrocede alejándose del jugador mientras lo mira
                    MoverseAlejandoseDelJugador();
                }
                else
                {
                    // Ataque en curso (warning y disparo): COMPLETAMENTE ESTÁTICO
                    // No se mueve aunque el jugador lo golpee o empuje
                    rb.linearVelocity = Vector2.zero;
                }
                break;
        }
    }

    // ===================== THOMPSON =====================

    private void IniciarAtaqueThompson()
    {
        estadoActual = EstadoBoss.AtaqueThompson;
        ataqueEnCurso = true;
        enAtaqueThompson = false;
        disparandoThompson = false;
        ultimoAtaqueThompson = Time.time;
        aturdimientoPendienteThompson = false;
        StartCoroutine(SecuenciaThompson());
    }

    private IEnumerator SecuenciaThompson()
    {
        if (zonaThompson == null || jugador == null)
        {
            FinalizarAtaque();
            yield break;
        }

        // --- FASE DE PREPARACIÓN (0.5s) ---
        // Mirar hacia el jugador y empezar a retroceder alejándose
        Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        GirarSprite(dirAlJugador, suave: true);

        if (spriteRenderer != null && spritePreparacion != null)
            spriteRenderer.sprite = spritePreparacion;

        // Esperar el tiempo de preparación (0.5s)
        yield return new WaitForSeconds(tiempoPreparacion);

        // --- COMIENZA EL ATAQUE ---
        // 1. Obtener la dirección fija hacia el jugador en este instante preciso
        Vector2 dirAtaque = ((Vector2)jugador.position - (Vector2)transform.position).normalized;

        // 2. Bloquear la mirada en esa dirección: A PARTIR DE AQUÍ NO SIGUE AL JUGADOR CON LA MIRADA
        GirarSprite(dirAtaque, suave: true);

        // 3. Bloquear movimiento del boss: se queda completamente estático
        enAtaqueThompson = true;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // 4. Lanzar el ataque Thompson con la dirección fija
        zonaThompson.IniciarAtaque(
            dirAtaque,
            onDisparoIniciado: () =>
            {
                disparandoThompson = true;
                rb.linearVelocity = Vector2.zero;
            },
            onAtaqueTerminado: () =>
            {
                enAtaqueThompson = false;
                disparandoThompson = false;
                if (rb != null)
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // Restaurar sprite normal al terminar
                if (spriteRenderer != null && spriteNormal != null)
                    spriteRenderer.sprite = spriteNormal;

                FinalizarAtaque();

                // Si recibió hits durante el Thompson, aplicar aturdimiento ahora
                if (aturdimientoPendienteThompson && tiempoAturdimientoAcumulado > 0f)
                {
                    aturdimientoPendienteThompson = false;
                    if (crAturdimiento == null)
                        crAturdimiento = StartCoroutine(RutinaAturdimiento());
                }
            }
        );
    }

    /// <summary>
    /// Durante el ataque Thompson, el boss retrocede alejándose del jugador sin colisionar con paredes
    /// y deslizándose a lo largo de los muros si no puede retroceder más.
    /// </summary>
    private void MoverseAlejandoseDelJugador()
    {
        if (jugador == null) return;

        Vector2 posActual = transform.position;
        Vector2 posJugador = jugador.position;
        Vector2 dirAlJugador = (posJugador - posActual).normalized;
        Vector2 dirHuida = -dirAlJugador;

        // Mantener al boss mirando y apuntando suavemente hacia el jugador
        GirarSprite(dirAlJugador, suave: true);

        // Buscar una dirección que se aleje del jugador esquivando paredes
        Vector2 dirEscape = BuscarDireccionHuidaSinParedes(posActual, dirHuida);

        if (dirEscape != Vector2.zero)
        {
            Vector2 velDeseada = dirEscape * velocidadHuidaThompson;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velDeseada, Time.fixedDeltaTime * 4f);
        }
        else
        {
            // Desacelera suavemente sin bloquear fuerzas externas (empujones/golpes)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 2f);
        }
    }

    private Vector2 BuscarDireccionHuidaSinParedes(Vector2 origen, Vector2 dirDeseada)
    {
        // 1. Probar huida directa si no hay pared cerca
        RaycastHit2D hitDirecto = Physics2D.CircleCast(origen, 0.6f, dirDeseada, distanciaDeteccionPared, capaParedes);
        if (hitDirecto.collider == null)
        {
            return dirDeseada;
        }

        // 2. Si hay pared detrás, deslizarse por la tangente que aleje más del jugador
        Vector2 normalPared = hitDirecto.normal;
        Vector2 tan1 = new Vector2(-normalPared.y, normalPared.x);
        Vector2 tan2 = -tan1;

        Vector2 posJugador = (jugador != null) ? (Vector2)jugador.position : origen;
        float d1 = Vector2.Distance(origen + tan1, posJugador);
        float d2 = Vector2.Distance(origen + tan2, posJugador);

        Vector2 mejorTan = d1 >= d2 ? tan1 : tan2;
        Vector2 segundaTan = d1 >= d2 ? tan2 : tan1;

        RaycastHit2D hitTan1 = Physics2D.CircleCast(origen, 0.6f, mejorTan, 1.2f, capaParedes);
        if (hitTan1.collider == null)
        {
            return mejorTan;
        }

        RaycastHit2D hitTan2 = Physics2D.CircleCast(origen, 0.6f, segundaTan, 1.2f, capaParedes);
        if (hitTan2.collider == null)
        {
            return segundaTan;
        }

        // 3. Probar abanico de ángulos intermedios
        float[] angulos = { 30f, -30f, 60f, -60f, 90f, -90f };
        for (int i = 0; i < angulos.Length; i++)
        {
            float rad = angulos[i] * Mathf.Deg2Rad;
            Vector2 dirCand = new Vector2(
                dirDeseada.x * Mathf.Cos(rad) - dirDeseada.y * Mathf.Sin(rad),
                dirDeseada.x * Mathf.Sin(rad) + dirDeseada.y * Mathf.Cos(rad)
            ).normalized;

            RaycastHit2D hitCand = Physics2D.CircleCast(origen, 0.6f, dirCand, 1.0f, capaParedes);
            if (hitCand.collider == null)
            {
                return dirCand;
            }
        }

        return Vector2.zero;
    }

    // ===================== TÉ (CURACIÓN) =====================

    private void IniciarAtaqueTe()
    {
        teUsado = true;
        estadoActual = EstadoBoss.AtaqueTe;
        ataqueEnCurso = true;
        StartCoroutine(SecuenciaTe());
    }

    private IEnumerator SecuenciaTe()
    {
        // Parar en seco
        rb.linearVelocity = Vector2.zero;

        // Cambiar al sprite del té
        if (spriteRenderer != null && spriteTe != null)
            spriteRenderer.sprite = spriteTe;

        // Esperar la duración de la "animación" de beber
        yield return new WaitForSeconds(duracionTe);

        // Curar al boss
        if (vidaEnemigo != null)
            vidaEnemigo.Curar(curacionTe);

        // Restaurar sprite normal
        if (spriteRenderer != null && spriteNormal != null)
            spriteRenderer.sprite = spriteNormal;

        FinalizarAtaque();
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
                tiempoLimite = Random.Range(1.5f, 3f);
            }
            else
            {
                tiempoLimite = Random.Range(0.4f, 1f);
            }
        }

        if (estaMoviendose)
        {
            if (Time.time >= tiempoCooldownRebote)
            {
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.7f, direccion, distanciaDeteccionPared, capaParedes);
                if (hit.collider != null)
                {
                    direccion = Vector2.Reflect(direccion, hit.normal).normalized;
                    tiempoCooldownRebote = Time.time + 0.35f;
                }
            }
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

    private void GirarSprite(Vector2 dir, bool suave = false)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (!suave)
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
        enAtaqueThompson = false;
        disparandoThompson = false;
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        estadoActual = EstadoBoss.Idle;
        tiempoProximoAtaque = Time.time + Random.Range(tiempoEntreAtaquesMin, tiempoEntreAtaquesMax);
    }

    // ===================== ATURDIMIENTO =====================

    public void SumarAturdimiento(float tiempoExtra)
    {
        if (!gameObject.activeInHierarchy) return;
        if (estadoActual == EstadoBoss.Muerto) return;

        // Si el ataque Thompson está en curso NO lo interrumpimos:
        // acumulamos el aturdimiento y lo aplicamos cuando el Thompson acabe.
        if (estadoActual == EstadoBoss.AtaqueThompson)
        {
            tiempoAturdimientoAcumulado += tiempoExtra;
            aturdimientoPendienteThompson = true;
            return;
        }

        // Para cualquier otro estado (Idle, AtaqueTe, etc.) sí aplicamos el aturdimiento normal
        if (estadoActual == EstadoBoss.AtaqueTe)
        {
            // El té se interrumpe si recibe un golpe
            StopCoroutine(nameof(SecuenciaTe));
            if (spriteRenderer != null && spriteNormal != null)
                spriteRenderer.sprite = spriteNormal;
            ataqueEnCurso = false;
            // No restaurar teUsado: ya gastó el té aunque lo interrumpieran
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
        enAtaqueThompson = false;
        disparandoThompson = false;
        StopAllCoroutines();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = Vector2.zero;
        }

        if (zonaThompson != null)
            zonaThompson.DetenerAtaque();

        // Notificar a SalaBoss que el combate terminó
        OnBossMuerto?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccionPared);
    }
}
