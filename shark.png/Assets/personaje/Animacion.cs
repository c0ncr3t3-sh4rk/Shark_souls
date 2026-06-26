using UnityEngine;

public class AnimacionMeneitoPng : MonoBehaviour
{
    private MovimientoTiburon movTiburon;
    private Rigidbody2D rb;

    [Header("Meneíto Rígido (Al Nadar)")]
    [SerializeField] private float velocidadMeneito = 25f; // Rapidez del aleteo
    [SerializeField] private float anguloMeneito = 6f;      // Grados de inclinación

    [Header("Balanceo Boya (Al Estar Quieto)")]
    [SerializeField] private float velocidadIdle = 3f;
    [SerializeField] private float anguloIdle = 2.5f;

    // Variables de control sincronizadas
    private bool direccionBase = false; 
    private float rotacionYBase = 0f;
    private float anguloZBase = 0f;

    private void Awake()
    {
        movTiburon = GetComponent<MovimientoTiburon>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (movTiburon == null || rb == null) return;

        // 1. Leemos el input actual de tu script de movimiento
        Vector2 input = movTiburon.getImput();

        // 2. Sincronizamos la base con la NUEVA lógica del tiburón
        // Guardamos la memoria del lado al que mira
        if (input.x > 0f)
        {
            direccionBase = true; // Derecha
        }
        else if (input.x < 0f)
        {
            direccionBase = false; // Izquierda
        }

        // Asignamos el giro en Y
        rotacionYBase = direccionBase ? 180f : 0f;

        // Asignamos la inclinación en Z (vertical puro o diagonal)
        if (input.y > 0f)
        {
            anguloZBase = -45f; // Sube
        }
        else if (input.y < 0f)
        {
            anguloZBase = 45f;  // Baja
        }
        else
        {
            anguloZBase = 0f;   // Recto si no se pulsa arriba/abajo
        }

        // 3. Calculamos la oscilación estética en el eje Z
        float modificadorZ = 0f;

        if (rb.linearVelocity.magnitude < 0.2f)
        {
            // Efecto pegatina flotando de lado a lado muy suave
            modificadorZ = Mathf.Sin(Time.time * velocidadIdle) * anguloIdle;
        }
        else
        {
            // Efecto meneíto arcade rápido de pez rígido
            modificadorZ = Mathf.Sin(Time.time * velocidadMeneito) * anguloMeneito;
        }

        // 4. Aplicamos la rotación final combinada directamente al transform
        transform.eulerAngles = new Vector3(0f, rotacionYBase, anguloZBase + modificadorZ);
    }
}