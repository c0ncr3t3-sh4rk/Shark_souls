using UnityEngine;
using System.Collections;

public class MovimientoAleatorio : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float velocidadMax = 3f;
    [SerializeField] private float tiempoCaminandoMax = 3f;
    [SerializeField] private float tiempoCaminandoMin = 1f;
    [SerializeField] private float tiempoEsperaMax = 2f;
    [SerializeField] private float tiempoEsperaMin = 0.5f;

    [Header("Suavizado de Nado (Lerp)")]
    [Range(1f, 10f)] 
    [SerializeField] private float suavizadoGiroYAceleracion = 4f;

    [Header("Sensores de Paredes (Múltiples Rayos)")]
    [SerializeField] private float distanciaDeteccionPared = 1.5f; 
    [SerializeField] private LayerMask capaParedes; 
    [Range(10f, 60f)] 
    [SerializeField] private float anguloAperturaSensores = 35f; 

    private Rigidbody2D rb;
    private Vector2 direccionObjetivo;
    private Vector2 velocidadActual;
    private bool estaMoviendose = false;
    
    private Coroutine rutinaMovimientoActual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Fundamental: evita que las colisiones físicas externas alteren nuestra rotación por código
    }

    private void Start()
    {
        rutinaMovimientoActual = StartCoroutine(RutinaMovimiento());
    }

    private void FixedUpdate()
    {
        if (estaMoviendose)
        {
            // 👁️👁️👁️ TRIPLE SENSOR FRONTAL Y LATERAL
            EsquivarObstaculosConSensores();
        }

        // Aplicar velocidad fluida con inercia acuática (Lerp)
        Vector2 velocidadDeseada = estaMoviendose ? (direccionObjetivo * velocidadMax) : Vector2.zero;
        velocidadActual = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * suavizadoGiroYAceleracion);
        rb.linearVelocity = velocidadActual;

        // 🔥 NUEVO: Rotación orgánica basada en el movimiento real del pez
        if (estaMoviendose && rb.linearVelocity.magnitude > 0.1f)
        {
            RotarSpriteHaciaMovimiento(rb.linearVelocity);
        }
    }

    private void RotarSpriteHaciaMovimiento(Vector2 direccion)
    {
        // 1. Calculamos el ángulo real en grados hacia donde se mueve el pez
        float anguloZ = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        // 2. 🔥 EL TRUCO: Redondeamos el ángulo al múltiplo de 45 grados más cercano
        // Esto divide los 360 grados del espacio en 8 porciones perfectas
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        // 3. Control anti-bocarriba mejorado para evitar temblores en vertical puro
        if (Mathf.Abs(anguloZ) > 90f)
        {
            // El ángulo corresponde a mirar hacia la izquierda (135, 180, -135 grados)
            transform.localRotation = Quaternion.Euler(180f, 0f, -anguloZ);
        }
        else if (Mathf.Abs(anguloZ) < 90f)
        {
            // El ángulo corresponde a mirar hacia la derecha (-45, 0, 45 grados)
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ);
        }
        else
        {
            // Si el ángulo es exactamente 90 o -90 (Puro arriba o puro abajo), 
            // usamos la 'x' real del movimiento para decidir si el sprite mira a un lado o al otro.
            if (direccion.x < 0)
                transform.localRotation = Quaternion.Euler(180f, 0f, -anguloZ);
            else
                transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ);
        }
    }

    private void EsquivarObstaculosConSensores()
    {
        Vector2 dirFrontal = direccionObjetivo;
        Vector2 dirIzquierda = GirarVector(direccionObjetivo, anguloAperturaSensores);
        Vector2 dirDerecha = GirarVector(direccionObjetivo, -anguloAperturaSensores);

        RaycastHit2D hitFrontal = Physics2D.Raycast(transform.position, dirFrontal, distanciaDeteccionPared, capaParedes);
        RaycastHit2D hitIzquierda = Physics2D.Raycast(transform.position, dirIzquierda, distanciaDeteccionPared, capaParedes);
        RaycastHit2D hitDerecha = Physics2D.Raycast(transform.position, dirDerecha, distanciaDeteccionPared, capaParedes);

        RaycastHit2D impactoDetectado = new RaycastHit2D();

        if (hitFrontal.collider != null) impactoDetectado = hitFrontal;
        else if (hitIzquierda.collider != null) impactoDetectado = hitIzquierda;
        else if (hitDerecha.collider != null) impactoDetectado = hitDerecha;

        if (impactoDetectado.collider != null)
        {
            Vector2 direccionRebote = Vector2.Reflect(direccionObjetivo, impactoDetectado.normal).normalized;

            float variacion = Random.Range(-0.2f, 0.2f);
            direccionObjetivo = (direccionRebote + new Vector2(variacion, Random.Range(-0.2f, 0.2f))).normalized;

            if (rutinaMovimientoActual != null) StopCoroutine(rutinaMovimientoActual);
            rutinaMovimientoActual = StartCoroutine(RutinaMovimiento(forzarNadoInmediato: true));
        }
    }

    private Vector2 GirarVector(Vector2 vector, float grados)
    {
        float radianes = grados * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radianes);
        float cos = Mathf.Cos(radianes);
        
        float tx = vector.x;
        float ty = vector.y;
        
        return new Vector2((cos * tx) - (sin * ty), (sin * tx) + (cos * ty));
    }

    private IEnumerator RutinaMovimiento(bool forzarNadoInmediato = false)
    {
        while (true)
        {
            if (!forzarNadoInmediato)
            {
                float randomX = Random.Range(-1f, 1f);
                float randomY = Random.Range(-1f, 1f);
                if (Mathf.Abs(randomX) < 0.1f && Mathf.Abs(randomY) < 0.1f) randomX = 1f;

                direccionObjetivo = new Vector2(randomX, randomY).normalized;
            }

            forzarNadoInmediato = false;

            estaMoviendose = true;
            float tiempoCaminando = Random.Range(tiempoCaminandoMin, tiempoCaminandoMax);
            yield return new WaitForSeconds(tiempoCaminando);

            estaMoviendose = false;
            float tiempoEspera = Random.Range(tiempoEsperaMin, tiempoEsperaMax);
            yield return new WaitForSeconds(tiempoEspera);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 dirBase = direccionObjetivo == Vector2.zero ? Vector2.right : direccionObjetivo;

        Vector2 dirFrontal = dirBase;
        Vector2 dirIzquierda = GirarVector(dirBase, anguloAperturaSensores);
        Vector2 dirDerecha = GirarVector(dirBase, -anguloAperturaSensores);

        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirFrontal * distanciaDeteccionPared);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirIzquierda * distanciaDeteccionPared);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirDerecha * distanciaDeteccionPared);
    }
}