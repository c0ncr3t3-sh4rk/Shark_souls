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

        //Sangre();
        // Efecto cutre de parpadeo rojo al recibir daño
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) StartCoroutine(EfectoDano(sr));

        if (vidaActual <= 0)
        {
            MonaMuerte();
        }
    }

    /*private void Sangre()
    {
        if (efectoSangrePrefab != null)
        {
            GameObject sangre = Instantiate(efectoSangrePrefab, transform.position, transform.rotation);
            Destroy(sangre, 2f); 
        }
    }*/

    private System.Collections.IEnumerator EfectoDano(SpriteRenderer sr)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    [Header("Efectos de Muerte")]
    [SerializeField] private GameObject manchaSangrePrefab; // <--- Ponemos la casilla para la mancha fija

private void MonaMuerte()
{
    // Si tenemos configurada la mancha fija de muerte...
    if (manchaSangrePrefab != null)
    {
        // La instanciamos en el sitio exacto donde ha muerto el pez
        // Usamos Quaternion.identity para que no herede rotaciones raras del pez y se quede recta
        Instantiate(manchaSangrePrefab, transform.position, Quaternion.identity);
    }

    Debug.Log(gameObject.name + " HA MUERTO.");
    Destroy(gameObject); // El pez desaparece, pero la mancha se queda flotando independiente
}
}