using UnityEngine;
using System.Collections;

public class BarracudaAtaque : MonoBehaviour, IParryable
{
    private enum EstadoEnemigo { Cazando, Cargando, Embestiendo, Aturdido, Cooldown }
    private EstadoEnemigo estadoActual = EstadoEnemigo.Cazando;

    [Header("Configuración de Caza y Movimiento")]
    [SerializeField] private float velocidadCazaMax = 3.5f;
    [SerializeField] private float velocidadEmbestida = 15f;
    [SerializeField] private float rangoPersecucionJugador = 16f; 
    [SerializeField] private float rangoAtaqueJugador = 10f; 

    [Header("Detección de Paredes (Estilo Carpa)")]
    [SerializeField] private float distanciaDeteccionPared = 1.5f;
    [SerializeField] private LayerMask capaParedes;

    [Header("Configuración de Ataque")]
    [Range(0f, 100f)] [SerializeField] private float probabilidadEmbestida = 15f; 
    [SerializeField] private float frecuenciaChequeoAtaque = 0.5f; 
    [SerializeField] private float tiempoDeCarga = 1.2f; 
    [SerializeField] private float duracionMaximaEmbestida = 0.7f; 
    [SerializeField] private float cooldownAtaque = 3f;
    [SerializeField] private float cooldownTrasAcertarHit = 0.8f;

    [Header("Daño y Colisión")]
    [SerializeField] private int danoAlJugador = 1;
    [SerializeField] private float radioColisionAtaque = 0.5f; 
    [SerializeField] private float desfaseHocicoDelantero = 0.8f; 

    [Header("Tiempos de Aturdimiento")]
    [SerializeField] private float stunNormal = 1.5f; 
    [SerializeField] private float stunPorPared = 3.5f; 

    [Header("Visuales")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteEmbestida;
    [SerializeField] private Sprite spriteAturdido;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform objetivoJugador;
    private Vector2 direccionDeseada;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true; 
        CambiarSprite(spriteNormal);
    }

    private void Start()
    {
        SaludTiburon jugador = FindAnyObjectByType<SaludTiburon>();
        if (jugador != null) objetivoJugador = jugador.transform;
        StartCoroutine(RutinaPensamientoAtaque());
    }

    private void FixedUpdate()
    {
        if (objetivoJugador == null) return;

        Vector2 velocidadDeseadaFrame = Vector2.zero;
        float distanciaAlJugador = Vector2.Distance(transform.position, objetivoJugador.position);

        // 1. Detección de paredes por CircleCast en el FixedUpdate (como la carpa)
        if (estadoActual == EstadoEnemigo.Cazando || estadoActual == EstadoEnemigo.Embestiendo)
        {
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.4f, direccionDeseada, distanciaDeteccionPared, capaParedes);
            if (hit.collider != null)
            {
                if (estadoActual == EstadoEnemigo.Embestiendo)
                {
                    ForzarEstadoStun(true, false);
                    return;
                }
                // Si está cazando, simplemente rebota o cambia de rumbo temporalmente
                direccionDeseada = Vector2.Reflect(direccionDeseada, hit.normal).normalized;
            }
        }

        // 2. Máquina de estados limpia
        switch (estadoActual)
        {
            case EstadoEnemigo.Cazando:
                if (distanciaAlJugador <= rangoPersecucionJugador)
                {
                    direccionDeseada = (objetivoJugador.position - transform.position).normalized;
                    velocidadDeseadaFrame = direccionDeseada * velocidadCazaMax;
                }
                break;

            case EstadoEnemigo.Cargando:
                velocidadDeseadaFrame = Vector2.zero;
                break;

            case EstadoEnemigo.Embestiendo:
                velocidadDeseadaFrame = direccionDeseada * velocidadEmbestida;
                ChequearImpactoJugador();
                break;

            case EstadoEnemigo.Aturdido:
            case EstadoEnemigo.Cooldown:
                velocidadDeseadaFrame = Vector2.zero;
                break;
        }

