using UnityEngine;

public class CamaronUzi : MonoBehaviour, IParryable
{
    private enum EstadoEnemigo { Patrullando, Atacando, Huyendo }
    [SerializeField] private EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

    [Header("Base")]
    [SerializeField] private float velocidadMax = 3f;
    [SerializeField] private float distanciaDeteccionPared = 1.5f; 
    [SerializeField] private LayerMask capaParedes; 

    [Header("Distancias")]
    [SerializeField] private float distanciaAlJugador; 
    [SerializeField] private float rangoAtaque = 5f;
    [SerializeField] private float rangoHuida = 1.5f;

    [Header("Ajustes de Ataque / Disparo")]
    [SerializeField] private GameObject prefabBala;       
    [SerializeField] private Transform puntoDisparo;      
    [SerializeField] private float velocidadProyectil = 10f; 
    [SerializeField] private float danoProyectil = 1f;       
    [SerializeField] private float tiempoEntreAtaques = 2f; 
    [SerializeField] private int nBalas = 15;
    [SerializeField] private float tiempoEntreDisparos = 0.3f;
    [SerializeField] private float gradosAtaque = 15f;
    private bool estaDisparando = false; 

    private float cronometroDisparo = 0f;
    private Vector2 direccion;
    private Transform jugador;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;

    private Rigidbody2D rb;
    private Collider2D[] misColliders;
    private Vector3 escalaOriginal;
    private AnimacionEnemigo animEnemigo;

    // Referencia al componente externo
    private Agarrable agarrable;

    [Header("Ajustes de Huida / Dash")]
    [SerializeField] private float fuerzaDash = 8f;        
    [SerializeField] private float cdDash = 0.5f;          
    [SerializeField] private float amortiguacionHuida = 5f; 
    private float tiempoEntreDash = 0f;

    [Header("Ajustes Nuevos: Dash Perpendicular Ataque")]
    private float tiempoEntreDashAtaque = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        misColliders = GetComponentsInChildren<Collider2D>();
        escalaOriginal = transform.localScale;
        animEnemigo = GetComponent<AnimacionEnemigo>();

        agarrable = GetComponent<Agarrable>();
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
        if ((agarrable != null && agarrable.EstaAgarrado) || jugador == null || estaDisparando) return;

        distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        if (distanciaAlJugador <= rangoHuida)
        {
            estadoActual = EstadoEnemigo.Huyendo;
        }
        else if (distanciaAlJugador <= rangoAtaque)
        {
            estadoActual = EstadoEnemigo.Atacando;
        }
        else
        {
            estadoActual = EstadoEnemigo.Patrullando;
        }

        if (cronometroDisparo <= tiempoEntreAtaques)
        {
            cronometroDisparo += Time.deltaTime;
        }

        if (cronometroDisparo >= tiempoEntreAtaques && (estadoActual == EstadoEnemigo.Atacando || estadoActual == EstadoEnemigo.Huyendo))
        {
            StartCoroutine(CamaronDisparar());
            cronometroDisparo = 0f;
        }
    }

    private void FixedUpdate()
    {
        if ((agarrable != null && agarrable.EstaAgarrado) || estaDisparando) return;

        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                Patrulla();
                break;

            case EstadoEnemigo.Atacando:
                Ataque();
                break;

            case EstadoEnemigo.Huyendo:
                Huida();
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

            if (estaMoviendose) // Corregido: Tenías un error de escritura aquí (estaMoviemdose)
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

    private void Ataque()
    {
        if (Time.time >= tiempoEntreDashAtaque)
        {
            EjecutarDashPerpendicular();
            // El doble de cooldown que el de huida (cdDash * 2)
            tiempoEntreDashAtaque = Time.time + (cdDash * 2f); 
        }

        // Fricción para que desacelere tras el dash táctico
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * amortiguacionHuida);

        MirarJugador();
    }

    private void EjecutarDashPerpendicular()
    {
        if (jugador == null) return;

        // 1. Obtener la dirección hacia el jugador
        Vector2 dirHaciaJugador = (jugador.position - transform.position).normalized;

        // 2. Calcular los vectores perpendiculares puros (90 grados) Izquierda o Derecha
        float signoPerpendicular = Random.value > 0.5f ? 1f : -1f;
        Vector2 dirPerpendicularPura = new Vector2(-dirHaciaJugador.y, dirHaciaJugador.x) * signoPerpendicular;

        // 3. Resetear físicas e impulsar lateralmente
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dirPerpendicularPura * fuerzaDash, ForceMode2D.Impulse);
    }

    System.Collections.IEnumerator CamaronDisparar()
    {
        estaDisparando = true;

        Vector2 dirHaciaJugador = (jugador.position - puntoDisparo.position).normalized;

        for (int i = 0; i < nBalas; i++)
        {
            float anguloBala = Random.Range(-gradosAtaque, gradosAtaque);

            Vector2 dirBalaFinal = Quaternion.Euler(0, 0, anguloBala) * dirHaciaJugador;

            GirarSprite(dirBalaFinal);

            GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.identity);
            Proyectil scriptProyectil = bala.GetComponent<Proyectil>();

            scriptProyectil.Disparar(dirBalaFinal, velocidadProyectil, danoProyectil);

            yield return new WaitForSeconds(tiempoEntreDisparos);
        }

        estaDisparando = false;
    }

    private void Huida()
    {
        if (Time.time >= tiempoEntreDash)
        {
            Vector2 dirHuida = (transform.position - jugador.position).normalized;
            Dash(dirHuida);
            tiempoEntreDash = Time.time + cdDash;
        }

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * amortiguacionHuida);

        MirarJugador();
    }

    private void Dash(Vector2 dirHuida)
    {
        float signoDiagonal = Random.value > 0.5f ? 1f : -1f;
        Vector2 dirPerpendicular = new Vector2(-dirHuida.y, dirHuida.x) * signoDiagonal;
        Vector2 dirDiagonalFinal = (dirHuida + dirPerpendicular).normalized;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dirDiagonalFinal * fuerzaDash, ForceMode2D.Impulse);
    }

    private void GirarSprite()
    {
        if (rb != null)
        {
            GirarSprite(rb.linearVelocity);
        }
    }

    private void GirarSprite(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.001f) return;

        float anguloZ = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (direccion.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (direccion.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    public void MirarJugador()
    {
        if (jugador == null) return;

        Vector2 dirHaciaJugador = (jugador.position - transform.position).normalized;
        
       GirarSprite(dirHaciaJugador);
    }

    public void OnParry(GameObject parriedBy, int damage)
    {
        transform.SetParent(null);
        transform.localScale = escalaOriginal;

        rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (var col in misColliders)
        {
            if (col != null) col.enabled = true;
        }

        if (animEnemigo != null) animEnemigo.enabled = true;

        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() 
                                   ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, damage + 5);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        Gizmos.color = Color.red;Gizmos.DrawWireSphere(transform.position, rangoHuida);
    }
}
