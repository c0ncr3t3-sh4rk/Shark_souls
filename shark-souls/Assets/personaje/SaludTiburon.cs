using UnityEngine;
using System.Collections;

public class SaludTiburon : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private int vidasMaximas = 3;
    private int vidasActuales;

    [Header("Efecto de Daño (UI)")]
    // Arrastra aquí el objeto 'FlashRojo' desde el inspector
    [SerializeField] private GameObject pantallaRoja; 
    [SerializeField] private float duracionFlash = 0.2f;

    private void Awake()
    {
        // Al empezar el juego, el tiburón inicia con la vida al máximo
        vidasActuales = vidasMaximas;
        
        // Nos aseguramos de que la pantalla roja empiece apagada
        if (pantallaRoja != null)
        {
            pantallaRoja.SetActive(false);
        }
    }

    // Este método nativo de Unity detecta cuando algo entra en nuestro "Is Trigger"
    // 1. PARA OBJETOS FANTASMAS (Como el Círculo con 'Is Trigger' activado)
    private void OnTriggerEnter2D(Collider2D oponente)
    {
        // Si el objeto contiene "Circle" o "Spike" en el nombre
        if (oponente.gameObject.name.Contains("Circle") || oponente.gameObject.name.Contains("Spike"))
        {
            RecibirDano();
        }
    }

    // 2. PARA OBJETOS SÓLIDOS (Como el Triángulo con 'Is Trigger' desactivado)
    private void OnCollisionEnter2D(Collision2D colision)
    {
        // Comprobamos si el objeto sólido que chocamos contiene "Spike" o "Triangulo" en su nombre
        if (colision.gameObject.name.Contains("Spike"))
        {
            RecibirDano();
        }
    }

    private void RecibirDano()
    {
        vidasActuales--;
        Debug.Log("¡El tiburón fue golpeado! Vidas restantes: " + vidasActuales);

        // Activamos el efecto visual en paralelo usando una Corrutina
        if (pantallaRoja != null)
        {
            StartCoroutine(EfectoPantallaRoja());
        }

        // Si te quedas sin vidas
        if (vidasActuales <= 0)
        {
            Muerte();
        }
    }

    // Proceso asíncrono temporal para encender y apagar el flash rojo
    private IEnumerator EfectoPantallaRoja()
    {
        pantallaRoja.SetActive(true); // Enciende el color rojo
        yield return new WaitForSeconds(duracionFlash); // Espera el tiempo configurado
        pantallaRoja.SetActive(false); // Apaga el color rojo
    }

    private void Muerte()
    {
        Debug.Log("¡Game Over! El tiburón ha muerto.");
        // Aquí puedes destruir el objeto, reiniciar el nivel o congelar el juego
        Destroy(gameObject); 
    }
}