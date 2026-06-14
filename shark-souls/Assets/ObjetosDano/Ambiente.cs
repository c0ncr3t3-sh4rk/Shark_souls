using UnityEngine;

public class Ambiente : MonoBehaviour
{
    [SerializeField] private int puntosDeDano = 1; 

    private void OnTriggerEnter2D(Collider2D oponente)
    {
        SaludTiburon salud = oponente.GetComponent<SaludTiburon>();
        
        if (salud != null)
        {
            salud.RecibirDano(puntosDeDano);
        }
    }

    private void OnCollisionEnter2D(Collision2D colision)
    {
        SaludTiburon salud = colision.gameObject.GetComponent<SaludTiburon>();
        
        if (salud != null)
        {
            salud.RecibirDano(puntosDeDano);
        }
    }
}