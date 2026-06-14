using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DashTiburon : MonoBehaviour
{
    [Header("Configuración del Dash")]
    [SerializeField] private float fuerzaDash = 20f;
    [SerializeField] private float duracionDash = 0.2f;
    [SerializeField] private float tiempoEsperaDash = 1f;

    [Header("Efecto Imágenes Residuales")]
    [SerializeField] private Color colorFantasma = new Color(0f, 0.7f, 1f, 0.6f); // Azul transparente por defecto
    [SerializeField] private float tiempoVidaFantasma = 0.4f;
    [SerializeField] private float tiempoEntreFantasmas = 0.04f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private MovimientoTiburon scriptMovimiento;

    private bool puedeHacerDash = true;
    private bool estaHaciendoDash = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        scriptMovimiento = GetComponent<MovimientoTiburon>();
    }

    // El Input System llamará a esto al pulsar Shift (Suele ser la acción "Sprint")
    // Si en tu mapa de inputs se llama de otra forma, cambia el nombre a OnNombreAccion
    public void OnSprint(InputValue value)
    {
        // Solo hace el dash si se pulsa el botón, no estamos ya en un dash, tenemos el cooldown listo
        // Y además comprobamos que el jugador se esté moviendo (tenga un input de dirección)
        if (value.isPressed && puedeHacerDash && !estaHaciendoDash)
        {
            // Le pedimos al script de movimiento la dirección que está pulsando el jugador
            // Para poder usar la dirección, necesitamos hacer una pequeña modificación en MovimientoTiburon
            Vector2 direccionInput = scriptMovimiento.ObtenerDireccionInput();

            // Solo hacemos el dash si el jugador está pulsando alguna dirección (WASD)
            if (direccionInput.magnitude > 0.01f)
            {
                StartCoroutine(EjecutarDash(direccionInput.normalized));
            }
        }
    }

    private IEnumerator EjecutarDash(Vector2 direccion)
    {
        puedeHacerDash = false;
        estaHaciendoDash = false; // Guardamos estado por si acaso

        // Desactivamos temporalmente el FixedUpdate del movimiento para que no frene el dash
        if (scriptMovimiento != null) scriptMovimiento.enabled = false;

        // Metemos el subidón de velocidad en la dirección del input
        rb.linearVelocity = direccion * fuerzaDash;

        // Iniciamos el efecto de las imágenes residuales de fondo
        Coroutine rutinaFantasmas = StartCoroutine(RutinaImagenesResiduales());

        // Esperamos lo que dura el dash propiamente dicho
        yield return new WaitForSeconds(duracionDash);

        // Frenamos al tiburón y devolvemos el control al script de movimiento normal
        rb.linearVelocity = Vector2.zero;
        if (scriptMovimiento != null) scriptMovimiento.enabled = true;

        // Paramos de generar fantasmas
        StopCoroutine(rutinaFantasmas);

        // Esperamos el tiempo de recarga (cooldown) antes de poder hacer otro dash
        yield return new WaitForSeconds(tiempoEsperaDash);
        puedeHacerDash = true;
    }

    private IEnumerator RutinaImagenesResiduales()
    {
        while (true)
        {
            GameObject fantasma = new GameObject("EcoDash_Clon");
            EcoDash componenteEco = fantasma.AddComponent<EcoDash>();

            // Le pasamos todos los datos, incluyendo el spriteRenderer.sortingOrder del final
            componenteEco.Inicializar(
                spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                colorFantasma,
                tiempoVidaFantasma,
                spriteRenderer.sortingOrder // 👈 Le enviamos su capa actual
            );

            yield return new WaitForSeconds(tiempoEntreFantasmas);
        }
    }
}