using UnityEngine;

/// <summary>
/// Proyectil disparado por las CarpitaMinion.
/// Se mueve en línea recta. Es parryable por el jugador.
/// Al impactar al jugador, causa 1 de daño.
/// Se destruye al tocar paredes o tras 10 segundos.
/// </summary>
public class ProyectilCarpa : MonoBehaviour, IParryable
{
    [SerializeField] private float velocidad = 8f;
    [SerializeField] private int dano = 1;
    [SerializeField] private float tiempoVida = 10f;
    [SerializeField] private LayerMask capaParedes;

    private Vector2 direccion;
    private bool inicializado = false;

    public void Inicializar(Vector2 dir)
    {
        direccion = dir.normalized;
        inicializado = true;

        // Rotar el sprite hacia la dirección de viaje
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        if (!inicializado) return;
        transform.Translate(direccion * velocidad * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!inicializado) return;

        if (collision.CompareTag("Player"))
        {
            SaludTiburon salud = collision.GetComponent<SaludTiburon>();
            if (salud != null)
            {
                salud.RecibirDano(dano);
            }
            Destroy(gameObject);
        }
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Salas")) != 0)
        {
            Destroy(gameObject);
        }
    }

    public void OnParry(GameObject parriedBy, int damage)
    {
        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>()
                                   ?? gameObject.AddComponent<ProyectilDevuelto>();
        proyectil.Disparar(50f, damage + 3);
    }
}
