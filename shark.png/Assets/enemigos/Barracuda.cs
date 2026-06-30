using UnityEngine;

public class Barracuda : MonoBehaviour, IParryable
{
    private enum EstadoEnemigo { Patrullando, Persiguiendo, Atacando }
    [SerializeField] private EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

    [Header("Base")]
    [SerializeField] private float velocidadMax = 3f;
    [SerializeField] private float distanciaDeteccionPared = 1.5f; 
    [SerializeField] private LayerMask capaParedes; 

    [Header("Distancias")]
    [SerializeField] private float distanciaAlJugador; 
    [SerializeField] private float rangoAlerta = 5f;
    [SerializeField] private float rangoAtaque = 1.5f;

    [Header("Configuración Barracuda (Persecución y Ataque)")]
    [SerializeField] private float velocidadPersecucion = 4.5f; // Algo más rápida que la patrulla
    [SerializeField] private float fuerzaPlacaje = 50f;         // La fuerza del impulso final
    [SerializeField] private float tiempoCargaAtaque = 1.5f;    // El segundo y medio que se queda quieta
    [SerializeField] private float tiempoAturdimientoPostAtaque = 1.0f; // El tiempo que se queda "tonta"

    private float temporizadorAturdimiento = 0f;
    private Vector2 direccion;
    private Transform jugador;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;

    // Variables internas para el control del ataque
    private float temporizadorAtaque = 0f;
    private bool yaHizoPlacaje = false;
    private Vector2 direccionPlacaje;

    private Rigidbody2D rb;

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
    }

    private void Update()
    {
        if (jugador == null) return;

        distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        // Si está atacando (ya sea cargando, haciendo el placaje o recuperándose en el cooldown),
        // no dejamos que las distancias del Update interrumpan el ataque hasta que pase el tiempo.
        if (estadoActual == EstadoEnemigo.Atacando) return;

        // Control de transiciones estándar cuando NO está atacando
        if (distanciaAlJugador <= rangoAtaque)
        {
            estadoActual = EstadoEnemigo.Atacando;
            temporizadorAtaque = 0f;
            yaHizoPlacaje = false;
        }
        else if (distanciaAlJugador <= rangoAlerta)
        {
            estadoActual = EstadoEnemigo.Persiguiendo;
        }
        else
        {
            estadoActual = EstadoEnemigo.Patrullando;
        }
    }

    private void FixedUpdate()
    {
        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                Patrulla();
                break;

            case EstadoEnemigo.Persiguiendo:
                Persecucion();
                break;

            case EstadoEnemigo.Atacando:
                Ataque();
                break;
        }
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
                direccion = Random.insideUnitCircle.normalized;
                tiempoLimite = Random.Range(1f, 5f); 
            }
            else
            {
                tiempoLimite = Random.Range(0.5f, 2f);
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

    private void Persecucion()
    {
        if (jugador == null) return;

        // 1. Calcular dirección hacia el jugador
        direccion = ((Vector2)jugador.position - (Vector2)transform.position).normalized;

        // 2. Moverse directamente hacia él usando Lerp para mantener la fluidez física
        Vector2 velocidadDeseada = direccion * velocidadPersecucion;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * 5f);

        // 3. Orientar el sprite hacia donde está persiguiendo
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            GirarSprite();
        }
    }

    private void Ataque()
    {
        if (jugador == null) return;

        temporizadorAtaque += Time.fixedDeltaTime;

        // FASE 1: Carga del ataque (Se queda quieta segundo y medio apuntando)
        if (temporizadorAtaque < tiempoCargaAtaque)
        {
            // Frenamos a la barracuda poco a poco para que se quede quieta acechando
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
            
            // Mientras carga, calcula y actualiza la dirección hacia donde está el jugador
            direccionPlacaje = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
            
            // Giramos el sprite para que mire al jugador MIENTRAS apunta
            Vector2 velocidadOriginal = rb.linearVelocity;
            rb.linearVelocity = direccionPlacaje; 
            GirarSprite();
            rb.linearVelocity = velocidadOriginal; 
        }
        // FASE 2: ¡Placaje gordo! (Se ejecuta en un solo frame al terminar la carga)
        else if (!yaHizoPlacaje)
        {
            yaHizoPlacaje = true;
            temporizadorAturdimiento = 0f; // Reseteamos el reloj del cooldown para la siguiente fase
            
            // Le metemos el impulso físico gordo
            rb.linearVelocity = direccionPlacaje * fuerzaPlacaje;
            
            // Aplicamos el giro una última vez justo antes de salir disparada para que se alinee con el placaje
            Vector2 velocidadOriginal = rb.linearVelocity;
            rb.linearVelocity = direccionPlacaje;
            GirarSprite();
            rb.linearVelocity = velocidadOriginal;
        }
        // FASE 3: Envestida en curso y Cooldown (Se queda "tonta")
        else
        {
            temporizadorAturdimiento += Time.fixedDeltaTime;

            // Mientras dure el cooldown, la barracuda va perdiendo velocidad por la fricción del agua
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 2f);

            // ¡AQUÍ ESTÁ EL TRUCO! 
            // Durante todo este tiempo (la envestida y el cooldown), NO llamamos a GirarSprite().
            // Al no llamarlo, el pez se deslizará manteniendo rígidamente la dirección que tomó en la Fase 2.

            // Si ya ha pasado el tiempo de aturdimiento total...
            if (temporizadorAturdimiento >= tiempoAturdimientoPostAtaque)
            {
                // Forzamos un pequeño reseteo para que la máquina de estados en el Update pueda sacarla de aquí
                // (Ya que el Update comprobará que el ataque terminó y la mandará a Perseguir o Patrullar)
                estadoActual = EstadoEnemigo.Patrullando; 
            }
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

    public void OnParry(GameObject parriedBy, int damage)
    {
        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, damage + 5);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoAlerta);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}