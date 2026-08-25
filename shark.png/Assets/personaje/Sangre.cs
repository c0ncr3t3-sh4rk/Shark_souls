using UnityEngine;

public class Sangre : MonoBehaviour
{
    [SerializeField] private float tiempoDeVida = 1f; // La duración exacta de tu animación

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }
}