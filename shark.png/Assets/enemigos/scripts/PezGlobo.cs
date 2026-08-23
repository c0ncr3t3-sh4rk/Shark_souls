/*using UnityEngine;
using System.Collections;

public class PezGlobo : MonoBehaviour, IParryable
{
    [Header("Configuración")]
    [SerializeField] private float velocidadMax = 3f;
    [SerializeField] private float distanciaDeteccion = 1.5f; 
    [SerializeField] private LayerMask capaParedes; 
    
    [Header("Configuración del Ataque")]
    [SerializeField] private GameObject prefabProyectil; // El prefab de la espina/bala
    [SerializeField] private int cantidadProyectiles = 8; // Cuántas puntas tendrá la estrella (ej. 8, 12, 16)
    [SerializeField] private float velocidadProyectil = 5f;
    [SerializeField] private float tiempoEntreAtaques = 4f; // Cada cuánto tiempo dispara

    private Rigidbody2D rb;
    private Vector2 direccion;
    private bool estaMoviendose = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        StartCoroutine(Movimiento());
        StartCoroutine(Ataque());
    }

    private void FixedUpdate()
    {
        // deteccion de choque
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.3f, direccion, distanciaDeteccion, capaParedes);
        if (hit.collider != null)
        {
            direccion = Random.insideUnitCircle.normalized;
        }

        Vector2 velocidadDeseada = estaMoviendose ? (direccion * velocidadMax) : Vector2.zero;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseada, Time.fixedDeltaTime * 4f);

        if (estaMoviendose && rb.linearVelocity.magnitude > 0.1f)
        {
            GirarSprite();
        }
    }

    private IEnumerator Movimiento()
    {
        while (true)
        {
            // Elegir dirección aleatoria
            direccion = Random.insideUnitCircle.normalized;

            // Nadar un tiempo al azar
            estaMoviendose = true;
            yield return new WaitForSeconds(Random.Range(1f, 5f));

            // Descansar un tiempo al azar
            estaMoviendose = false;
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
        }
    }

    private IEnumerator Ataque()
    {
        // Esperar un momento antes del primer disparo al aparecer el pez
        yield return new WaitForSeconds(tiempoEntreAtaques);

        while (true)
        {
            float anguloPaso = 360f / cantidadProyectiles;

            for (int i = 0; i < cantidadProyectiles; i++)
            {
                // Calcular el ángulo actual de esta "punta" de la estrella
                float anguloActual = i * anguloPaso;

                // Convertir ese ángulo en una dirección matemática Vector2 (X, Y)
                float radianes = anguloActual * Mathf.Deg2Rad;
                Vector2 direccionProyectil = new Vector2(Mathf.Cos(radianes), Mathf.Sin(radianes));

                // 2. Instanciar el proyectil en la posición del pez
                // Usamos Quaternion.Euler para rotar el sprite del proyectil hacia donde va a viajar
                Quaternion rotacionProyectil = Quaternion.Euler(0f, 0f, anguloActual);
                GameObject nuevoProyectil = Instantiate(prefabProyectil, transform.position, rotacionProyectil);

                // 3. Pasar la dirección y velocidad al script del proyectil
                if (nuevoProyectil.TryGetComponent(out Proyectil proyectilScript))
                {
                    proyectilScript.Inicializar(direccionProyectil, velocidadProyectil);
                }
            }

            // Esperar el tiempo de cooldown antes de la siguiente ráfaga
            yield return new WaitForSeconds(tiempoEntreAtaques);
        }
    }

    private void GirarSprite()
    {
        float anguloZ = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        // Voltear el sprite en X (escala negativa) para que no nade boca abajo al ir a la izquierda
        Vector3 escala = transform.localScale;
        escala.x = (rb.linearVelocity.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        // Si mira a la izquierda, invertimos el ángulo Z para que la inclinación vertical sea natural
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
}
*/