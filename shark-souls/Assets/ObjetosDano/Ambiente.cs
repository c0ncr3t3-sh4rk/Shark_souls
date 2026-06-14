using UnityEngine;

public class Ambiente : MonoBehaviour
{
    [SerializeField] private int puntosDeDano = 1; 

    private void OnTriggerEnter2D(Collider2D oponente)
    {
        if (oponente.isTrigger || !(oponente is PolygonCollider2D)) return;
        SaludTiburon salud = oponente.GetComponent<SaludTiburon>();
        
        if (salud != null && oponente is PolygonCollider2D)
        {
            salud.RecibirDano(puntosDeDano);
        }
    }

    private void OnCollisionEnter2D(Collision2D colision)
    {
        // 1. FILTRO DE SEGURIDAD: Accedemos a '.collider' para comprobar los datos.
        // Si el colisionador del oponente es un trigger o NO es el PolygonCollider2D, nos salimos ya.
        if (colision.collider.isTrigger || !(colision.collider is PolygonCollider2D)) return;

        // 2. Si pasa la barrera, ya sabemos al 100% que hemos chocado físicamente contra el cuerpo del tiburón.
        SaludTiburon salud = colision.gameObject.GetComponent<SaludTiburon>();
        
        if (salud != null)
        {
            salud.RecibirDano(puntosDeDano);
        }
    }
}