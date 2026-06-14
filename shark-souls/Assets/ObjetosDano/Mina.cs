using UnityEngine;
using System.Collections;

public class MinaSubmarina : MonoBehaviour
{
    [Header("Configuración de la Mina")]
    [SerializeField] private float tiempoAntesDeExplotar = 2f; 
    [SerializeField] private float radioDeExplosion = 2.5f; 

    [Header("Valores de Daño")]
    [SerializeField] private int danoAlJugador = 1; 
    [SerializeField] private int danoAlEnemigo = 3; 

    [Header("Efecto Visual de Parpadeo Rojo")]
    [SerializeField] private float velocidadParpadeoInicial = 0.3f; 
    [SerializeField] private float velocidadParpadeoFinal = 0.06f; 

    [Header("Gráficos de la Explosión")]
    [SerializeField] private Sprite spriteExplosion; 
    [SerializeField] private Color colorParpadeoAlerta = Color.red;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D[] todosLosColliders;
    private bool yaSeActivo = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Obtenemos todos los colliders que tenga la mina (el sólido y el trigger)
        todosLosColliders = GetComponents<Collider2D>();
    }

    // Se activa cuando el "Trigger" de proximidad detecta al jugador o enemigo
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (yaSeActivo) return;

        bool esJugador = collision.GetComponent<SaludTiburon>() != null;
        bool esEnemigo = collision.GetComponent<VidaEnemigo>() != null; 

        if (esJugador || esEnemigo)
        {
            StartCoroutine(SecuenciaActivacionMina());
        }
    }

    private IEnumerator SecuenciaActivacionMina()
    {
        yaSeActivo = true;
        float tiempoPasado = 0f;

        // La mina parpadea MIENTRAS se mueve libremente por el escenario debido al empujón
        while (tiempoPasado < tiempoAntesDeExplotar)
        {
            float progreso = tiempoPasado / tiempoAntesDeExplotar;
            float intervaloActual = Mathf.Lerp(velocidadParpadeoInicial, velocidadParpadeoFinal, progreso);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = (spriteRenderer.color == Color.white) ? colorParpadeoAlerta : Color.white;
            }

            yield return new WaitForSeconds(intervaloActual);
            tiempoPasado += intervaloActual;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        StartCoroutine(SecuenciaExplosionVisual());
    }

    private IEnumerator SecuenciaExplosionVisual()
    {
        // 1. Apagamos todos los colliders para que no choque con nada DURANTE la explosión
        foreach (var col in todosLosColliders)
        {
            if (col != null) col.enabled = false;
        }

        // 2. Frenamos por completo el Rigidbody para que la explosión ocurra fija en ese punto final
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Evita que fuerzas externas la sigan moviendo
        }

        // 3. Cambiamos el sprite y lo escalamos al radio de daño
        if (spriteExplosion != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteExplosion;

            float tamanoSpriteOriginal = spriteRenderer.sprite.bounds.size.x;
            float escalaRequerida = (radioDeExplosion * 2f) / tamanoSpriteOriginal;
            transform.localScale = new Vector3(escalaRequerida, escalaRequerida, 1f);
        }

        // 4. Aplicamos el daño real en área
        AplicarDanoExplosion();

        yield return new WaitForSeconds(0.25f);

        Destroy(gameObject);
    }

    private void AplicarDanoExplosion()
    {
        Collider2D[] objetosAlcanzados = Physics2D.OverlapCircleAll(transform.position, radioDeExplosion);

        foreach (Collider2D col in objetosAlcanzados)
        {
            if (col.gameObject == gameObject) continue;

            // Daño al Jugador
            SaludTiburon vidaJugador = col.GetComponent<SaludTiburon>();
            if (vidaJugador != null)
            {
                vidaJugador.RecibirDano(danoAlJugador);
                continue; 
            }

            // Daño a los Enemigos
            VidaEnemigo vidaEnemigo = col.GetComponent<VidaEnemigo>();
            if (vidaEnemigo != null)
            {
                vidaEnemigo.RecibirDanoEnemigo(danoAlEnemigo);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, radioDeExplosion);
    }
}