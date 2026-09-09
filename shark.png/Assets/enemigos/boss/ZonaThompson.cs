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
/// 1. Warning: cono rojo semi-transparente parpadeante con líneas de límite (~1.5s)
/// 2. Disparo: cono rojo sólido con flicker, daño continuo (~3s)
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
    [SerializeField] private Color colorWarning = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private Color colorDisparo = new Color(1f, 0.1f, 0f, 0.75f);

    [Header("Líneas Limitantes")]
    [SerializeField] private LineRenderer lineaLimiteIzq;
    [SerializeField] private LineRenderer lineaLimiteDer;
    [SerializeField] private float grosorLineas = 0.08f;
    [SerializeField] private Color colorLineasWarning = new Color(1f, 0.2f, 0.2f, 0.85f);
    [SerializeField] private Color colorLineasDisparo = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 10;

    // --- Referencias internas ---
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh meshCono;
    private Transform jugador;
    private Transform bossTransform;

    // --- Estado del ataque ---
    private bool ataqueActivo = false;
    private bool estaDisparando = false;
    private float anguloActual;     // Ángulo del cono elegido para este ataque
    private float anguloBase;       // Dirección central del cono (en grados, fija durante el ataque)
    private float tiempoProximoDano = 0f;
    private Color colorActual = Color.red;

    // --- Arrays reutilizables para evitar allocations ---
    private Vector3[] vertices;
    private Color[] colores;
    private Vector2[] uvs;
    private int[] triangulos;
    private Vector2[] puntosCono;

    public void Configurar(Transform boss)
    {
        bossTransform = boss;
        transform.SetParent(null);
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (transform.parent != null)
        {
            bossTransform = transform.parent;
            transform.SetParent(null);
        }

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        meshCono = new Mesh();
        meshCono.name = "ConoDañoThompson";
        meshFilter.mesh = meshCono;

        ConfigurarMaterial();
        PrealocarArrays();
        CrearLineasSiNoExisten();

        meshRenderer.enabled = false;
    }

    private void ConfigurarMaterial()
    {
        Material baseMat = meshRenderer.sharedMaterial;
        Material mat;

        if (baseMat != null)
        {
            mat = new Material(baseMat);
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("UI/Default");
            mat = new Material(shader);
        }

        mat.name = "Mat_ZonaThompson_Procedural";
        mat.mainTexture = Texture2D.whiteTexture;
        mat.color = colorWarning;
        mat.SetColor("_Color", colorWarning);

        if (mat.HasProperty("_RendererColor"))
            mat.SetColor("_RendererColor", Color.white);
        if (mat.HasProperty("_Flip"))
            mat.SetVector("_Flip", Vector4.one);

        mat.renderQueue = 3000;
        meshRenderer.material = mat;
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private void CrearLineasSiNoExisten()
    {
        if (lineaLimiteIzq == null)
        {
            GameObject goIzq = new GameObject("LineaLimiteIzq");
            goIzq.transform.SetParent(transform);
            lineaLimiteIzq = goIzq.AddComponent<LineRenderer>();
            ConfigurarLineRenderer(lineaLimiteIzq);
        }
        if (lineaLimiteDer == null)
        {
            GameObject goDer = new GameObject("LineaLimiteDer");
            goDer.transform.SetParent(transform);
            lineaLimiteDer = goDer.AddComponent<LineRenderer>();
            ConfigurarLineRenderer(lineaLimiteDer);
        }
    }

    private void ConfigurarLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.startWidth = grosorLineas;
        lr.endWidth = grosorLineas;
        lr.positionCount = 2;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder + 1;

        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Color");
        Material mat = new Material(s);
        mat.mainTexture = Texture2D.whiteTexture;
        mat.color = Color.white;
        if (mat.HasProperty("_RendererColor"))
            mat.SetColor("_RendererColor", Color.white);

        lr.material = mat;
        lr.enabled = false;
    }

    private void PrealocarArrays()
    {
        vertices = new Vector3[cantidadRayos + 1];
        colores = new Color[cantidadRayos + 1];
        uvs = new Vector2[cantidadRayos + 1];
        puntosCono = new Vector2[cantidadRayos];

        for (int i = 0; i <= cantidadRayos; i++)
        {
            colores[i] = Color.white;
            uvs[i] = new Vector2(0.5f, 0.5f);
        }

        // Doble cara para que nunca sufra de backface culling
        triangulos = new int[(cantidadRayos - 1) * 6];
        for (int i = 0; i < cantidadRayos - 1; i++)
        {
            // Cara 1
            triangulos[i * 6] = 0;
            triangulos[i * 6 + 1] = i + 1;
            triangulos[i * 6 + 2] = i + 2;

            // Cara 2 (inversa)
            triangulos[i * 6 + 3] = 0;
            triangulos[i * 6 + 4] = i + 2;
            triangulos[i * 6 + 5] = i + 1;
        }
    }

    /// <summary>
    /// Inicia el ataque con una dirección fija (no sigue al jugador durante el ataque).
    /// </summary>
    public void IniciarAtaque(Vector2 direccionFijada, System.Action onDisparoIniciado = null, System.Action onAtaqueTerminado = null)
    {
        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindWithTag("Player");
            if (objJugador != null) jugador = objJugador.transform;
        }

        if (bossTransform == null && transform.parent != null)
        {
            bossTransform = transform.parent;
            transform.SetParent(null);
        }

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        anguloActual = Random.Range(anguloConoMin, anguloConoMax);
        // Fijar dirección: a partir de aquí NO sigue al jugador con la mirada
        anguloBase = Mathf.Atan2(direccionFijada.y, direccionFijada.x) * Mathf.Rad2Deg;

        gameObject.SetActive(true);
        meshRenderer.enabled = true;

        StopAllCoroutines();
        StartCoroutine(SecuenciaAtaque(onDisparoIniciado, onAtaqueTerminado));
    }

    public void IniciarAtaque(System.Action onAtaqueTerminado = null)
    {
        Vector2 origen = bossTransform != null ? (Vector2)bossTransform.position : (Vector2)transform.position;
        Vector2 dir = Vector2.right;
        if (jugador != null)
        {
            dir = ((Vector2)jugador.position - origen).normalized;
        }

        IniciarAtaque(dir, null, onAtaqueTerminado);
    }

    private IEnumerator SecuenciaAtaque(System.Action onDisparo, System.Action onTerminado)
    {
        ataqueActivo = true;
        estaDisparando = false;

        // FASE 1: WARNING (cono rojo parpadeante con dirección fija telegrafiando el ataque)
        float tWarning = 0f;
        while (tWarning < tiempoWarning)
        {
            tWarning += Time.deltaTime;

            if (bossTransform == null)
            {
                DetenerAtaque();
                yield break;
            }

            // Parpadeo visual en la dirección fijada
            float alpha = Mathf.Lerp(colorWarning.a * 0.5f, colorWarning.a, Mathf.PingPong(tWarning * 5f, 1f));
            SetColorMesh(new Color(colorWarning.r, colorWarning.g, colorWarning.b, alpha));

            ReconstruirMesh();

            yield return null;
        }

        // FASE 2: DISPARO (cono sólido con flicker y daño en la dirección fija)
        estaDisparando = true;
        tiempoProximoDano = 0f;
        onDisparo?.Invoke();

        float tDisparo = 0f;
        while (tDisparo < tiempoDisparo)
        {
            tDisparo += Time.deltaTime;

            if (bossTransform == null)
            {
                DetenerAtaque();
                yield break;
            }

            ReconstruirMesh();

            if (Time.time >= tiempoProximoDano)
            {
                IntentarDanarJugador();
                tiempoProximoDano = Time.time + intervaloDano;
            }

            float flicker = Random.Range(colorDisparo.a * 0.7f, colorDisparo.a);
            SetColorMesh(new Color(colorDisparo.r, colorDisparo.g, colorDisparo.b, flicker));

            yield return null;
        }

        // FIN DEL ATAQUE
        ataqueActivo = false;
        estaDisparando = false;
        meshRenderer.enabled = false;
        if (meshCono != null) meshCono.Clear();
        if (lineaLimiteIzq != null) lineaLimiteIzq.enabled = false;
        if (lineaLimiteDer != null) lineaLimiteDer.enabled = false;
        gameObject.SetActive(false);

        onTerminado?.Invoke();
    }

    private void ReconstruirMesh()
    {
        Vector2 origen = bossTransform != null
            ? (Vector2)bossTransform.position
            : (Vector2)transform.position;

        transform.position = new Vector3(origen.x, origen.y, -0.05f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        float anguloInicio = anguloBase - anguloActual / 2f;
        float anguloFin = anguloBase + anguloActual / 2f;
        float paso = (anguloFin - anguloInicio) / (cantidadRayos - 1);

        vertices[0] = Vector3.zero;

        for (int i = 0; i < cantidadRayos; i++)
        {
            float angulo = anguloInicio + paso * i;
            float rad = angulo * Mathf.Deg2Rad;
            Vector2 direccionRayo = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // RaycastAll para ignorar triggers (límites de cámara, salas, etc.) y no colapsar a distancia 0
            RaycastHit2D[] hits = Physics2D.RaycastAll(origen, direccionRayo, distanciaMax, capaObstaculos);
            float distanciaFinal = distanciaMax;

            for (int j = 0; j < hits.Length; j++)
            {
                RaycastHit2D h = hits[j];
                if (h.collider == null) continue;
                if (h.collider.isTrigger) continue; // ¡Triggers nunca bloquean!
                if (bossTransform != null && (h.collider.transform == bossTransform || h.collider.transform.IsChildOf(bossTransform))) continue;
                if (h.distance < 0.15f) continue;

                distanciaFinal = h.distance;
                break;
            }

            Vector2 puntoLocal = direccionRayo * distanciaFinal;
            vertices[i + 1] = new Vector3(puntoLocal.x, puntoLocal.y, 0f);
            puntosCono[i] = puntoLocal;
        }

        meshCono.Clear();
        meshCono.vertices = vertices;
        meshCono.uv = uvs;
        meshCono.colors = colores;
        meshCono.triangles = triangulos;
        meshCono.RecalculateNormals();
        meshCono.RecalculateBounds();

        // Actualizar líneas limitantes exteriores
        if (lineaLimiteIzq != null)
        {
            lineaLimiteIzq.enabled = true;
            lineaLimiteIzq.SetPosition(0, (Vector3)origen);
            lineaLimiteIzq.SetPosition(1, (Vector3)(origen + (Vector2)vertices[1]));
            Color col = estaDisparando ? colorLineasDisparo : colorLineasWarning;
            lineaLimiteIzq.startColor = col;
            lineaLimiteIzq.endColor = col;
        }
        if (lineaLimiteDer != null)
        {
            lineaLimiteDer.enabled = true;
            lineaLimiteDer.SetPosition(0, (Vector3)origen);
            lineaLimiteDer.SetPosition(1, (Vector3)(origen + (Vector2)vertices[cantidadRayos]));
            Color col = estaDisparando ? colorLineasDisparo : colorLineasWarning;
            lineaLimiteDer.startColor = col;
            lineaLimiteDer.endColor = col;
        }
    }

    private void SetColorMesh(Color color)
    {
        colorActual = color;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
            meshRenderer.material.SetColor("_Color", color);
            if (meshRenderer.material.HasProperty("_RendererColor"))
                meshRenderer.material.SetColor("_RendererColor", Color.white);
        }
    }

    private void IntentarDanarJugador()
    {
        if (jugador == null) return;

        SaludTiburon salud = jugador.GetComponent<SaludTiburon>();
        if (salud == null || salud.esInvencible) return;

        Vector2 origen = bossTransform != null
            ? (Vector2)bossTransform.position
            : (Vector2)transform.position;

        Vector2 dirAlJugador = (Vector2)jugador.position - origen;
        float distanciaJugador = dirAlJugador.magnitude;

        if (distanciaJugador > distanciaMax) return;

        float anguloJugador = Mathf.Atan2(dirAlJugador.y, dirAlJugador.x) * Mathf.Rad2Deg;
        float diferenciaAngulo = Mathf.DeltaAngle(anguloBase, anguloJugador);

        if (Mathf.Abs(diferenciaAngulo) > anguloActual / 2f) return;

        // Comprobar si hay obstáculos sólidos que cubran al jugador
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dirAlJugador.normalized, distanciaJugador, capaObstaculos);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null) continue;
            if (h.collider.isTrigger) continue; // Triggers no cubren
            if (bossTransform != null && (h.collider.transform == bossTransform || h.collider.transform.IsChildOf(bossTransform))) continue;
            if (h.collider.transform == jugador || h.collider.transform.IsChildOf(jugador)) continue;
            if (h.distance < 0.15f) continue;

            // Hay un obstáculo sólido entre boss y jugador
            return;
        }

        salud.RecibirDano(danoPorHit);
    }

    public void DetenerAtaque()
    {
        StopAllCoroutines();
        ataqueActivo = false;
        estaDisparando = false;

        if (meshRenderer != null) meshRenderer.enabled = false;
        if (meshCono != null) meshCono.Clear();
        if (lineaLimiteIzq != null) lineaLimiteIzq.enabled = false;
        if (lineaLimiteDer != null) lineaLimiteDer.enabled = false;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (meshCono != null)
            Destroy(meshCono);
        if (lineaLimiteIzq != null && lineaLimiteIzq.gameObject != null)
            Destroy(lineaLimiteIzq.gameObject);
        if (lineaLimiteDer != null && lineaLimiteDer.gameObject != null)
            Destroy(lineaLimiteDer.gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!ataqueActivo) return;

        Vector2 origen = bossTransform != null
            ? (Vector2)bossTransform.position
            : (Vector2)transform.position;

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
