using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 👈 OBLIGATORIO: Para usar listas de corazones

public class SaludTiburon : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private int vidasMaximas = 3; 
    private int vidasActuales;

    [Header("Conexión con la GUI (Corazones)")]
    // Arrastra aquí el objeto 'ContenedorCorazones'
    [SerializeField] private Transform contenedorCorazones; 
    // Arrastra aquí el archivo azul 'Corazon_Prefab' desde tus carpetas
    [SerializeField] private GameObject corazonPrefab; 

    // Lista interna para guardar los corazones que se vayan creando
    private List<GameObject> listaCorazones = new List<GameObject>();

    [Header("Frames de Invencibilidad (I-Frames)")]
    [SerializeField] private float duracionInvencibilidadHit = 1.5f; 
    private bool esInvencible = false;

    [Header("Efectos Visuales (UI y Sprite)")]
    [SerializeField] private GameObject pantallaRoja; 
    [SerializeField] private float duracionFlash = 0.2f;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        vidasActuales = vidasMaximas;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (pantallaRoja != null) pantallaRoja.SetActive(false);

        // Generamos los corazones visuales al iniciar
        CrearCorazonesUI();
    }

    public void SetInvencible(bool estado)
    {
        if (!esInvencible || estado == false)
        {
            esInvencible = estado;
        }
    }

    public void RecibirDano(int cantidadDano)
    {
        if (esInvencible) return;

        vidasActuales -= cantidadDano;
        Debug.Log($"¡Tiburón golpeado! Vidas restantes: {vidasActuales}");

        // Actualizamos los corazones visuales inmediatamente
        ActualizarCorazonesUI();

        if (pantallaRoja != null) StartCoroutine(EfectoPantallaRoja());

        if (vidasActuales <= 0)
        {
            Muerte();
            return; 
        }

        StartCoroutine(IFrames());
    }

    // 🎯 NUEVO MÉTODO: Dibuja los corazones iniciales en pantalla de forma dinámica
    private void CrearCorazonesUI()
    {
        // Limpiamos por si acaso
        foreach (GameObject corazon in listaCorazones) Destroy(corazon);
        listaCorazones.Clear();

        // Creamos tantos clones del corazón como vidas máximas tenga
        for (int i = 0; i < vidasMaximas; i++)
        {
            GameObject nuevoCorazon = Instantiate(corazonPrefab, contenedorCorazones);
            listaCorazones.Add(nuevoCorazon);
        }
    }

    // 🎯 NUEVO MÉTODO: Apaga los corazones perdidos
    private void ActualizarCorazonesUI()
    {
        for (int i = 0; i < listaCorazones.Count; i++)
        {
            // Si el índice es menor que nuestras vidas actuales, el corazón se enciende.
            // Si sufrimos daño, los corazones del final se apagarán automáticamente.
            if (i < vidasActuales)
            {
                listaCorazones[i].SetActive(true);
            }
            else
            {
                listaCorazones[i].SetActive(false);
            }
        }
    }

    private IEnumerator EfectoPantallaRoja()
    {
        pantallaRoja.SetActive(true);
        yield return new WaitForSeconds(duracionFlash);
        pantallaRoja.SetActive(false);
    }

    private IEnumerator IFrames()
    {
        esInvencible = true;
        float tiempoPasado = 0f;
        float intervaloParpadeo = 0.1f; 

        while (tiempoPasado < duracionInvencibilidadHit)
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = (c.a == 1f) ? 0.3f : 1f; 
                spriteRenderer.color = c;
            }

            yield return new WaitForSeconds(intervaloParpadeo);
            tiempoPasado += intervaloParpadeo;
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
        esInvencible = false;
    }

    private void Muerte()
    {
        Debug.Log("¡Game Over!");
        Destroy(gameObject); 
    }
}