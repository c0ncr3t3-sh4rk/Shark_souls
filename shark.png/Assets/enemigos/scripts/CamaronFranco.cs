using UnityEngine;

public class CamaronFranco : MonoBehaviour, IParryable
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

    [Header("Ajustes Sniper")]
    [SerializeField] private Transform puntoDisparo;      
    [SerializeField] private float danoProyectil = 1f;       
    [SerializeField] private float tiempoEntreAtaques = 2f; 
    [SerializeField] private LayerMask capaSalas; // Paredes que bloquean visión
    [SerializeField] private float tiempoCargaSniper = 4f; // Tiempo total para disparar
    [SerializeField] private float tiempoGraciaCobertura = 2f; // Tiempo que puede estar escondido
    [SerializeField] private LineRenderer laserRenderer; // ¡NUEVO! Arrastra tu LineRenderer aquí

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
    private Agarrable agarrable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        misColliders = GetComponentsInChildren<Collider2D>();
        escalaOriginal = transform.localScale;
        animEnemigo = GetComponent<AnimacionEnemigo>();
        agarrable = GetComponent<Agarrable>();

        // Nos aseguramos de que el láser empiece apagado
        if (laserRenderer != null) laserRenderer.enabled = false;
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

        if (cronometroDisparo >= tiempoEntreAtaques && estadoActual == EstadoEnemigo.Atacando)
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

    private void Ataque()
    {
        MirarJugador();
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 4f); // Frena suavemente
    }

    private void Huida()
    {
        Vector2 dirHuida = (transform.position - jugador.position).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.3f, dirHuida, distanciaDeteccionPared, capaParedes);
        if (hit.collider != null)
        {
            dirHuida = Vector2.Reflect(dirHuida, hit.normal).normalized; 
        }

        Vector2 velocidadDeseada = dirHuida * velocidadMax;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * 4f);

        GirarSprite(); 
    }

    System.Collections.IEnumerator CamaronDisparar()
    {
        estaDisparando = true;
        rb.linearVelocity = Vector2.zero; // Se clava en el sitio

        float timerCarga = 0f;
        float timerOculto = 0f;

        if (laserRenderer != null) laserRenderer.enabled = true;

        while (timerCarga < tiempoCargaSniper)
        {
            // Validamos que el jugador exista (por si muere a mitad de carga)
            if (jugador == null) break;

            Vector2 dirHaciaJugador = jugador.position - puntoDisparo.position;
            float distancia = dirHaciaJugador.magnitude;

            RaycastHit2D hitVision = Physics2D.Raycast(puntoDisparo.position, dirHaciaJugador.normalized, distancia, capaSalas);

            if (laserRenderer != null)
            {
                laserRenderer.SetPosition(0, puntoDisparo.position); // Inicio del láser
            }

            if (hitVision.collider == null)
            {
                // JUGADOR A LA VISTA
                timerOculto = 0f; 
                MirarJugador();   

                if (laserRenderer != null) laserRenderer.SetPosition(1, jugador.position); // Láser llega al jugador
            }
            else
            {
                // JUGADOR ESCONDIDO
                timerOculto += Time.deltaTime;

                if (laserRenderer != null) laserRenderer.SetPosition(1, hitVision.point); // Láser choca contra la pared

                if (timerOculto >= tiempoGraciaCobertura)
                {
                    // Abortamos tiro
                    if (laserRenderer != null) laserRenderer.enabled = false;
                    estaDisparando = false;
                    cronometroDisparo = 0f; 
                    yield break; 
                }
            }

            // EFECTO DE PARPADEO ESTILO ULTRAKILL (último 1.5 segundos)
            if (laserRenderer != null)
            {
                float tiempoRestante = tiempoCargaSniper - timerCarga;
                if (tiempoRestante <= 1.5f)
                {
                    // Hace que el láser parpadee rapidísimo usando Mathf.PingPong
                    laserRenderer.enabled = Mathf.PingPong(Time.time * 20f, 1f) > 0.5f;
                }
                else
                {
                    laserRenderer.enabled = true; // Se mantiene sólido si aún falta tiempo
                }
            }

            timerCarga += Time.deltaTime;
            yield return null; 
        }

        // --- ¡BAM! DISPARO EJECUTADO ---
        if (laserRenderer != null) laserRenderer.enabled = false; // Apagamos el láser
        
        Debug.Log("¡BAM! El francotirador ha disparado");
        
        // Aquí tu lógica de daño (Hitscan instantáneo)
        /*
        SaludJugador salud = jugador.GetComponent<SaludJugador>();
        if (salud != null) {
            salud.RecibirDano(danoProyectil);
        }
        */

        estaDisparando = false;
        cronometroDisparo = 0f;
    }

    private void GirarSprite()
    {
        if (rb != null) GirarSprite(rb.linearVelocity, true);
    }

    private void GirarSprite(Vector2 direccionParam, bool redondearA8Direcciones = false)
    {
        if (direccionParam.sqrMagnitude < 0.001f) return;

        float anguloZ = Mathf.Atan2(direccionParam.y, direccionParam.x) * Mathf.Rad2Deg;

        // Solo aplicamos la limitación de 8 direcciones si se nos pide
        if (redondearA8Direcciones)
        {
            anguloZ = Mathf.Round(anguloZ / 45f) * 45f;
        }

        Vector3 escala = transform.localScale;
        escala.x = (direccionParam.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (direccionParam.x < 0) anguloZ += 180f;

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

        if (laserRenderer != null) laserRenderer.enabled = false; // Apaga el láser si sufre parry
        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, damage + 5);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, rangoHuida);
    }
}