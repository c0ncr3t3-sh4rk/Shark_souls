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
            StartCoroutine(AvisarImpacto(collision.gameObject));
        }
    }

    private System.Collections.IEnumerator AvisarImpacto(GameObject jugador)
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if (barracudaPadre != null)
        {
            barracudaPadre.DetenerPorImpacto();
        }

        SaludTiburon salud = jugador.GetComponent<SaludTiburon>();
        if (salud != null)
        {
            salud.RecibirDano(1);
        }
    }
}