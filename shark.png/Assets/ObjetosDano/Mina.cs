using UnityEngine;
using System.Collections;

public class MinaSubmarina : MonoBehaviour, IParryable
{
    [Header("Configuración de la Mina")]
    [SerializeField] private float tiempoAntesDeExplotar = 2f; 
    [SerializeField] private float radioDeExplosion = 2.5f; 
    [SerializeField] private int danoAObjetivos = 3; 

    [Header("Efecto de Parpadeo Rojo")]
    [SerializeField] private float velocidadParpadeoInicial = 0.3f; 
    [SerializeField] private Color colorParpadeoAlerta = Color.red;

    [Header("Gráficos de la Explosión")]
    [SerializeField] private Sprite spriteExplosion; 

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool yaSeActivo = false;
    private bool haExplotado = false;
    private bool fueParreada = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (yaSeActivo || haExplotado || fueParreada) return;

        // Si cualquier cosa con vida (Jugador o Enemigo) pasa cerca, la mina se activa
        if (collision.GetComponent<SaludTiburon>() != null || collision.GetComponent<VidaEnemigo>() != null)
        {
            ActivarMina();
        }
    }

    public void ActivarMina()
    {
        if (!yaSeActivo)
        {
            yaSeActivo = true;
            StartCoroutine(SecuenciaActivacionMina());
        }
    }

    private IEnumerator SecuenciaActivacionMina()
    {
        float tiempoPasado = 0f;

        while (tiempoPasado < tiempoAntesDeExplotar)
        {
            float progreso = tiempoPasado / tiempoAntesDeExplotar;
            float intervaloActual = Mathf.Lerp(velocidadParpadeoInicial, (velocidadParpadeoInicial / 5), progreso);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = (spriteRenderer.color == Color.white) ? colorParpadeoAlerta : Color.white;
            }

            yield return new WaitForSeconds(intervaloActual);
            tiempoPasado += intervaloActual;
        }

        DetonarEnArea();
    }

    // --- INTEGRACIÓN CON TU SISTEMA DE PARRY ---
    public void OnParry(GameObject parriedBy, int damage)
    {
        if (haExplotado) return;

        StopAllCoroutines(); // Detiene el parpadeo y la cuenta atrás para explotar
        if (spriteRenderer != null) spriteRenderer.color = Color.white; // Resetea el color

        fueParreada = true;
        yaSeActivo = false;

        // Le inyectamos tu script estrella reutilizable
        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        
        // Saldrá disparada hacia el ratón girando y dejando imágenes residuales
        proyectil.Disparar(40f, damage + 2);

        // Desactivamos este script de la mina para que el viaje lo controle ProyectilDevuelto
        this.enabled = false;
    }

    // Este método lo llamará el ProyectilDevuelto automáticamente si choca contra una pared o enemigo tras el parry
    private void ImpactarYRestaurar()
    {
        DetonarEnArea();
    }

    private void DetonarEnArea()
    {
        if (haExplotado) return;
        haExplotado = true;

        StopAllCoroutines();

        // Desactivar físicas y colisionadores propios de la mina
        foreach (var col in GetComponents<Collider2D>()) { if (col != null) col.enabled = false; }
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }

        // Cambiar visual al sprite de explosión escalado al radio real
        if (spriteExplosion != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteExplosion;
            float tamanoSpriteOriginal = spriteRenderer.sprite.bounds.size.x;
            float escalaRequerida = (radioDeExplosion * 2f) / tamanoSpriteOriginal;
            transform.localScale = new Vector3(escalaRequerida, escalaRequerida, 1f);
        }

        // Buscar a quién dañar en el radio circular
        Collider2D[] objetosAlcanzados = Physics2D.OverlapCircleAll(transform.position, radioDeExplosion);
        foreach (Collider2D col in objetosAlcanzados)
        {
            if (col.gameObject == gameObject) continue;

            // Daño al Jugador (Solo si la mina explotó sola. Si fue parreada, el jugador es inmune a su explosión)
            SaludTiburon vidaJugador = col.GetComponent<SaludTiburon>();
            if (vidaJugador != null && !fueParreada)
            {
                vidaJugador.RecibirDano(1); // Daño fijo al jugador
                continue;
            }

            // Daño a Enemigos (Afecta tanto a otros enemigos como a otras minas vecinas si tienen VidaEnemigo)
            VidaEnemigo vidaEnemigo = col.GetComponent<VidaEnemigo>() ?? col.GetComponentInParent<VidaEnemigo>();
            if (vidaEnemigo != null)
            {
                vidaEnemigo.RecibirDano(danoAObjetivos, VidaEnemigo.TipoMuerte.Normal);
            }
        }

        Destroy(gameObject, 0.25f); // Tiempo para que se vea el sprite de la explosión antes de borrarse
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioDeExplosion);
    }
}