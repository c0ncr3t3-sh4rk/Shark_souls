using UnityEngine;
using System.Collections;

public class ProyectilDevuelto : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int danoAEnemigos;
    private bool lanzado = false;

    [SerializeField] private float velocidadGiro = 5400f;

    [Header("Imágenes Residuales")]
    [SerializeField] private Color colorFantasma = new Color(0f, 0.8f, 1f, 0.5f);
    [SerializeField] private float tiempoVidaFantasma = 0.4f;
    [SerializeField] private float tiempoEntreFantasmas = 0.02f;

    // Cambiamos el método para que calcule la dirección internamente
    public void Disparar(float fuerza, int dano)
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        danoAEnemigos = dano;
        lanzado = true;

        Vector3 posicionRatonMundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        posicionRatonMundo.z = 0f;
        Vector2 direccionAlRaton = ((Vector2)posicionRatonMundo - (Vector2)transform.position).normalized;

        rb.freezeRotation = false;
        rb.linearVelocity = direccionAlRaton * fuerza;

        StartCoroutine(ImagenesResiduales());
    }

    private void Update()
    {
        if (lanzado)
        {
            transform.Rotate(0f, 0f, velocidadGiro * Time.deltaTime);
        }
    }

    private IEnumerator ImagenesResiduales()
    {
        while (lanzado && spriteRenderer != null)
        {
            GameObject fantasma = new GameObject("EcoProyectil_Clon");
            EcoDash componenteEco = fantasma.AddComponent<EcoDash>();

            componenteEco.Ecos(
                spriteRenderer.sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                colorFantasma,
                tiempoVidaFantasma,
                spriteRenderer.sortingOrder - 1
            );

            yield return new WaitForSeconds(tiempoEntreFantasmas);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!lanzado) return;
        if (collision.CompareTag("Player")) return;

        VidaEnemigo vidaOtroEnemigo = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();
        if (vidaOtroEnemigo != null)
        {
            vidaOtroEnemigo.RecibirDano(danoAEnemigos);
            ImpactarYDestruir();
            return;
        }

        if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Salas")) != 0)
        {
            ImpactarYDestruir();
        }
    }

    private void ImpactarYDestruir()
    {
        lanzado = false; 
        StopAllCoroutines();

        VidaEnemigo miVida = GetComponent<VidaEnemigo>();
        if (miVida != null)
        {
            miVida.RecibirDano(1);

            // Si el objeto sigue existiendo aquí, significa que sobrevivió al impacto:
            if (gameObject != null)
            {
                rb.freezeRotation = true;
                rb.angularVelocity = 0f;

                // Buscamos cualquier MonoBehaviour que implemente la interfaz IParryable (la carpa, barracuda, etc.)
                MonoBehaviour scriptIA = GetComponent<IParryable>() as MonoBehaviour;
                if (scriptIA != null)
                {
                    scriptIA.enabled = true;

                    scriptIA.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                }

                Destroy(this);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}