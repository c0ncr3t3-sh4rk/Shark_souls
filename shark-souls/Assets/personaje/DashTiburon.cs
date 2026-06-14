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
    [SerializeField] private Color colorFantasma = new Color(0f, 0.7f, 1f, 0.6f); 
    [SerializeField] private float tiempoVidaFantasma = 0.4f;
    [SerializeField] private float tiempoEntreFantasmas = 0.04f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private MovimientoTiburon scriptMovimiento;
    
    private SaludTiburon scriptSalud; 

    private bool puedeHacerDash = true;
    private bool estaHaciendoDash = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        scriptMovimiento = GetComponent<MovimientoTiburon>();
        
        // 🛡️ Buscamos el componente de salud en el tiburón
        scriptSalud = GetComponent<SaludTiburon>(); 
    }

    public void OnSprint(InputValue value)
    {
        if (value.isPressed && puedeHacerDash && !estaHaciendoDash)
        {
            Vector2 direccionInput = scriptMovimiento.ObtenerDireccionInput();

            if (direccionInput.magnitude > 0.01f)
            {
                StartCoroutine(EjecutarDash(direccionInput.normalized));
            }
        }
    }

    private IEnumerator EjecutarDash(Vector2 direccion)
    {
        puedeHacerDash = false;
        estaHaciendoDash = true;

        // 🛡️ ACTIVAR INVENCIBILIDAD AL EMPEZAR EL DASH
        if (scriptSalud != null) scriptSalud.SetInvencible(true);

        if (scriptMovimiento != null) scriptMovimiento.enabled = false;

        rb.linearVelocity = direccion * fuerzaDash;
        Coroutine rutinaFantasmas = StartCoroutine(RutinaImagenesResiduales());

        yield return new WaitForSeconds(duracionDash);

        rb.linearVelocity = Vector2.zero;
        if (scriptMovimiento != null) scriptMovimiento.enabled = true;
        StopCoroutine(rutinaFantasmas);

        // 🛡️ DESACTIVAR INVENCIBILIDAD EN CUANTO EL DASH TERMINA
        if (scriptSalud != null) scriptSalud.SetInvencible(false);

        estaHaciendoDash = false; // El dash terminó, ya no está haciendo dash

        yield return new WaitForSeconds(tiempoEsperaDash);
        puedeHacerDash = true;
    }

    private IEnumerator RutinaImagenesResiduales()
    {
        while (true)
        {
            GameObject fantasma = new GameObject("EcoDash_Clon");
            EcoDash componenteEco = fantasma.AddComponent<EcoDash>();

            componenteEco.Inicializar(
                spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                colorFantasma,
                tiempoVidaFantasma,
                spriteRenderer.sortingOrder
            );

            yield return new WaitForSeconds(tiempoEntreFantasmas);
        }
    }
}