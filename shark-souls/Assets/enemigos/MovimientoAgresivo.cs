using UnityEngine;

public class MovimientoAgresivo : MonoBehaviour
{
    [Header("Configuración de Caza")]
    [SerializeField] private float velocidad = 4.5f;
    [SerializeField] private float rangoDeteccion = 7f;

    private Rigidbody2D rb;
    private Transform objetivoTiburona;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Evita que la física vuelque al pez locamente
    }

    private void Start()
    {
        SaludTiburon jugador = FindFirstObjectByType<SaludTiburon>();
        if (jugador != null) objetivoTiburona = jugador.transform;
    }

    private void FixedUpdate()
    {
        if (objetivoTiburona == null) return;

        float distanciaAlJugador = Vector2.Distance(transform.position, objetivoTiburona.position);

        if (distanciaAlJugador <= rangoDeteccion)
        {
            Vector2 direccion = (objetivoTiburona.position - transform.position).normalized;
            rb.linearVelocity = direccion * velocidad;

            // Llamamos a nuestra función de 8 direcciones fijas
            GirarSpriteFijo(direccion);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void GirarSpriteFijo(Vector2 direccion)
    {
        // --- 1. HORIZONTAL PURA (Derecha / Izquierda) ---
        if (direccion.x > 0.1f && Mathf.Abs(direccion.y) <= 0.3f)
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        else if (direccion.x < -0.1f && Mathf.Abs(direccion.y) <= 0.3f)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }

        // --- 2. DIAGONALES HACIA ARRIBA ---
        else if (direccion.y > 0.3f && direccion.x > 0.1f)
        {
            // Diagonal Arriba-Derecha
            transform.eulerAngles = new Vector3(0f, 0f, 45f); 
        }
        else if (direccion.y > 0.3f && direccion.x < -0.1f)
        {
            // Diagonal Arriba-Izquierda (Corregido el revés con 180 en Y)
            transform.eulerAngles = new Vector3(0f, 180f, -45f); 
        }

        // --- 3. DIAGONALES HACIA ABAJO ---
        else if (direccion.y < -0.3f && direccion.x > 0.1f)
        {
            // Diagonal Abajo-Derecha
            transform.eulerAngles = new Vector3(0f, 0f, -45f); 
        }
        else if (direccion.y < -0.3f && direccion.x < -0.1f)
        {
            // Diagonal Abajo-Izquierda
            transform.eulerAngles = new Vector3(0f, 180f, 45f); 
        }

        // --- 4. VERTICAL PURA (Arriba / Abajo) ---
        else if (direccion.y > 0.3f && Mathf.Abs(direccion.x) <= 0.1f)
        {
            // Recto hacia arriba (mantiene el lado al que miraba)
            transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 90f);
        }
        else if (direccion.y < -0.3f && Mathf.Abs(direccion.x) <= 0.1f)
        {
            // Recto hacia abajo
            transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, -90f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}