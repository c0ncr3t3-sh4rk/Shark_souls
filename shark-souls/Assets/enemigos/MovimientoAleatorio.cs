using UnityEngine;
using System.Collections;

public class MovimientoAleatorio : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float velocidad = 3f;
    [SerializeField] private float tiempoCaminandoMax = 3f;
    [SerializeField] private float tiempoCaminandoMin = 1f;
    [SerializeField] private float tiempoEsperaMax = 2f;
    [SerializeField] private float tiempoEsperaMin = 0.5f;

    private Rigidbody2D rb;
    private Vector2 direccionAleatoria;
    private bool estaMoviendose = false;

    private Vector3 escalaOriginal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 

        escalaOriginal = transform.localScale;
    }

    private void Start()
    {
        StartCoroutine(RutinaMovimiento());
    }

    private void FixedUpdate()
    {
        if (estaMoviendose)
        {
            rb.linearVelocity = direccionAleatoria * velocidad;
            
            // CORREGIDO: Multiplicamos por la escala original para mantener su tamaño reducido
            if (direccionAleatoria.x > 0.1f) 
                transform.localScale = new Vector3(escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
            else if (direccionAleatoria.x < -0.1f) 
                transform.localScale = new Vector3(-escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator RutinaMovimiento()
    {
        while (true)
        {
            float randomX = Random.Range(-1f, 1f);
            float randomY = Random.Range(-1f, 1f);
            direccionAleatoria = new Vector2(randomX, randomY).normalized;

            estaMoviendose = true;
            float tiempoCaminando = Random.Range(tiempoCaminandoMin, tiempoCaminandoMax);
            yield return new WaitForSeconds(tiempoCaminando);

            estaMoviendose = false;
            float tiempoEspera = Random.Range(tiempoEsperaMin, tiempoEsperaMax);
            yield return new WaitForSeconds(tiempoEspera);
        }
    }
}