using UnityEngine;

public class ObjDaño : MonoBehaviour
{
    public float dano = 1f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo nos interesa si entra el Jugador
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent(out SaludTiburon Vida))
            {
                Vida.RecibirDano(Mathf.RoundToInt(dano));
            }
        }
    }
}