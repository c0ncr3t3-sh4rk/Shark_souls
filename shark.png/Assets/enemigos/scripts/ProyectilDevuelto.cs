using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ProyectilDevuelto : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int danoAEnemigos;
    private bool lanzado = false;

    [SerializeField] private float velocidadGiro = 5400f;

    [Header("Duración del efecto parry")]
    [SerializeField] private float tiempoVidaMaxima = 3f;

    [Header("Imágenes Residuales")]
    [SerializeField] private Color colorFantasma = new Color(0f, 0.8f, 1f, 0.5f);
    [SerializeField] private float tiempoVidaFantasma = 0.4f;
    [SerializeField] private float tiempoEntreFantasmas = 0.02f;

    public void Disparar(float fuerza, int dano)
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        danoAEnemigos = dano;
        lanzado = true;

        Vector3 posicionRatonMundo = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        posicionRatonMundo.z = 0f;
        Vector2 direccionAlRaton = ((Vector2)posicionRatonMundo - (Vector2)transform.position).normalized;

        rb.freezeRotation = false;
        rb.linearVelocity = direccionAlRaton * fuerza;

        StartCoroutine(ImagenesResiduales());

        if (GetComponent<VidaEnemigo>() != null)
        {
            StartCoroutine(TemporizadorVida());
        }
    }

    private void Update()
    {
        if (lanzado)
        {
            transform.Rotate(0f, 0f, velocidadGiro * Time.deltaTime);
        }
    }

    private IEnumerator TemporizadorVida()
    {
        yield return new WaitForSeconds(tiempoVidaMaxima);

        if (lanzado)
        {
            ImpactarYDestruir();
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
        if (collision.gameObject == gameObject) return;

        VidaEnemigo vidaOtroEnemigo = collision.GetComponent<VidaEnemigo>() ?? collision.GetComponentInParent<VidaEnemigo>();
        if (vidaOtroEnemigo != null)
        {
            vidaOtroEnemigo.RecibirDano(danoAEnemigos, VidaEnemigo.TipoMuerte.proyectil);
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
            miVida.RecibirDano(1, VidaEnemigo.TipoMuerte.proyectil);

            if (gameObject != null)
            {
                rb.freezeRotation = true;
                rb.angularVelocity = 0f;

                MonoBehaviour scriptIAParry = GetComponent<IParryable>() as MonoBehaviour;
                IEnemigo scriptIAStun = GetComponent<IEnemigo>();
                if (scriptIAStun != null)
                {
                    scriptIAParry.enabled = true;
                    scriptIAStun.SumarAturdimiento(3f);

                    scriptIAParry.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
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