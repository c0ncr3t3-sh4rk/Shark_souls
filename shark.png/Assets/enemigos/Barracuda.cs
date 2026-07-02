using UnityEngine;
using System.Collections;

public class Barracuda : MonoBehaviour, IParryable, IEnemigo
{
    private enum EstadoEnemigo { Patrullando, Persiguiendo, Atacando, Aturdido }
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
    [SerializeField] private float fuerzaPlacaje = 50f;
    [SerializeField] private float tiempoCargaAtaque = 1.5f;
    [SerializeField] public float tAturdimiento = 1.0f;

    [Header("Referencias")]
    [SerializeField] private GameObject objAtaque;
    [SerializeField] private GameObject objParry;

    // --- NUEVO SISTEMA DE ACUMULACIÓN DE ATURDIMIENTO ---
    [Header("Debug Aturdimiento")]
    [SerializeField] private float tiempoAturdimientoAcumulado = 0f;

    private Vector2 direccion;
    private Transform jugador;
    private bool estaMoviendose = false;
    private float tiempoEnEstado = 0f;
    private float tiempoLimite = 2f;
    private float temporizadorAtaque = 0f;
    private bool yaHizoPlacaje = false;
    private Vector2 direccionAtaque;
    private Rigidbody2D rb;
    private Coroutine crAturdimiento;

    private void Awake()
    {
        objAtaque.SetActive(false);
        objParry.SetActive(false);
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
        if (estadoActual == EstadoEnemigo.Aturdido) return;
        if (estadoActual == EstadoEnemigo.Atacando) return;

        distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

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

            case EstadoEnemigo.Aturdido:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 6f);
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
            GirarSprite(rb.linearVelocity);
        }
    }

    private void Persecucion()
    {
        if (jugador == null) return;

        direccion = ((Vector2)jugador.position - (Vector2)transform.position).normalized;

        Vector2 velocidad = direccion * (velocidadMax * 1.5f);
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidad, Time.fixedDeltaTime * 5f);

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            GirarSprite(rb.linearVelocity);
        }
    }

    private void Ataque()
    {
        if (jugador == null) return;

        temporizadorAtaque += Time.fixedDeltaTime;

        if (temporizadorAtaque < tiempoCargaAtaque)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
            direccionAtaque = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
            GirarSprite(direccionAtaque);
            objAtaque.SetActive(false);
        }
        else if (!yaHizoPlacaje)
        {
            if (rb.linearVelocity.magnitude < 0.1f) 
            {
                rb.linearVelocity = direccionAtaque * fuerzaPlacaje;
                GirarSprite(direccionAtaque);
                objAtaque.SetActive(true);
            }
            
            if (rb.linearVelocity.magnitude < 20f) 
            {
                Debug.Log("fin Ataque");
                yaHizoPlacaje = true;
            }
        }
        else
        {
            Debug.Log("stun de 3 por ataque de la barracuda");
            SumarAturdimiento(tAturdimiento);
        }
    }

    public void SumarAturdimiento(float tiempoExtra)
    {
        rb.linearVelocity = Vector2.zero;
        tiempoAturdimientoAcumulado += tiempoExtra;
        objAtaque.SetActive(false);

        if (crAturdimiento == null)
        {
            crAturdimiento = StartCoroutine(Aturdimiento());
        }
    }

    private IEnumerator Aturdimiento()
    {
        estadoActual = EstadoEnemigo.Aturdido;
        if (objAtaque != null) objAtaque.SetActive(false);

        while (tiempoAturdimientoAcumulado > 0f)
        {
            tiempoAturdimientoAcumulado -= Time.deltaTime;
            yield return null;
        }

        tiempoAturdimientoAcumulado = 0f;

        estadoActual = EstadoEnemigo.Patrullando;
        crAturdimiento = null;
    }

    public void DetenerPorImpacto(float stun)
    {
        rb.linearVelocity = Vector2.zero;
        SumarAturdimiento(stun); 
    }

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

    public void OnParry(GameObject parriedBy, int damage)
    {
        ColisionAtaqueBarracuda scriptHijo = objAtaque.GetComponent<ColisionAtaqueBarracuda>();
        if (scriptHijo != null) scriptHijo.StopAllCoroutines(); 

        objAtaque.SetActive(false);
        objParry.SetActive(true);

        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(30f, damage + 5);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoAlerta);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}