        // Aplicar movimiento
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseadaFrame, Time.fixedDeltaTime * 5f);

        // Rotar usando la lógica limpia de tu carpa
        if (estadoActual == EstadoEnemigo.Cazando || estadoActual == EstadoEnemigo.Embestiendo || estadoActual == EstadoEnemigo.Cargando)
        {
            GirarSpriteHacia(direccionDeseada);
        }
    }

    private void GirarSpriteHacia(Vector2 dir)
    {
        if (dir.magnitude < 0.1f) return;

        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        // Voltear horizontal (Estilo Carpa)
        Vector3 escala = transform.localScale;
        escala.x = (dir.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        // Corrección de ángulo si va a la izquierda
        if (dir.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    private IEnumerator RutinaPensamientoAtaque()
    {
        while (estadoActual == EstadoEnemigo.Cazando)
        {
            yield return new WaitForSeconds(frecuenciaChequeoAtaque);

            if (objetivoJugador != null && Vector2.Distance(transform.position, objetivoJugador.position) <= rangoAtaqueJugador)
            {
                if (Random.Range(0f, 100f) <= probabilidadEmbestida)
                {
                    StartCoroutine(SecuenciaEmbestidaCompleta());
                    yield break; // Salimos de la rutina actual para que no se solape
                }
            }
        }
    }

    private IEnumerator SecuenciaEmbestidaCompleta()
    {
        // CARGA
        estadoActual = EstadoEnemigo.Cargando;
        direccionDeseada = (objetivoJugador.position - transform.position).normalized;
        yield return new WaitForSeconds(tiempoDeCarga);

        // EMBESTIDA
        estadoActual = EstadoEnemigo.Embestiendo;
        CambiarSprite(spriteEmbestida);
        yield return new WaitForSeconds(duracionMaximaEmbestida);

        // Si terminó el tiempo y no chocó con nada, entra en stun normal
        if (estadoActual == EstadoEnemigo.Embestiendo)
        {
            ForzarEstadoStun(false, false);
        }
    }

    private void ChequearImpactoJugador()
    {
        Vector2 posicionHocico = (Vector2)transform.position + ((Vector2)transform.right * desfaseHocicoDelantero);
        Collider2D col = Physics2D.OverlapCircle(posicionHocico, radioColisionAtaque);

        if (col != null && col.gameObject != gameObject)
        {
            SaludTiburon vidaTiburon = col.GetComponent<SaludTiburon>() ?? col.GetComponentInParent<SaludTiburon>();
            if (vidaTiburon != null)
            {
                Parry parry = col.GetComponent<Parry>() ?? col.GetComponentInParent<Parry>() ?? col.GetComponentInChildren<Parry>();

                if (parry != null && parry.HacerParry(gameObject, danoAlJugador))
                {
                    return; // El sistema de parry externo se encarga de llamar a OnParry()
                }

                vidaTiburon.RecibirDano(danoAlJugador);
                ForzarEstadoStun(false, true);
            }
        }
    }

    public void OnParry(GameObject parriedBy, int damage)
    {
        VidaEnemigo vidaBarracuda = GetComponent<VidaEnemigo>();
        if (vidaBarracuda != null) vidaBarracuda.RecibirDano(damage + 3);

        ForzarEstadoStun(false, false);
    }

    private void ForzarEstadoStun(bool porPared, bool golpeoAlJugador)
    {
        StopAllCoroutines(); // Detiene la secuencia de embestida de forma segura
        StartCoroutine(RutinaStunYCooldown(porPared, golpeoAlJugador));
    }

    private IEnumerator RutinaStunYCooldown(bool porPared, bool golpeoAlJugador)
    {
        estadoActual = EstadoEnemigo.Aturdido;
        rb.linearVelocity = Vector2.zero;
        CambiarSprite(spriteAturdido);

        yield return new WaitForSeconds(porPared ? stunPorPared : stunNormal);

        estadoActual = EstadoEnemigo.Cooldown;
        CambiarSprite(spriteNormal);

        yield return new WaitForSeconds(golpeoAlJugador ? cooldownTrasAcertarHit : cooldownAtaque);

        estadoActual = EstadoEnemigo.Cazando;
        StartCoroutine(RutinaPensamientoAtaque());
    }

    private void CambiarSprite(Sprite nuevoSprite)
    {
        if (nuevoSprite != null && spriteRenderer != null) spriteRenderer.sprite = nuevoSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoPersecucionJugador);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaqueJugador);

        Vector2 posicionHocicoGizmo = (Vector2)transform.position + ((Vector2)transform.right * desfaseHocicoDelantero);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(posicionHocicoGizmo, radioColisionAtaque);
    }
}