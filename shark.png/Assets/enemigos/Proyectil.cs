using UnityEngine;

public class Proyectil : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad;
    private float dano;
    private bool iniciada = false;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;   
        spriteRenderer.flipX = true; // cagada mia, el sprite estaba al revés y tardo mas abriendo el gimp
    }

    public void Disparar(Vector2 dir, float vel, float dan)
    {
        direccion = dir.normalized;
        velocidad = vel;
        iniciada = true;
        dano = dan;

        rb.linearVelocity = direccion * velocidad;

        Destroy(gameObject, 15f);
    }

    void Update()
    {
        if (!iniciada) return;

        GirarSprite();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) 
        {
            SaludTiburon vida = collision.GetComponent<SaludTiburon>();
            
            // Seguridad: Validamos que el componente del jugador exista antes de dañarlo
            if (vida != null)
            {
                int danoInt = (int)dano;
                vida.RecibirDano(danoInt);
            }
            Destroy(gameObject);
        }
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Paredes")) != 0) 
        {
            Destroy(gameObject);
        }
    }

    private void GirarSprite()
    {
        // Evitamos calcular si la bala se frena por completo por alguna razón
        if (rb.linearVelocity.magnitude < 0.1f) return;

        float anguloZ = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (rb.linearVelocity.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (rb.linearVelocity.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    public void OnParry(GameObject parriedBy, float dano)
    {
        StopAllCoroutines();

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        int danoInt = Mathf.CeilToInt(dano);
        proyectil.Disparar(50f, danoInt + 5);

        Destroy(this); 
    }
}
