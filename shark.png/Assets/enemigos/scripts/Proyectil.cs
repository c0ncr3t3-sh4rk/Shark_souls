using UnityEngine;

public class Proyectil : MonoBehaviour, IParryable
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
        spriteRenderer.flipX = true; // cagada mia, el sprite estaba al revés y tardo mas abriendo el gimp q haciendo esta mierda
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
            
            int danoInt = (int)dano;
            vida.RecibirDano(danoInt);
            Destroy(gameObject);
        }
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Salas")) != 0) 
        {
            Destroy(gameObject);
        }
    }

    private void GirarSprite()
    {
        // Evitamos calcular si la bala se frena por completo por alguna razón

        // Peña he leido el comentario este de arriba y no se que clase de retrasado lo ha escrito por q no he sido yo
        // y me ha hecho mucha gracia por que claramente se por que 
        // ''evitamos'' calcular si la bala se frena por completo xd
        if (rb.linearVelocity.magnitude < 0.1f) return;

        float anguloZ = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        anguloZ = Mathf.Round(anguloZ / 45f) * 45f;

        Vector3 escala = transform.localScale;
        escala.x = (rb.linearVelocity.x < 0) ? -Mathf.Abs(escala.x) : Mathf.Abs(escala.x);
        transform.localScale = escala;

        if (rb.linearVelocity.x < 0) anguloZ += 180f;

        transform.localEulerAngles = new Vector3(0f, 0f, anguloZ);
    }

    public void OnParry(GameObject parriedBy, int dano)
    {
        StopAllCoroutines();

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, dano + 5);

        Destroy(this); 
    }
    //no se q me dio en este archivo que no hay un solo comentario util, pero bueno en plan si todo es copy paste de otros laos
}
