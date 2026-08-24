using UnityEngine;

public class Hitmarker : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 0.12f; // Súper rápido, estilo arcade

    void Start()
    {
        // Variación aleatoria de tamaño para que no sea estático
        float escalaRandom = Random.Range(0.8f, 1.2f);
        transform.localScale *= escalaRandom;

        // Rotación aleatoria ligera (+- 15 grados) para dar dinamismo
        transform.Rotate(0f, 0f, Random.Range(-15f, 15f));

        // Se destruye solo en un pestañeo
        Destroy(gameObject, tiempoVida);
    }
}