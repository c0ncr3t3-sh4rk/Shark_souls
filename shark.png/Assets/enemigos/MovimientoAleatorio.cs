using UnityEngine;
using System.Collections;

public class MovimientoAleatorio : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float velocidadMax = 3f;
    [SerializeField] private float distanciaDeteccion = 1.5f; 
    [SerializeField] private LayerMask capaParedes; 

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
        StartCoroutine(RutinaMovimiento());
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

    private IEnumerator RutinaMovimiento()
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
}
