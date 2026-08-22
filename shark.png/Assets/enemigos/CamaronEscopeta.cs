using UnityEngine;

public class CamaronEscopeta : MonoBehaviour, IParryable
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
    [SerializeField] private float tiempoEntreDisparos = 2f; 
    [SerializeField] private int nBalas = 5;
    [SerializeField] private float anguloDispersion = 30f;
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
        if (agarrable != null && agarrable.EstaAgarrado) return;
        if (jugador == null) return;

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

        if (cronometroDisparo <= tiempoEntreDisparos)
        {
            cronometroDisparo += Time.deltaTime;
        }

        if (cronometroDisparo >= tiempoEntreDisparos && (estadoActual == EstadoEnemigo.Atacando || estadoActual == EstadoEnemigo.Huyendo))
        {
            CamaronDisparar();
            cronometroDisparo = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (agarrable != null && agarrable.EstaAgarrado) return;

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

    void CamaronDisparar()
{
    if (jugador == null || prefabBala == null || puntoDisparo == null || nBalas <= 0) return;

    // 1. Calcular la dirección base hacia el jugador
    Vector2 dirBase = (jugador.position - puntoDisparo.position).normalized;
    
    // 2. Obtener el ángulo base en grados de esa dirección
    float anguloBase = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;

    // 3. Bucle para fabricar y desviar cada una de las balas
    for (int i = 0; i < nBalas; i++)
    {
        // Si es solo 1 bala, dispara al frente. Si son más, calcula la desviación en abanico
        float desvio = 0f;
        if (nBalas > 1)
        {
            // Distribuye los ángulos equitativamente entre el rango negativo y positivo de la dispersión
            desvio = (-anguloDispersion / 2f) + (anguloDispersion / (nBalas - 1)) * i;
        }

        // 4. Sumar el desvío al ángulo base y convertirlo de nuevo a un vector de dirección (Vector2)
        float anguloFinal = anguloBase + desvio;
        float radianes = anguloFinal * Mathf.Deg2Rad;
        Vector2 dirBalaFinal = new Vector2(Mathf.Cos(radianes), Mathf.Sin(radianes));

        // 5. Instanciar e inicializar la bala con su dirección rotada
        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.identity);
        Proyectil scriptProyectil = bala.GetComponent<Proyectil>();
        
        if (scriptProyectil != null)
        {
            scriptProyectil.Disparar(dirBalaFinal, velocidadProyectil, danoProyectil);
        }
    }
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
        float anguloZ = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (rb.linearVelocity.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (rb.linearVelocity.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    public void MirarJugador()
    {
        if (jugador == null) return;

        // Calcular el vector de dirección entre el camarón y el jugador
        Vector2 dirHaciaJugador = (jugador.position - transform.position).normalized;
        
        // Calcular el ángulo Z en grados en base a esa dirección
        float anguloZ = Mathf.Atan2(dirHaciaJugador.y, dirHaciaJugador.x) * Mathf.Rad2Deg;

        // Control del volteo (Flip) horizontal del sprite según la posición X del jugador
        Vector3 escala = transform.localScale;
        escala.x = (dirHaciaJugador.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        // Si mira hacia la izquierda, corregimos 180 grados de desfase para que el sprite no quede invertido de cabeza
        if (dirHaciaJugador.x < 0) 
        {
            anguloZ += 180f;
        }

        // Aplicamos la rotación exacta
        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
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
