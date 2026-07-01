using UnityEngine;

public class ColisionAtaqueBarracuda : MonoBehaviour
{
    private Barracuda barracudaPadre;

    private void Awake()
    {
        barracudaPadre = GetComponentInParent<Barracuda>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (barracudaPadre != null)
            {
                barracudaPadre.DetenerPorImpacto();
            }

            SaludTiburon salud = collision.GetComponent<SaludTiburon>();
            if (salud != null)
            {
                salud.RecibirDano(1);
            }
        }
    }
}