using UnityEngine;
using UnityEngine.InputSystem;

public class Proyectil : MonoBehaviour, IParryable
{
    [Header("Configuración del Proyectil")]
    [SerializeField] protected float velocidad = 25f;
    [SerializeField] protected int danoBase = 1;
    [SerializeField] protected bool fueParreado = false;

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void OnParry(GameObject parriedBy, int damage)
    {
        fueParreado = true;

        Vector3 mouseScreenPos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0f;

            Vector2 dirMirado = -parriedBy.transform.right;
            Vector2 dirHaciaRatonDesdeJugador = ((Vector2)mouseWorldPos - (Vector2)parriedBy.transform.position).normalized;
            float dot = Vector2.Dot(dirMirado, dirHaciaRatonDesdeJugador);

            if (dot > 0f)
            {
                Vector2 dirHaciaRatonDesdeProyectil = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
                Lanzar(dirHaciaRatonDesdeProyectil);
            }
            else
            {
                Lanzar(dirMirado);
            }
        }
        else
        {
            Vector2 dirMirado = -parriedBy.transform.right;
            Lanzar(dirMirado);
        }
    }

    public virtual void Lanzar(Vector2 direccion)
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = direccion * velocidad;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Lógica de colisión básica para proyectiles estándar (p.ej. balas)
        if (fueParreado)
        {
            if (collision.GetComponent<SaludTiburon>() != null) return;

            VidaEnemigo enemigo = collision.GetComponent<VidaEnemigo>();
            if (enemigo != null)
            {
                enemigo.RecibirDanoEnemigo(danoBase);
                Destroy(gameObject);
                return;
            }

            if (collision.gameObject.layer == LayerMask.NameToLayer("Paredes"))
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            SaludTiburon jugador = collision.GetComponent<SaludTiburon>();
            if (jugador != null)
            {
                jugador.RecibirDano(danoBase);
                Destroy(gameObject);
            }
        }
    }
}
