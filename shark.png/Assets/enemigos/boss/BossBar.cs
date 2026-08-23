using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Barra de vida del boss. Se crea enteramente por código.
/// Simplemente pon este script en un GameObject vacío en la escena
/// y CarpaBoss lo encontrará automáticamente.
/// 
/// Crea su propio Canvas con:
/// - Fondo gris oscuro
/// - Barra fantasma blanca (baja con delay)
/// - Barra de vida roja (baja instantánea)
/// - Texto con nombre del boss
/// </summary>
public class BossBar : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string nombreBoss = "La Gran Carpa";
    [SerializeField] private float velocidadFantasma = 0.5f;
    [SerializeField] private float delayFantasma = 0.5f;
    [SerializeField] private float tiempoEntrada = 1f;

    // --- UI generada por código ---
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelBossBar;
    private Image barraVida;
    private Image barraFantasma;

    // --- Estado ---
    private float vidaObjetivoNormalizada = 1f;
    private float vidaFantasmaNormalizada = 1f;
    private float tiempoUltimoDano = 0f;
    private VidaEnemigo vidaBoss;
    private bool estaVisible = false;
    private bool uiCreada = false;

    private void Awake()
    {
        CrearUI();
        // Empezar oculto
        canvasGroup.alpha = 0f;
        canvas.gameObject.SetActive(false);
    }

    // ========================================
    // CREACIÓN DE UI POR CÓDIGO
    // ========================================

    private void CrearUI()
    {
        if (uiCreada) return;
        uiCreada = true;

        // ---- Canvas propio ----
        GameObject canvasGO = new GameObject("BossBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Por encima del resto de UI

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // ---- Panel contenedor (parte inferior, centrado) ----
        GameObject panelGO = new GameObject("PanelBossBar");
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelBossBar = panelGO.AddComponent<RectTransform>();

        // Anclado abajo centro
        panelBossBar.anchorMin = new Vector2(0.5f, 0f);
        panelBossBar.anchorMax = new Vector2(0.5f, 0f);
        panelBossBar.pivot = new Vector2(0.5f, 0f);
        panelBossBar.anchoredPosition = new Vector2(0f, 40f);
        panelBossBar.sizeDelta = new Vector2(700f, 32f);

        // Crear un sprite 1x1 blanco por código para que funcionen los rellenos (FillAmount)
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite spriteBlanco = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);

        // ---- Fondo gris oscuro ----
        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(panelGO.transform, false);
        RectTransform fondoRT = fondoGO.AddComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero;
        fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = Vector2.zero;
        fondoRT.offsetMax = Vector2.zero;

        fondoGO.AddComponent<CanvasRenderer>();
        Image fondoImg = fondoGO.AddComponent<Image>();
        fondoImg.sprite = spriteBlanco;
        fondoImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // ---- Borde del fondo (outline simple con otra Image detrás) ----
        GameObject bordeGO = new GameObject("Borde");
        bordeGO.transform.SetParent(panelGO.transform, false);
        bordeGO.transform.SetAsFirstSibling();
        RectTransform bordeRT = bordeGO.AddComponent<RectTransform>();
        bordeRT.anchorMin = Vector2.zero;
        bordeRT.anchorMax = Vector2.one;
        bordeRT.offsetMin = new Vector2(-2f, -2f);
        bordeRT.offsetMax = new Vector2(2f, 2f);

        bordeGO.AddComponent<CanvasRenderer>();
        Image bordeImg = bordeGO.AddComponent<Image>();
        bordeImg.sprite = spriteBlanco;
        bordeImg.color = new Color(0.6f, 0.1f, 0.1f, 1f);

        // ---- Barra fantasma (blanca, detrás de la roja) ----
        GameObject fantasmaGO = new GameObject("BarraFantasma");
        fantasmaGO.transform.SetParent(panelGO.transform, false);
        RectTransform fantasmaRT = fantasmaGO.AddComponent<RectTransform>();
        fantasmaRT.anchorMin = new Vector2(0f, 0f);
        fantasmaRT.anchorMax = new Vector2(1f, 1f);
        fantasmaRT.offsetMin = new Vector2(4f, 4f);
        fantasmaRT.offsetMax = new Vector2(-4f, -4f);

        fantasmaGO.AddComponent<CanvasRenderer>();
        barraFantasma = fantasmaGO.AddComponent<Image>();
        barraFantasma.sprite = spriteBlanco;
        barraFantasma.color = new Color(1f, 1f, 1f, 0.6f);
        barraFantasma.type = Image.Type.Filled;
        barraFantasma.fillMethod = Image.FillMethod.Horizontal;
        barraFantasma.fillOrigin = 0;
        barraFantasma.fillAmount = 1f;

        // ---- Barra de vida roja ----
        GameObject vidaGO = new GameObject("BarraVida");
        vidaGO.transform.SetParent(panelGO.transform, false);
        RectTransform vidaRT = vidaGO.AddComponent<RectTransform>();
        vidaRT.anchorMin = new Vector2(0f, 0f);
        vidaRT.anchorMax = new Vector2(1f, 1f);
        vidaRT.offsetMin = new Vector2(4f, 4f);
        vidaRT.offsetMax = new Vector2(-4f, -4f);

        vidaGO.AddComponent<CanvasRenderer>();
        barraVida = vidaGO.AddComponent<Image>();
        barraVida.sprite = spriteBlanco;
        barraVida.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        barraVida.type = Image.Type.Filled;
        barraVida.fillMethod = Image.FillMethod.Horizontal;
        barraVida.fillOrigin = 0;
        barraVida.fillAmount = 1f;

        // ---- Texto nombre del boss (encima de la barra) ----
        GameObject textoGO = new GameObject("TextoNombre");
        textoGO.transform.SetParent(panelGO.transform, false);
        RectTransform textoRT = textoGO.AddComponent<RectTransform>();
        textoRT.anchorMin = new Vector2(0.5f, 1f);
        textoRT.anchorMax = new Vector2(0.5f, 1f);
        textoRT.pivot = new Vector2(0.5f, 0f);
        textoRT.anchoredPosition = new Vector2(0f, 6f);
        textoRT.sizeDelta = new Vector2(400f, 30f);

        textoGO.AddComponent<CanvasRenderer>();
        Text textoComp = textoGO.AddComponent<Text>();
        textoComp.text = nombreBoss;
        textoComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoComp.fontSize = 22;
        textoComp.fontStyle = FontStyle.Bold;
        textoComp.alignment = TextAnchor.MiddleCenter;
        textoComp.color = Color.white;
        textoComp.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Sombra al texto
        Shadow sombra = textoGO.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(1.5f, -1.5f);
    }

    // ========================================
    // VINCULACIÓN CON EL BOSS
    // ========================================

    /// <summary>
    /// Vincula la boss bar a un enemigo con VidaEnemigo.
    /// Llama esto cuando el boss aparece en la sala.
    /// </summary>
    public void Vincular(VidaEnemigo vida)
    {
        gameObject.SetActive(true);

        vidaBoss = vida;

        if (!uiCreada) CrearUI();

        canvas.gameObject.SetActive(true);

        // Configurar estado inicial
        vidaObjetivoNormalizada = 1f;
        vidaFantasmaNormalizada = 1f;

        barraVida.fillAmount = 1f;
        barraFantasma.fillAmount = 1f;

        // Suscribirse a eventos
        vidaBoss.OnVidaCambiada += ActualizarVida;
        vidaBoss.OnMuerto += OnBossMuerto;

        // Animación de entrada
        StartCoroutine(AnimacionEntrada());
    }

    private void OnDestroy()
    {
        if (vidaBoss != null)
        {
            vidaBoss.OnVidaCambiada -= ActualizarVida;
            vidaBoss.OnMuerto -= OnBossMuerto;
        }
    }

    // ========================================
    // ACTUALIZACIÓN
    // ========================================

    private void ActualizarVida(int vidaActual, int vidaMaxima)
    {
        if (vidaMaxima <= 0) return;

        vidaObjetivoNormalizada = Mathf.Clamp01((float)vidaActual / vidaMaxima);

        // Actualizar barra roja inmediatamente
        barraVida.fillAmount = vidaObjetivoNormalizada;

        tiempoUltimoDano = Time.time;
    }

    private void Update()
    {
        if (!estaVisible) return;

        // Barra fantasma (baja más lento, siguiendo a la roja con delay)
        if (Time.time - tiempoUltimoDano > delayFantasma)
        {
            vidaFantasmaNormalizada = Mathf.MoveTowards(
                vidaFantasmaNormalizada,
                vidaObjetivoNormalizada,
                velocidadFantasma * Time.deltaTime
            );
            barraFantasma.fillAmount = vidaFantasmaNormalizada;
        }
    }

    // ========================================
    // ANIMACIONES
    // ========================================

    private void OnBossMuerto()
    {
        StartCoroutine(AnimacionMuerte());
    }

    private IEnumerator AnimacionEntrada()
    {
        estaVisible = false;

        float posYInicial = -80f;
        float posYFinal = 40f;

        float t = 0f;
        while (t < tiempoEntrada)
        {
            t += Time.deltaTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t / tiempoEntrada);

            canvasGroup.alpha = smooth;
            panelBossBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(posYInicial, posYFinal, smooth));

            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelBossBar.anchoredPosition = new Vector2(0f, posYFinal);
        estaVisible = true;
    }

    private IEnumerator AnimacionMuerte()
    {
        // Flash blanco
        barraVida.color = Color.white;
        yield return new WaitForSeconds(0.3f);

        // Fade out
        float t = 0f;
        float duracion = 1.5f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duracion);
            yield return null;
        }

        canvas.gameObject.SetActive(false);
    }
}
