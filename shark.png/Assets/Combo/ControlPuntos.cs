using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI Componentes")]
    [SerializeField] private TextMeshProUGUI PuntuacionTemp;       // Muestra el acumulado de la pelea actual
    [SerializeField] private TextMeshProUGUI PuntuacionTotalTexto; // Muestra los puntos globales/permanentes

    [Header("Configuración del Mensaje")]
    [SerializeField] private GameObject prefabTextoFlotante; // El prefab del textico flotante
    [SerializeField] private Transform contenedorUI;         // El Canvas o panel donde se crean los textos

    private int puntuacionActual = 0; // Total acumulado de la pelea actual
    private int puntuacionTotal = 0;  // Puntaje general permanente

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ActualizarUICombo();
        ActualizarUITotal();
    }

    // 1. LLAMAR MIENTRAS DURA EL COMBO: Acumula en la pelea y suelta el textico
    public void AcumularPuntosPelea(int cantidad, string mensaje)
    {
        puntuacionActual += cantidad;
        ActualizarUICombo();

        // Creamos el textico flotante con el mensaje personalizado
        SpawnearTextoMensaje(cantidad, mensaje);
    }

    // 2. LLAMAR CUANDO EL COMBO TERMINA: Tu script de combo llamará a este método
    public void FinalizarComboYSumarAlTotal()
    {
        puntuacionTotal += puntuacionActual; // Pasamos los puntos de la pelea al total
        puntuacionActual = 0;                // Reseteamos el acumulado de la pelea

        ActualizarUICombo();
        ActualizarUITotal();
    }

    // --- MÉTODOS DE INTERFAZ ---

    private void ActualizarUICombo()
    {
        if (PuntuacionTemp != null)
        {
            // Si el acumulado es 0, ocultamos el texto para limpiar la pantalla
            PuntuacionTemp.text = puntuacionActual > 0 ? $"Combo: +{puntuacionActual}" : "";
        }
    }

    private void ActualizarUITotal()
    {
        if (PuntuacionTotalTexto != null)
        {
            PuntuacionTotalTexto.text = "Puntos: " + puntuacionTotal.ToString();
        }
    }

    private void SpawnearTextoMensaje(int cantidad, string mensaje)
    {
        if (prefabTextoFlotante == null || contenedorUI == null) return;

        // Instanciar el cartelito dentro del Canvas
        GameObject nuevoTexto = Instantiate(prefabTextoFlotante, contenedorUI);
        
        if (nuevoTexto.TryGetComponent(out TextMeshProUGUI textoComp))
        {
            // Ejemplo de resultado: "¡Parry! +100" o "¡Pez Globo! +50"
            textoComp.text = $"{mensaje} +{cantidad}";
        }

        // Se destruye en 1.5 segundos (puedes meterle un script al prefab para que flote)
        Destroy(nuevoTexto, 1.5f);
    }

    public void ResetearPuntuacion()
    {
        puntuacionActual = 0;
        puntuacionTotal = 0;
        ActualizarUICombo();
        ActualizarUITotal();
    }
}