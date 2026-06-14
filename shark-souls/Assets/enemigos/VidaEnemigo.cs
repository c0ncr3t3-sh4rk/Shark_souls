using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 3;
    private int vidaActual;

    private void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanoEnemigo(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log(gameObject.name + " ha recibido daño. Vida restante: " + vidaActual);

        // Efecto cutre de parpadeo rojo al recibir daño
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) StartCoroutine(EfectoDano(sr));

        if (vidaActual <= 0)
        {
            MonaMuerte();
        }
    }

    private System.Collections.IEnumerator EfectoDano(SpriteRenderer sr)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    private void MonaMuerte()
    {
        // Aquí podréis meter partículas de sangre de mentira más adelante
        Debug.Log(gameObject.name + " HA MUERTO.");
        Destroy(gameObject);
    }
}