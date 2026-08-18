using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public bool esInvencible = false;
    private float tiempoI = 0f;

    private void Awake()
    {
        vidasActuales = vidasMaximas;
        spriteRenderer = GetComponent<SpriteRenderer>();
        pantallaRoja.SetActive(false);

        CrearCorazones();
    }

    public void SumarI(float tiempo)
    {
        tiempoI += tiempo;

        if (!esInvencible)
        {
            StartCoroutine(Invencibilidad());
        }
    }

    private IEnumerator Invencibilidad()
    {
        esInvencible = true;

        while (tiempoI > 0f)
        {
            tiempoI -= Time.deltaTime;
            
            yield return null; 
        }

        tiempoI = 0f;
        esInvencible = false;
    }

    /*public IEnumerator SetInvencible(float time)
    {   

        if (time > 0)
        {
            esInvencible = estado;
            time = time - ;
        }
    }*/

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
        Animacion animacion = GetComponent<Animacion>();
        animacion.enabled = false;
        pantallaRoja.SetActive(true);
        Time.timeScale = 0f; 

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;
        animacion.enabled = true;
        pantallaRoja.SetActive(false);
    }

    private IEnumerator IFrames()
    {
        SumarI(duracionInvencibilidad);
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
    }

    private void Muerte()
    {
        Debug.Log("¡Game Over!");
        Destroy(gameObject); 
    }

    public void addDuracionInvencibilidad(float cantidad)
    {
        duracionInvencibilidad += cantidad;
    }

    public void addHealth(int amount)
    {
        vidasActuales += amount;
        if (vidasActuales > vidasMaximas) vidasActuales = vidasMaximas;
        ActualizarCorazones();
    }

    public void addMaxHealth(int amount)
    {
        vidasMaximas += amount;
        vidasActuales += amount;

        for (int i = 0; i < amount; i++)
        {
            GameObject Corazon = Instantiate(corazonPrefab, contenedorCorazones);
            listaCorazones.Add(Corazon);
        }

        ActualizarCorazones();
    }
}