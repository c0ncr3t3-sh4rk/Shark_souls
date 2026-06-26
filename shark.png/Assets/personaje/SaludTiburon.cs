using UnityEngine;
using System.Collections;
using System.Collections.Generic; // 👈 OBLIGATORIO: Para usar listas de corazones

public class SaludTiburon : MonoBehaviour
{
    [SerializeField] private int vidasMaximas = 3; 
    [SerializeField] private Transform contenedorCorazones; 
    [SerializeField] private GameObject corazonPrefab; 
    [SerializeField] private float duracionInvencibilidad = 1.5f; 
    [SerializeField] private GameObject pantallaRoja; 
    private SpriteRenderer spriteRenderer;
    private int vidasActuales;
    private List<GameObject> listaCorazones = new List<GameObject>();
    private bool esInvencible = false;

    private void Awake()
    {
        vidasActuales = vidasMaximas;
        spriteRenderer = GetComponent<SpriteRenderer>();
        pantallaRoja.SetActive(false);

        CrearCorazones();
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

        ActualizarCorazones();

        if (pantallaRoja != null) StartCoroutine(EfectoPantallaRoja());

        if (vidasActuales <= 0)
        {
            Muerte();
            return; 
        }

        StartCoroutine(IFrames());
    }

    private void CrearCorazones()
    {
        for (int i = 0; i < vidasMaximas; i++)
        {
            GameObject Corazon = Instantiate(corazonPrefab, contenedorCorazones);
            listaCorazones.Add(Corazon);
        }
    }

    private void ActualizarCorazones()
    {
        for (int i = 0; i < listaCorazones.Count; i++)
        {
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
        yield return new WaitForSeconds(0.2f);
        pantallaRoja.SetActive(false);
    }

    private IEnumerator IFrames()
    {
        esInvencible = true;
        float tiempoPasado = 0f;
        float intervaloParpadeo = 0.1f; 

        while (tiempoPasado < duracionInvencibilidad)
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