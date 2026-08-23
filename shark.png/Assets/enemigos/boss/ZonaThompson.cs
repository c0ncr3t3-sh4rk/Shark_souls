using UnityEngine;
using System.Collections;

/// <summary>
/// Ataque Thompson del boss con sistema de sombras de obstáculos.
/// 
/// Genera un cono de daño visual (mesh procedural) que se adapta en tiempo real
/// a los obstáculos del mapa. Si un obstáculo (roca, pared) está entre el boss
/// y el jugador, el jugador NO recibe daño (está "cubierto").
/// 
/// Fases:
/// 1. Warning: cono rojo semi-transparente parpadeante (~1.5s)
/// 2. Disparo: cono rojo sólido con flicker, daño continuo (~3s)
/// 
/// Configuración en Unity:
/// - Necesita MeshFilter + MeshRenderer en el mismo GameObject
/// - El MeshRenderer necesita un material (Sprites/Default funciona)
/// - Configurar capaObstaculos con las layers que bloquean (ej. Salas)
/// - Ajustar Sorting Layer del MeshRenderer para que esté sobre el suelo
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ZonaThompson : MonoBehaviour
{
    [Header("Forma del Cono")]
    [Tooltip("Ángulo mínimo del cono (grados). Se elige aleatoriamente entre min y max.")]
    [SerializeField] private float anguloConoMin = 30f;
    [Tooltip("Ángulo máximo del cono (grados). Se elige aleatoriamente entre min y max.")]
    [SerializeField] private float anguloConoMax = 90f;
    [Tooltip("Distancia máxima del cono desde el boss.")]
    [SerializeField] private float distanciaMax = 12f;
    [Tooltip("Número de rayos del abanico. Más rayos = más resolución visual pero más coste.")]
    [SerializeField] private int cantidadRayos = 60;

    [Header("Detección de Obstáculos")]
    [Tooltip("Layers que bloquean el disparo (paredes, rocas, etc.)")]
    [SerializeField] private LayerMask capaObstaculos;

    [Header("Tiempos")]
    [SerializeField] private float tiempoWarning = 1.5f;
    [SerializeField] private float tiempoDisparo = 3f;
    [SerializeField] private float intervaloDano = 0.3f;

    [Header("Daño")]
    [SerializeField] private int danoPorHit = 2;

    [Header("Colores")]
    [SerializeField] private Color colorWarning = new Color(1f, 0f, 0f, 0.25f);
    [SerializeField] private Color colorDisparo = new Color(1f, 0f, 0f, 0.6f);

    // --- Referencias internas ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh meshCono;
    private Transform jugador;

    // --- Estado del ataque ---
    private bool ataqueActivo = false;
    private bool estaDisparando = false;
    private float anguloActual;     // Ángulo del cono elegido para este ataque
    private float anguloBase;       // Dirección central del cono (en grados)
    private float tiempoProximoDano = 0f;

    // --- Arrays reutilizables para evitar allocations ---
    private Vector3[] vertices;
    private int[] triangulos;
    private Vector2[] puntosCono;   // Puntos finales de los rayos (espacio local)

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshCono = new Mesh();
        meshCono.name = "ConoDañoThompson";
        meshFilter.mesh = meshCono;

        // Pre-allocar arrays
        PrealocarArrays();

        // Empezar oculto
        meshRenderer.enabled = false;
    }

    private void PrealocarArrays()
    {
        // Vértices: 1 centro (boss) + cantidadRayos puntos del arco
        vertices = new Vector3[cantidadRayos + 1];
        puntosCono = new Vector2[cantidadRayos];

        // Triángulos: cantidadRayos - 1 triángulos (abanico)
        triangulos = new int[(cantidadRayos - 1) * 3];
        for (int i = 0; i < cantidadRayos - 1; i++)
        {
            triangulos[i * 3] = 0;           // Centro
            triangulos[i * 3 + 1] = i + 1;   // Punto actual del arco
            triangulos[i * 3 + 2] = i + 2;   // Siguiente punto del arco
        }
    }

    /// <summary>
    /// Inicia la secuencia del ataque Thompson.
    /// Llamar desde CarpaBoss con la dirección hacia el jugador.
    /// </summary>
    /// <param name="direccionAlJugador">Dirección normalizada del boss al jugador</param>
    /// <param name="onAtaqueTerminado">Callback cuando el ataque termina</param>
    public void IniciarAtaque(Vector2 direccionAlJugador, System.Action onAtaqueTerminado = null)
    {
        // Buscar jugador
        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
        }

        // Elegir ángulo aleatorio del cono
        anguloActual = Random.Range(anguloConoMin, anguloConoMax);

        // Calcular dirección central en grados
        anguloBase = Mathf.Atan2(direccionAlJugador.y, direccionAlJugador.x) * Mathf.Rad2Deg;

        gameObject.SetActive(true);
        meshRenderer.enabled = true;

        StartCoroutine(SecuenciaAtaque(onAtaqueTerminado));
    }

    /// <summary>
    /// Sobrecarga de compatibilidad: sin dirección (usa la última calculada).
    /// Mantenida por si CarpaBoss la llama sin parámetros.
    /// </summary>
    public void IniciarAtaque(System.Action onAtaqueTerminado = null)
    {
        // Calcular dirección al jugador desde la posición del padre (el boss)
        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
        }

        Vector2 dir = Vector2.right;
        if (jugador != null)
        {
            dir = ((Vector2)jugador.position - (Vector2)transform.parent.position).normalized;
        }

        IniciarAtaque(dir, onAtaqueTerminado);
    }

    private IEnumerator SecuenciaAtaque(System.Action onTerminado)
    {
        ataqueActivo = true;
        estaDisparando = false;

        // ========================================
        // FASE 1: WARNING (visual sin daño)
        // ========================================
        float tWarning = 0f;
        while (tWarning < tiempoWarning)
        {
            tWarning += Time.deltaTime;

            // Reconstruir el mesh cada frame para reflejar cambios en obstáculos
            ReconstruirMesh();

            // Parpadeo de warning
            float alpha = Mathf.Lerp(0.1f, 0.35f, Mathf.PingPong(tWarning * 4f, 1f));
            SetColorMesh(new Color(1f, 0f, 0f, alpha));

            yield return null;
        }

        // ========================================
        // FASE 2: DISPARO (visual + daño)
        // ========================================
        estaDisparando = true;
        tiempoProximoDano = 0f;

        float tDisparo = 0f;
        while (tDisparo < tiempoDisparo)
        {
            tDisparo += Time.deltaTime;

            // Reconstruir mesh (los obstáculos podrían moverse, aunque raro)
            ReconstruirMesh();

            // Intentar dañar al jugador
            if (Time.time >= tiempoProximoDano)
            {
                IntentarDanarJugador();
                tiempoProximoDano = Time.time + intervaloDano;
            }

            // Efecto visual: flicker rojo/naranja durante el disparo
            float flicker = Random.Range(0.45f, 0.75f);
            SetColorMesh(new Color(1f, Random.Range(0f, 0.15f), 0f, flicker));

            yield return null;
        }

        // ========================================
        // FIN DEL ATAQUE
        // ========================================
        ataqueActivo = false;
        estaDisparando = false;
        meshRenderer.enabled = false;
        meshCono.Clear();
        gameObject.SetActive(false);

        onTerminado?.Invoke();
    }

    // ========================================
    // MESH PROCEDURAL
    // ========================================

    /// <summary>
    /// Reconstruye el mesh del cono lanzando rayos y detectando obstáculos.
    /// Los obstáculos crean "sombras" (huecos) en el cono.
    /// </summary>
    private void ReconstruirMesh()
    {
        // Posición del boss (el padre de este GameObject)
        Vector2 origen = transform.parent != null
            ? (Vector2)transform.parent.position
            : (Vector2)transform.position;

        // Colocar este GO en la posición del boss y anular rotación y escala del padre
        transform.position = (Vector3)origen;
        transform.rotation = Quaternion.identity;
        
        if (transform.parent != null)
        {
            Vector3 pScale = transform.parent.localScale;
            transform.localScale = new Vector3(
                1f / (pScale.x != 0 ? pScale.x : 1f),
                1f / (pScale.y != 0 ? pScale.y : 1f),
                1f / (pScale.z != 0 ? pScale.z : 1f)
            );
        }

        // Calcular el rango angular del cono
        float anguloInicio = anguloBase - anguloActual / 2f;
        float anguloFin = anguloBase + anguloActual / 2f;
        float paso = (anguloFin - anguloInicio) / (cantidadRayos - 1);

        // Vértice 0 = centro (boss), en espacio local es (0,0,0)
        vertices[0] = Vector3.zero;

        for (int i = 0; i < cantidadRayos; i++)
        {
            float angulo = anguloInicio + paso * i;
            float rad = angulo * Mathf.Deg2Rad;
            Vector2 direccionRayo = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // Lanzar raycast desde el boss en esta dirección
            RaycastHit2D hit = Physics2D.Raycast(origen, direccionRayo, distanciaMax, capaObstaculos);

            float distanciaFinal;
            if (hit.collider != null)
            {
                // El rayo chocó con un obstáculo: el punto final es el impacto
                distanciaFinal = hit.distance;
            }
            else
            {
                // Sin obstáculo: alcance máximo
                distanciaFinal = distanciaMax;
            }

            // Guardar punto en espacio local (relativo al origen/boss)
            Vector2 puntoLocal = direccionRayo * distanciaFinal;
            vertices[i + 1] = new Vector3(puntoLocal.x, puntoLocal.y, 0f);
            puntosCono[i] = puntoLocal;
        }

        // Aplicar al mesh
        meshCono.Clear();
        meshCono.vertices = vertices;
        meshCono.triangles = triangulos;
        meshCono.RecalculateNormals();
    }

    /// <summary>
    /// Cambia el color del material del mesh.
    /// </summary>
    private void SetColorMesh(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    // ========================================
    // SISTEMA DE DAÑO CON COBERTURA
    // ========================================

    /// <summary>
    /// Comprueba si el jugador está en la zona de daño y no está cubierto
    /// por un obstáculo. Usa dos comprobaciones:
    /// 1. ¿Está dentro del ángulo del cono?
    /// 2. ¿Hay línea de visión directa (sin obstáculos entre boss y jugador)?
    /// </summary>
    private void IntentarDanarJugador()
    {
        if (jugador == null) return;

        SaludTiburon salud = jugador.GetComponent<SaludTiburon>();
        if (salud == null || salud.esInvencible) return;

        Vector2 origen = transform.parent != null
            ? (Vector2)transform.parent.position
            : (Vector2)transform.position;

        Vector2 dirAlJugador = (Vector2)jugador.position - origen;
        float distanciaJugador = dirAlJugador.magnitude;

        // ---- Comprobación 1: ¿Está dentro del rango? ----
        if (distanciaJugador > distanciaMax) return;

        // ---- Comprobación 2: ¿Está dentro del ángulo del cono? ----
        float anguloJugador = Mathf.Atan2(dirAlJugador.y, dirAlJugador.x) * Mathf.Rad2Deg;
        float diferenciaAngulo = Mathf.DeltaAngle(anguloBase, anguloJugador);

        if (Mathf.Abs(diferenciaAngulo) > anguloActual / 2f) return;

        // ---- Comprobación 3: ¿Hay línea de visión? (no hay obstáculo en medio) ----
        RaycastHit2D hit = Physics2D.Raycast(origen, dirAlJugador.normalized, distanciaJugador, capaObstaculos);

        if (hit.collider != null)
        {
            // Hay un obstáculo entre el boss y el jugador → CUBIERTO, no dañar
            return;
        }

        // ---- El jugador está expuesto: DAÑO ----
        salud.RecibirDano(danoPorHit);
    }

    // ========================================
    // DETENER ATAQUE (interrupción externa)
    // ========================================

    /// <summary>
    /// Fuerza la detención del ataque (ej. si el boss muere o es aturdido).
    /// </summary>
    public void DetenerAtaque()
    {
        StopAllCoroutines();
        ataqueActivo = false;
        estaDisparando = false;

        if (meshRenderer != null) meshRenderer.enabled = false;
        if (meshCono != null) meshCono.Clear();

        gameObject.SetActive(false);
    }

    // ========================================
    // GIZMOS (debug en editor)
    // ========================================

    private void OnDrawGizmosSelected()
    {
        if (!ataqueActivo) return;

        Vector2 origen = transform.parent != null
            ? (Vector2)transform.parent.position
            : (Vector2)transform.position;

        // Dibujar los límites del cono
        float anguloIzq = anguloBase - anguloActual / 2f;
        float anguloDer = anguloBase + anguloActual / 2f;

        Vector2 dirIzq = new Vector2(
            Mathf.Cos(anguloIzq * Mathf.Deg2Rad),
            Mathf.Sin(anguloIzq * Mathf.Deg2Rad)
        );
        Vector2 dirDer = new Vector2(
            Mathf.Cos(anguloDer * Mathf.Deg2Rad),
            Mathf.Sin(anguloDer * Mathf.Deg2Rad)
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine((Vector3)origen, (Vector3)(origen + dirIzq * distanciaMax));
        Gizmos.DrawLine((Vector3)origen, (Vector3)(origen + dirDer * distanciaMax));
    }
}
