/*using UnityEngine;

public class Proyectil : MonoBehaviour
{
    private Vector2 direccionMover;
    private float velocidad;
    private float dano;
    private bool inicializado = false;

    public void Inicializar(Vector2 dir, float vel, float dan)
    {
        direccionMover = dir.normalized;
        velocidad = vel;
        inicializado = true;
        dano = dan;

        // Autodestruir el proyectil a los 15 segundos
        Destroy(gameObject, 15f);
    }

    void Update()
    {
        if (!inicializado) return;

        transform.Translate(direccionMover * velocidad * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Aquí manejas si golpea al jugador o choca contra una pared
        if (collision.CompareStringTag("Player")) // Cambia el tag según tu juego
        {
            collision.RecibirDano(dano);
            Destroy(gameObject);
        }
        else if (((1 << collision.gameObject.layer) & LayerMask.GetMask("Paredes")) != 0) // Si choca con pared
        {
            Destroy(gameObject);
        }
    }

    public void OnParry(GameObject parriedBy, int damage)
    {
        StopAllCoroutines();
        this.enabled = false;

        ProyectilDevuelto proyectil = gameObject.GetComponent<ProyectilDevuelto>() ?? gameObject.AddComponent<ProyectilDevuelto>();

        proyectil.Disparar(50f, damage + 5);
    }
}*/