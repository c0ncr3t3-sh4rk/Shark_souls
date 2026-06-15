using UnityEngine;
using System.Collections;

public class MinaSubmarina : MonoBehaviour
{
    [Header("Configuración de la Mina")]
    [SerializeField] private float tiempoAntesDeExplotar = 2f; 
    [SerializeField] private float radioDeExplosion = 2.5f; 
    [SerializeField] private float retrasoReaccionEnCadena = 0.15f;

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
    private bool detonacionInminente = false; 

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        todosLosColliders = GetComponents<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        MinaSubmarina otraMina = collision.gameObject.GetComponent<MinaSubmarina>();
        if (otraMina != null)
        {
            ActivarMina();
            otraMina.ActivarMina();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (yaSeActivo) return;

        bool esJugador = collision.GetComponent<SaludTiburon>() != null;
        bool esEnemigo = collision.GetComponent<VidaEnemigo>() != null; 

        if (esJugador || esEnemigo)
        {
            ActivarMina();
        }
    }

    public void ActivarMina(bool porOndaExpansiva = false)
    {
        if (detonacionInminente) return;

        if (porOndaExpansiva)
        {
            detonacionInminente = true;
            yaSeActivo = true;
            
            StopAllCoroutines(); 
            StartCoroutine(SecuenciaRetrasoCadena());
            return;
        }

        if (!yaSeActivo)
        {
            yaSeActivo = true;
            StartCoroutine(SecuenciaActivacionMina());
        }
    }

    private IEnumerator SecuenciaRetrasoCadena()
    {
        if (spriteRenderer != null) spriteRenderer.color = colorParpadeoAlerta;

        yield return new WaitForSeconds(retrasoReaccionEnCadena);

        StartCoroutine(SecuenciaExplosionVisual());
    }

    private IEnumerator SecuenciaActivacionMina()
    {
        float tiempoPasado = 0f;

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
        foreach (var col in todosLosColliders)
        {
            if (col != null) col.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        if (spriteExplosion != null && spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; 
            spriteRenderer.sprite = spriteExplosion;

            float tamanoSpriteOriginal = spriteRenderer.sprite.bounds.size.x;
            float escalaRequerida = (radioDeExplosion * 2f) / tamanoSpriteOriginal;
            transform.localScale = new Vector3(escalaRequerida, escalaRequerida, 1f);
        }

        AplicarDanoYBuscarMinas();

        yield return new WaitForSeconds(0.25f);

        Destroy(gameObject);
    }

    private void AplicarDanoYBuscarMinas()
    {
        Debug.Log($"¡BOOM! {gameObject.name} detonó.");

        Collider2D[] objetosAlcanzados = Physics2D.OverlapCircleAll(transform.position, radioDeExplosion);

        foreach (Collider2D col in objetosAlcanzados)
        {
            if (col.gameObject == gameObject) continue;

            MinaSubmarina minaVecina = col.GetComponent<MinaSubmarina>();
            if (minaVecina != null)
            {
                minaVecina.ActivarMina(true);
                continue;
            }

            SaludTiburon vidaJugador = col.GetComponent<SaludTiburon>();
            if (vidaJugador != null)
            {
                vidaJugador.RecibirDano(danoAlJugador);
                continue; 
            }

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