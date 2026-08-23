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
                barracudaPadre.DetenerPorImpacto(barracudaPadre.tAturdimiento / 2);
            }

            SaludTiburon salud = collision.GetComponent<SaludTiburon>();
            if (salud != null)
            {
                salud.RecibirDano(1);
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Salas"))
        {
            if (barracudaPadre != null)
            {
                Debug.Log("¡La barracuda se ha estampado contra la pared!");
                barracudaPadre.DetenerPorImpacto(barracudaPadre.tAturdimiento);
            }
        }
    }
}