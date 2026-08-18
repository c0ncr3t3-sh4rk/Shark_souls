using UnityEngine;

public class Carpa : MonoBehaviour, IParryable, IAgarrable
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

    private Vector2 direccion;
    private Transform jugador;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;

    private Rigidbody2D rb;
    private Collider2D[] misColliders;

    // Referencia para seguir la boca sin cambiar de padre
    private Transform puntoBocaActual = null;
    public bool EstaAgarrado => puntoBocaActual != null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        misColliders = GetComponentsInChildren<Collider2D>();
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
        if (puntoBocaActual != null) return;
        if (jugador == null) return;

        distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (distanciaAlJugador <= rangoAtaque)
        {
            estadoActual = EstadoEnemigo.Atacando;
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
        // Si está agarrada, no ejecutamos la lógica de movimiento/patrulla
        if (puntoBocaActual != null) return;

        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                Patrulla();
                break;

            case EstadoEnemigo.Persiguiendo:
                Patrulla();
                break;

            case EstadoEnemigo.Atacando:
                Patrulla();
                break;
        }
    }

    private void LateUpdate()
    {
        // Si está agarrada por el tiburón, sigue la posición y rotación de la boca en tiempo real
        if (puntoBocaActual != null)
        {
            transform.position = puntoBocaActual.position;
            transform.rotation = puntoBocaActual.rotation;
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
        
    }

    private void Ataque()
    {
        
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
        puntoBocaActual = null;
        rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = true;
        }

        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() 
                                   ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, damage + 5);
    }

    // ==========================================
    // SISTEMA DE AGARRE (MANTENIENDO PADRE ORIGINAL)
    // ==========================================

    public void EnAgarrar(Transform boca)
    {
        puntoBocaActual = boca;

        // Desactivamos físicas pero NO cambiamos de padre (SetParent)
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = false;
        }
    }

    public void EnSoltar()
    {
        puntoBocaActual = null;
        rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = true;
        }

        this.enabled = true;
        estadoActual = EstadoEnemigo.Patrullando;
        tiempoEnEstado = 0f;
        estaMoviendose = false;
    }

    private void OnDisable()
    {
        puntoBocaActual = null;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (misColliders != null)
        {
            foreach (var col in misColliders)
            {
                if (col != null) col.enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoAlerta);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}