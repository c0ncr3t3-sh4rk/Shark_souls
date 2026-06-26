using UnityEngine;
using System.Collections;

public class BarracudaAtaque : MonoBehaviour, IParryable
{
    private enum EstadoEnemigo { Cazando, Cargando, Embestiendo, Aturdido, Cooldown }
    private EstadoEnemigo estadoActual = EstadoEnemigo.Cazando;

    [Header("Configuración de Persecución (Caza)")]
    [SerializeField] private float velocidadCazaMax = 3.5f;
    [SerializeField] private float rangoPersecucionJugador = 16f; 
    [SerializeField] private float rangoAtaqueJugador = 10f; 
    [Range(1f, 15f)]
    [SerializeField] private float velocidadGiroSuave = 7f; 

    [Header("Sensores de Paredes (Evasión Inteligente)")]
    [SerializeField] private float distanciaDeteccionPared = 1.8f;
    [SerializeField] private LayerMask capaParedes;
    [Range(10f, 60f)]
    [SerializeField] private float anguloAperturaSensores = 30f;

    [Header("Configuración de Probabilidad de Ataque")]
    [Range(0f, 100f)]
    [SerializeField] private float probabilidadEmbestida = 15f; 
    [SerializeField] private float frecuenciaChequeoAtaque = 0.5f; 

    [Header("Configuración de la Embestida y Daño")]
    [SerializeField] private float tiempoDeCarga = 1.2f; 
    [SerializeField] private float velocidadEmbestida = 15f; 
    [SerializeField] private float duracionMaximaEmbestida = 0.7f; 
    [SerializeField] private float cooldownAtaque = 3f;
    [SerializeField] private float cooldownTrasAcertarHit = 0.8f;
    [SerializeField] private int danoAlJugador = 1;
    [SerializeField] private float radioColisionAtaque = 0.5f; 
    [SerializeField] private float desfaseHocicoDelantero = 0.8f; 

    [Header("Configuración de Aturdimiento (Stun)")]
    [SerializeField] private float stunNormal = 1.5f; 
    [SerializeField] private float stunPorPared = 3.5f; 

