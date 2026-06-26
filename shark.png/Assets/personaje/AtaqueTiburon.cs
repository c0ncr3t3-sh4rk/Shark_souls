using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AtaqueTiburon : MonoBehaviour
{
    [Header("Sprites de Animación Cutre")]
    [SerializeField] private Sprite spriteNormal; 
    [SerializeField] private Sprite spriteMordisco; 
    [SerializeField] private float duracionMordisco = 0.15f; 

    [Header("Configuración de Daño")]
    [SerializeField] private int danoMordisco = 1;
    [SerializeField] private Collider2D colisionadorBoca; // 👈 Arrastraremos un colisionador aquí

    private SpriteRenderer spriteRenderer; 
    public static bool estaOcupado = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;
        
        // Empezamos con el colisionador de la boca apagado
        if (colisionadorBoca != null) colisionadorBoca.enabled = false;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !estaOcupado)
        {
            StartCoroutine(RutinaMordisco());
        }
    }

    private IEnumerator RutinaMordisco()
    {
        estaOcupado = true;
        
        if (spriteMordisco != null) spriteRenderer.sprite = spriteMordisco;

        // 🔥 ¡ABRIMOS LA BOCA! Activamos el colisionador para que haga daño
        if (colisionadorBoca != null) colisionadorBoca.enabled = true;

        yield return new WaitForSeconds(duracionMordisco);

        // 🛑 ¡CERRAMOS LA BOCA! Apagamos el colisionador
        if (colisionadorBoca != null) colisionadorBoca.enabled = false;

        if (spriteNormal != null) spriteRenderer.sprite = spriteNormal;

        estaOcupado = false;
    }

    // Este método mágico de Unity se activa si la boca toca a un enemigo mientras está activa
private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. FILTRO DE BOTÓN: Si no estás apretando el clic de morder, salimos
        if (!estaOcupado) return;

        // 2. FILTRO DE MORDISCO: Comprobamos si el objeto que ha provocado la colisión
        // tiene el mismo nombre que nuestro objeto de la boca.
        // (A las malas, si el trigger no se llama "Boca", ignoramos el golpe)
        if (colisionadorBoca != null && colisionadorBoca.gameObject.name != "Boca") 
        {
            return; 
        }

        // 3. Si pasa los filtros, buscamos la vida del enemigo
        VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>();
        if (enemigo != null)
        {
            Debug.Log("¡Atacaste con la boca con éxito!");
            enemigo.RecibirDanoEnemigo(danoMordisco);
            
            // Apagamos la boca para que no haga daño múltiple en el mismo frame
            if (colisionadorBoca != null) colisionadorBoca.enabled = false;
        }
    }
}