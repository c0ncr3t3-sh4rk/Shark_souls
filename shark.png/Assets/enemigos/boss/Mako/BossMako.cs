using System.Collections;
using UnityEngine;

public enum EstadoMarrajo
{
    FueraDeCamara,
    Sierra,
    EmbestidaExplosivo,
    EmbestidaLarga,
    Frenazo,
    CorteV,
    Cuadricula
}

// Cada punto tiene una posición y un array de direcciones válidas para embestir desde ese punto
[System.Serializable]
public class PuntoEmbestida
{
    public Transform _punto;
    public Vector2[] _direcciones; 

    // Constructor para poder instanciarlo con "new"
    public PuntoEmbestida(Transform punto, Vector2[] direcciones)
    {
        _punto = punto;
        _direcciones = direcciones;
    }
}

public class BossMako : MonoBehaviour, IVidaBoss
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual;

    [Header("Embestida Larga")]
    public float velocidadEmbestidaLarga = 25f;
    public float danoEmbestidaLarga = 2f;
    private int nEmbestidas = 0;
    public int nEmbestidasMaximas = 3;

    [Header("Embestida Explosiva")]
    public float velocidadEmbestidaExplosiva = 20f;
    public float danoEmbestidaExplosiva = 3f;
    public float danoExplosiones = 5f;

    [Header("Sierra")]
    public GameObject hitboxSierra;
    public float danoSierra = 1f;
    public float tiempoSierra = 2f;
    public float velocidadRotacionSierra = 3240f;
    [SerializeField] private Transform spriteTransform;

    [Header("Corte en V")]
    public float danoCorteV = 2f;
    public float velocidadCorteV = 20f;
    public int nCortes = 5;

    [Header("Salida de Pantalla")]
    public float velocidadSalida = 30f;
    public float distanciaFueraCamara = 12f;

    public LineRenderer lineaTelegrafiado;

    [Header("General")]
    public Transform jugador;
    private Rigidbody2D rb;
    public EstadoMarrajo estadoActual;
    public int faseActual = 1;
    private PuntoEmbestida[] puntosDeEntrada;
    public GameObject colisionPared;
    public float tiempoFrenazo = 1f;
    private bool StunAcabado = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisionPared.SetActive(false);

        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        vidaActual = vidaMaxima;

        if (hitboxSierra != null) hitboxSierra.SetActive(false);

        CargarPuntosReferencia();
    }

    private void CargarPuntosReferencia()
    {
        GameObject[] totalPuntos = GameObject.FindGameObjectsWithTag("PuntosReferenciaMako");
        
        if (totalPuntos.Length == 0) return;

        puntosDeEntrada = new PuntoEmbestida[totalPuntos.Length];

        // Sacamos la media de todos los puntos para tener un centro de referencia
        Vector3 centroSala = Vector3.zero;
        foreach (GameObject obj in totalPuntos)
        {
            centroSala += obj.transform.position;
        }
        centroSala /= totalPuntos.Length;

        // Calculamos las direcciones válidas para cada punto según su posición relativa al centro
        for (int i = 0; i < totalPuntos.Length; i++)
        {
            Transform PosicionP = totalPuntos[i].transform;
            Vector2[] dirsPosibles = CalcularDireccionesSegunPosicion(PosicionP.position, centroSala);

            puntosDeEntrada[i] = new PuntoEmbestida(PosicionP, dirsPosibles);
        }
    }

    private Vector2[] CalcularDireccionesSegunPosicion(Vector3 pos, Vector3 centroSala)
    {
        Vector2 dif = pos - centroSala;

        // --- ESQUINAS ---
        if (dif.x < -16f && dif.y > 6f)
        {
            return new Vector2[] { new Vector2(1f, -1f).normalized }; // Abajo-Derecha
        }
        else if (dif.x > 16f && dif.y > 6f)
        {
            return new Vector2[] { new Vector2(-1f, -1f).normalized }; // Abajo-Izquierda
        }
        else if (dif.x < -16f && dif.y < -6f)
        {
            return new Vector2[] { new Vector2(1f, 1f).normalized }; // Arriba-Derecha
        }
        else if (dif.x > 16f && dif.y < -6f)
        {
            return new Vector2[] { new Vector2(-1f, 1f).normalized }; // Arriba-Izquierda
        }

        // --- LADOS LATERALES ---
        else if (dif.x < -16f)
        {
            return new Vector2[] { Vector2.right }; // Derecha
        }
        else if (dif.x > 16f)
        {
            return new Vector2[] { Vector2.left }; // Izquierda
        }

        // --- LADOS INTERMEDIOS ---
        else if (dif.x > 9f && dif.y > 0f) // (Derecha-Arriba)
        {
            return new Vector2[] { 
                Vector2.down, 
                new Vector2(-1f, -1f).normalized // Diagonal Abajo-Izquierda
            };
        }
        else if (dif.x > 9f && dif.y < 0f) // (Derecha-Abajo)
        {
            return new Vector2[] { 
                Vector2.up, 
                new Vector2(-1f, 1f).normalized // Diagonal Arriba-Izquierda
            };
        } 
        else if (dif.x < -9f && dif.y > 0f) // (Izquierda-Arriba)
        {
            return new Vector2[] {
                Vector2.down,
                new Vector2(1f, -1f).normalized // Diagonal Abajo-Derecha
            };
        } 
        else if (dif.x < -9f && dif.y < 0f) // (Izquierda-Abajo)
        {
            return new Vector2[] {
                Vector2.up,
                new Vector2(1f, 1f).normalized // Diagonal Arriba-Derecha
            };
        } 

        // --- TECHO Y SUELO CENTRO ---
        else if (dif.y > 0f) // Techo
        {
            return new Vector2[] { 
                Vector2.down,
                new Vector2(1f, -1f).normalized,
                new Vector2(-1f, -1f).normalized
            };
        } 
        else // Suelo
        {
            return new Vector2[] { 
                Vector2.up,
                new Vector2(1f, 1f).normalized,
                new Vector2(-1f, 1f).normalized
            };
        }
    }

    private void Start()
    {
        StartCoroutine(BucleIA());
    }

    private IEnumerator BucleIA()
    {
        while (vidaActual > 0)
        {
            // --- FASE 1 ---
            if (faseActual == 1)
            {
                float rand = Random.value;
                float distancia = Vector2.Distance(transform.position, jugador.position);

                if (distancia < 3f && rand < 0.7f)
                {
                    yield return StartCoroutine(AtaqueSierra());
                }
                else if (distancia < 4f && rand < 0.5f)
                {
                    yield return StartCoroutine(AtaqueEmbestidaexplosiva());
                }
                else if (rand < 1f)
                {
                    yield return StartCoroutine(AtaqueEmbestidaLarga());
                }
                else
                {
                    yield return StartCoroutine(AtaqueCorteV());
                }
            }
            else if (faseActual == 2)
            {
                yield return null;
            }
            else if (faseActual == 3)
            {
                yield return null;
            }
        }

        Morir();
    }

    // --- MÉTODO SALIR DE PANTALLA ---
    private IEnumerator SalirDePantalla()
    {
        estadoActual = EstadoMarrajo.FueraDeCamara;

        Vector2 direccionHuida = (transform.position - jugador.position).normalized;
        if (direccionHuida == Vector2.zero) direccionHuida = transform.up;

        Camera cam = Camera.main;
        float altoCamara = cam.orthographicSize;
        float anchoCamara = cam.orthographicSize * cam.aspect;
        Vector3 centroCamara = cam.transform.position;

        float margen = 3f; 

        while (Mathf.Abs(transform.position.x - centroCamara.x) < (anchoCamara + margen) &&
            Mathf.Abs(transform.position.y - centroCamara.y) < (altoCamara + margen))
        {
            rb.linearVelocity = direccionHuida * velocidadSalida;
            GirarSprite(direccionHuida);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero; 
        Debug.Log("[Boss] El Mako ha salido completamente de la pantalla.");
    }

    // --- MÉTODOS DE ATAQUE ---
    private IEnumerator AtaqueSierra()
    {
        estadoActual = EstadoMarrajo.Sierra;
        Debug.Log("[Boss] ¡Ejecutando Ataque Sierra!");
        yield return new WaitForSeconds(0.8f);
        
        if (hitboxSierra != null) hitboxSierra.SetActive(true);

        float T = 0f;

        while (T < tiempoSierra)
        {
            transform.Rotate(0f, 0f, velocidadRotacionSierra * Time.deltaTime);
            T += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Quaternion.identity;
        if (hitboxSierra != null) hitboxSierra.SetActive(false);
    }

    private IEnumerator AtaqueEmbestidaexplosiva()
    {
        estadoActual = EstadoMarrajo.EmbestidaExplosivo;
        Debug.Log("[Boss] ¡Ejecutando Embestida Explosiva!");
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(SalirDePantalla());
    }

    private IEnumerator AtaqueEmbestidaLarga()
    {
        if (faseActual == 1)
        {
            yield return StartCoroutine(SalirDePantalla());

            estadoActual = EstadoMarrajo.EmbestidaLarga;
            Debug.Log("[Boss] ¡Ejecutando Embestida Larga!");

            nEmbestidas = Random.Range(1, 4);

            for (int i = 0; i < nEmbestidas; i++)
            {
                // Elegimos un punto al azar
                PuntoEmbestida P = puntosDeEntrada[Random.Range(0, puntosDeEntrada.Length)];

                transform.position = P._punto.position; // Nos movemos al punto de referencia

                Vector2 dirEmbestida = P._direcciones[Random.Range(0, P._direcciones.Length)]; // Elegimos una dirección válida al azar desde ese punto

                GirarSprite(dirEmbestida);

                // Telegrafiado
                if (lineaTelegrafiado != null)
                {
                    lineaTelegrafiado.enabled = true;
                    lineaTelegrafiado.SetPosition(0, transform.position);
                    lineaTelegrafiado.SetPosition(1, (Vector2)transform.position + (dirEmbestida * 40f));
                }

                yield return new WaitForSeconds(0.5f);

                if (lineaTelegrafiado != null) lineaTelegrafiado.enabled = false;

                // Embestida
                float tiempoPasada = 0f;
                float duracionPasada = 1f;

                if (i < nEmbestidas - 1)
                {
                    while (tiempoPasada < duracionPasada)
                    {
                        rb.linearVelocity = dirEmbestida * velocidadEmbestidaLarga;
                        tiempoPasada += Time.deltaTime;
                        yield return null;
                    }

                    rb.linearVelocity = Vector2.zero;
                    yield return new WaitForSeconds(0.2f);
                } else
                {
                    rb.linearVelocity = dirEmbestida * velocidadEmbestidaLarga;
                    yield return new WaitForSeconds(0.3f);

                    colisionPared.SetActive(true); //activamos la colision

                    while (!StunAcabado)
                    {
                        yield return null;
                    }
                }
                
            }
        }
    }

    private IEnumerator AtaqueCorteV()
    {
        estadoActual = EstadoMarrajo.CorteV;
        Debug.Log("[Boss] ¡Ejecutando Arpón en V!");
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(SalirDePantalla());
    }

    private IEnumerator Frenazo()
    {
        estadoActual = EstadoMarrajo.Frenazo;
        colisionPared.SetActive(false);   // Apagamos la colisión

        Debug.Log("[Boss] ¡Incrustado en la pared! Stun iniciado.");

        // Esperamos el tiempo de Stun
        yield return new WaitForSeconds(tiempoFrenazo);

        Debug.Log("[Boss] Stun terminado. Volviendo a la acción.");
        StunAcabado = true;
        yield return new WaitForSeconds(0.5f); // Pequeño retraso para asegurar que el estado se actualice antes de continuar
        StunAcabado = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (estadoActual == EstadoMarrajo.EmbestidaLarga || estadoActual == EstadoMarrajo.EmbestidaExplosivo)
        {
            // Detecta la sala sólida
            if (!collision.isTrigger && collision.gameObject.layer == LayerMask.NameToLayer("Salas"))
            {
                // Iniciamos el proceso de Frenazo/Stun de forma segura mediante una Corrutina
                rb.linearVelocity = Vector2.zero; // Frenazo inmediato al chocar
                StartCoroutine(Frenazo());
            }
        }
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

        // 1. Calculamos el ángulo libre exacto sin restringir a intervalos de 45°
        float anguloZ = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        // 2. Invertimos el Flip en X para adaptarse al sprite volteado
        Vector3 escala = transform.localScale;
        escala.x = (direccion.x < 0) ? Mathf.Abs(escala.x) : -Mathf.Abs(escala.x);
        transform.localScale = escala;

        // Ajuste de ángulo para mantener la coherencia al voltear en X
        if (direccion.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    // --- DAÑO Y MUERTE ---
    public void RecibirDano(float dano)
    {
        vidaActual -= dano;

        if (vidaActual <= vidaMaxima * 0.33f) faseActual = 3;
        else if (vidaActual <= vidaMaxima * 0.66f) faseActual = 2;

        if (vidaActual <= 0)
        {
            StopAllCoroutines();
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        vidaActual += cantidad;
        if (vidaActual > vidaMaxima) vidaActual = vidaMaxima;
    }

    public void Morir()
    {
        Debug.Log("El Boss Marrajo ha sido derrotado.");
        Destroy(gameObject);
    }
}