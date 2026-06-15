using UnityEngine;
using System.Collections;

public class AsfixiaTiburon : MonoBehaviour
{
    [Header("Configuración de Asfixia")]
    [SerializeField] private float tiempoLimiteQuieto = 4f;
    [SerializeField] private float tiempoEntreGolpesConsecutivos = 1.5f;
    [SerializeField] private int danoPorAsfixia = 1; 

    [Header("Efecto Visual de Alerta")]
    [SerializeField] private Color colorAsfixia = new Color(0.2f, 0.5f, 1f, 1f); 
    [SerializeField] private float velocidadParpadeoAlerta = 0.15f;
    [SerializeField] private float velocidadParpadeoCritico = 0.06f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SaludTiburon saludTiburon;

    private float cronometroQuieto = 0f;
    private bool estaParpadeando = false;
    private bool yaRecibioPrimerGolpe = false;
    private Coroutine corrutinaVisual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        saludTiburon = GetComponent<SaludTiburon>();
    }

    private void Update()
    {
        if (rb == null || saludTiburon == null) return;

        if (Mathf.Abs(rb.linearVelocity.x) < 0.05f && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
        {
            cronometroQuieto += Time.deltaTime;

            float limiteActual = yaRecibioPrimerGolpe ? tiempoEntreGolpesConsecutivos : tiempoLimiteQuieto;

            if (cronometroQuieto >= (limiteActual * 0.5f) && !estaParpadeando)
            {
                estaParpadeando = true;
                corrutinaVisual = StartCoroutine(EfectoVisualAsfixia());
            }

            if (cronometroQuieto >= limiteActual)
            {
                saludTiburon.RecibirDano(danoPorAsfixia);
                Debug.Log("¡El tiburón sufre por falta de movimiento!");
                
                yaRecibioPrimerGolpe = true; 
                
                cronometroQuieto = 0f; 

                ActualizarFrecuenciaVisual();
            }
        }
        else
        {
            cronometroQuieto = 0f;
            yaRecibioPrimerGolpe = false; 
            
            if (estaParpadeando)
            {
                DetenerEfectoVisual();
            }
        }
    }

    private IEnumerator EfectoVisualAsfixia()
    {
        while (estaParpadeando)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (spriteRenderer.color == Color.white) ? colorAsfixia : Color.white;
            }

            float ritmoActual = yaRecibioPrimerGolpe ? velocidadParpadeoCritico : velocidadParpadeoAlerta;
            yield return new WaitForSeconds(ritmoActual);
        }
    }

    private void ActualizarFrecuenciaVisual()
    {
        if (corrutinaVisual != null) StopCoroutine(corrutinaVisual);
        if (estaParpadeando)
        {
            corrutinaVisual = StartCoroutine(EfectoVisualAsfixia());
        }
    }

    private void DetenerEfectoVisual()
    {
        estaParpadeando = false;
        if (corrutinaVisual != null)
        {
            StopCoroutine(corrutinaVisual);
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; 
        }
    }
}