    [Header("Sprites de Estado")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteEmbestida;
    [SerializeField] private Sprite spriteAturdido;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform objetivoJugador;
    private Vector2 velocidadActual;
    private Vector2 direccionDeseadaFinal;
    private Vector2 direccionFijadaEmbestida;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true; 

        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;
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

        switch (estadoActual)
        {
            case EstadoEnemigo.Cazando:
                if (distanciaAlJugador <= rangoPersecucionJugador)
                {
                    Vector2 direccionAlTiburon = (objetivoJugador.position - transform.position).normalized;
                    direccionDeseadaFinal = EvaluarRutaYEsquivar(direccionAlTiburon);
                    velocidadDeseadaFrame = direccionDeseadaFinal * velocidadCazaMax;

                    RotarHaciaDireccionSuave(direccionDeseadaFinal);
                }
                break;

            case EstadoEnemigo.Cargando:
                velocidadDeseadaFrame = Vector2.zero;
                RotarHaciaDireccionInstantanea(direccionFijadaEmbestida);
                break;

            case EstadoEnemigo.Embestiendo:
                velocidadDeseadaFrame = direccionFijadaEmbestida * velocidadEmbestida;
                RotarHaciaDireccionInstantanea(direccionFijadaEmbestida);
                
                ChequearImpactosEmbestida();
                break;

            case EstadoEnemigo.Aturdido:
            case EstadoEnemigo.Cooldown:
                velocidadDeseadaFrame = Vector2.zero;
                break;
        }

        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, velocidadDeseadaFrame, Time.fixedDeltaTime * 6f);
    }

    private IEnumerator RutinaPensamientoAtaque()
    {
        while (true)
        {
            yield return new WaitForSeconds(frecuenciaChequeoAtaque);

            if (estadoActual == EstadoEnemigo.Cazando && objetivoJugador != null)
            {
                float distancia = Vector2.Distance(transform.position, objetivoJugador.position);

                if (distancia <= rangoAtaqueJugador)
                {
                    if (Random.Range(0f, 100f) <= probabilidadEmbestida)
                    {
                        StartCoroutine(SecuenciaEmbestidaCompleta());
                    }
                }
            }
        }
    }

    private IEnumerator SecuenciaEmbestidaCompleta()
    {
        estadoActual = EstadoEnemigo.Cargando;
        direccionFijadaEmbestida = (objetivoJugador.position - transform.position).normalized;
        yield return new WaitForSeconds(tiempoDeCarga);

        estadoActual = EstadoEnemigo.Embestiendo;
        if (spriteEmbestida != null) spriteRenderer.sprite = spriteEmbestida;

        yield return new WaitForSeconds(duracionMaximaEmbestida);

        if (estadoActual == EstadoEnemigo.Embestiendo)
        {
            StartCoroutine(EntrarEnStun(false, false));
        }
    }

    private void ChequearImpactosEmbestida()
    {
        Vector2 posicionHocico = (Vector2)transform.position + ((Vector2)transform.right * desfaseHocicoDelantero);
        Collider2D[] impactos = Physics2D.OverlapCircleAll(posicionHocico, radioColisionAtaque);
        
        foreach (var col in impactos)
        {
            if (col.gameObject == gameObject) continue;

            SaludTiburon vidaTiburon = col.GetComponent<SaludTiburon>();
            if (vidaTiburon != null)
            {
                // Comprobamos si el jugador está haciendo parry
                Parry parry = col.GetComponent<Parry>();
                if (parry == null) parry = col.GetComponentInParent<Parry>();
                if (parry == null) parry = col.GetComponentInChildren<Parry>();

                if (parry != null && parry.IntentarParry(gameObject, danoAlJugador))
                {
                    // El daño, stun y efectos de pantalla/sonido se manejan dentro de IntentarParry
                    return;
                }
                else
                {
                    vidaTiburon.RecibirDano(danoAlJugador);
                    StopAllCoroutines();
                    StartCoroutine(EntrarEnStun(false, true)); 
                    return;
                }
            }

            if (col.gameObject.layer == LayerMask.NameToLayer("Paredes"))
            {
                StopAllCoroutines();
                StartCoroutine(EntrarEnStun(true, false)); 
                return;
            }
        }
    }

    public void OnParry(GameObject parriedBy, int damage)
    {
        // Devolvemos el daño a la barracuda
        VidaEnemigo vidaBarracuda = GetComponent<VidaEnemigo>();
        if (vidaBarracuda != null)
        {
            vidaBarracuda.RecibirDanoEnemigo(damage);
        }

        // Detenemos la embestida y la aturdimos
        StopAllCoroutines();
        StartCoroutine(EntrarEnStun(false, false));
    }

    private IEnumerator EntrarEnStun(bool porPared, bool golpeoAlJugador)
    {
        estadoActual = EstadoEnemigo.Aturdido;
        rb.linearVelocity = Vector2.zero; 
        if (spriteAturdido != null) spriteRenderer.sprite = spriteAturdido;

        float tiempoEspera = porPared ? stunPorPared : stunNormal;
        yield return new WaitForSeconds(tiempoEspera);

        estadoActual = EstadoEnemigo.Cooldown;
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;

        float cooldownFinal = golpeoAlJugador ? cooldownTrasAcertarHit : cooldownAtaque;
        yield return new WaitForSeconds(cooldownFinal);

        estadoActual = EstadoEnemigo.Cazando;
        StartCoroutine(RutinaPensamientoAtaque());
    }

    // Al final de BarracudaAtaque.cs, reemplaza las antiguas funciones de rotación por estas:

    private void RotarHaciaDireccionSuave(Vector2 dir)
    {
        if (dir.magnitude < 0.1f) return;

        // 1. Calculamos y redondeamos el ángulo a múltiplos de 45 grados
        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Quaternion rotacionObjetivo;

        // 2. Aplicamos tu lógica de la carpa para rellenar la rotación objetivo
        if (Mathf.Abs(anguloZ) > 90f)
        {
            rotacionObjetivo = Quaternion.Euler(180f, 0f, -anguloZ);
        }
        else if (Mathf.Abs(anguloZ) < 90f)
        {
            rotacionObjetivo = Quaternion.Euler(0f, 0f, anguloZ);
        }
        else
        {
            if (dir.x < 0)
                rotacionObjetivo = Quaternion.Euler(180f, 0f, -anguloZ);
            else
                rotacionObjetivo = Quaternion.Euler(0f, 0f, anguloZ);
        }

        // 3. Interpolamos suavemente hacia ese bloque de ángulo fijo
        transform.localRotation = Quaternion.Lerp(transform.localRotation, rotacionObjetivo, Time.fixedDeltaTime * velocidadGiroSuave);
    }

    private void RotarHaciaDireccionInstantanea(Vector2 dir)
    {
        if (dir.magnitude < 0.1f) return;

        // Misma lógica pero aplicada en el acto (para la embestida recta)
        float anguloZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        if (Mathf.Abs(anguloZ) > 90f)
        {
            transform.localRotation = Quaternion.Euler(180f, 0f, -anguloZ);
        }
        else if (Mathf.Abs(anguloZ) < 90f)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ);
        }
        else
        {
            if (dir.x < 0)
                transform.localRotation = Quaternion.Euler(180f, 0f, -anguloZ);
            else
                transform.localRotation = Quaternion.Euler(0f, 0f, anguloZ);
        }
    }

    private Vector2 EvaluarRutaYEsquivar(Vector2 dirBase)
    {
        Vector2 dirFrontal = dirBase;
        Vector2 dirIzquierda = GirarVector(dirBase, anguloAperturaSensores);
        Vector2 dirDerecha = GirarVector(dirBase, -anguloAperturaSensores);

        RaycastHit2D hitFrontal = Physics2D.Raycast(transform.position, dirFrontal, distanciaDeteccionPared, capaParedes);
        RaycastHit2D hitIzquierda = Physics2D.Raycast(transform.position, dirIzquierda, distanciaDeteccionPared, capaParedes);
        RaycastHit2D hitDerecha = Physics2D.Raycast(transform.position, dirDerecha, distanciaDeteccionPared, capaParedes);

        if (hitFrontal.collider == null && hitIzquierda.collider == null && hitDerecha.collider == null) return dirBase;

        if (hitFrontal.collider != null)
        {
            if (hitIzquierda.collider == null) return dirIzquierda;
            if (hitDerecha.collider == null) return dirDerecha;
            return Vector2.Reflect(dirBase, hitFrontal.normal).normalized;
        }
        if (hitIzquierda.collider != null) return dirDerecha;
        if (hitDerecha.collider != null) return dirIzquierda;

        return dirBase;
    }

    private Vector2 GirarVector(Vector2 vector, float grados)
    {
        float radianes = grados * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radianes);
        float cos = Mathf.Cos(radianes);
        return new Vector2((cos * vector.x) - (sin * vector.y), (sin * vector.x) + (cos * vector.y));
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

        Gizmos.color = Color.cyan;
        Vector2 dirBase = direccionDeseadaFinal == Vector2.zero ? (Vector2)transform.right : direccionDeseadaFinal;
        Vector2 dirFrontal = dirBase;
        Vector2 dirIzquierda = GirarVector(dirBase, anguloAperturaSensores);
        Vector2 dirDerecha = GirarVector(dirBase, -anguloAperturaSensores);

        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirFrontal * distanciaDeteccionPared);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirIzquierda * distanciaDeteccionPared);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dirDerecha * distanciaDeteccionPared);
    }
}