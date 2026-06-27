using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 3;
    private int vidaActual;

    [Header("Visuales")]
    [SerializeField] private GameObject sangre;
    [SerializeField] private GameObject sangreMuerte;

    private void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log(gameObject.name + " ha recibido daño. Vida restante: " + vidaActual);

        Combo.Instancia.Kill();
        
        Sangre();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        StartCoroutine(EfectoDano(sr));

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Sangre()
    {
        GameObject Sangre = Instantiate(sangre, transform.position, Quaternion.identity);
        Destroy(Sangre, 1f); 
    }

    private System.Collections.IEnumerator EfectoDano(SpriteRenderer sr)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    private void Morir()
    {
        if (sangreMuerte != null)
        {
            Instantiate(sangreMuerte, transform.position, Quaternion.identity);
        }

        Combo.Instancia.Kill();
        
        Debug.Log(gameObject.name + " HA MUERTO.");
        Destroy(gameObject);
    }
}