using UnityEngine;

public class MovimientoAgresivo : MonoBehaviour
{
    [Header("Configuración de Caza")]
    [SerializeField] private float velocidad = 4.5f;
    [SerializeField] private float rangoDeteccion = 7f;

    private Rigidbody2D rb;
    private Transform objetivoTiburona;

    private Vector3 escalaOriginal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        escalaOriginal = transform.localScale;
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

            // CORREGIDO: Multiplicamos por la escala original para mantener su tamaño reducido
            if (direccion.x > 0.1f) 
                transform.localScale = new Vector3(escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
            else if (direccion.x < -0.1f) 
                transform.localScale = new Vector3(-escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}