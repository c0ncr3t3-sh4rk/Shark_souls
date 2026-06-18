using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 3;
    private int vidaActual;

    [Header("Efectos Visuales")]
    [SerializeField] private GameObject efectoSangrePrefab; // <-- Prefab para el daño
    [SerializeField] private GameObject manchaSangrePrefab; // <-- Prefab para la muerte

    private void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanoEnemigo(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log(gameObject.name + " ha recibido daño. Vida restante: " + vidaActual);

        if (Combo.Instancia != null)
        {
            Combo.Instancia.Refrescar();
        }
        
        // Disparamos la sangre
        Sangre();

        // Efecto cutre de parpadeo rojo al recibir daño
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) StartCoroutine(EfectoDano(sr));

        if (vidaActual <= 0)
        {
            MonaMuerte();
        }
    }

    private void Sangre()
    {
        // Si hay un prefab asignado en el Inspector, lo crea
        if (efectoSangrePrefab != null)
        {
            GameObject sangre = Instantiate(efectoSangrePrefab, transform.position, Quaternion.identity);
            // Destruye el objeto de sangre después de 1 segundo para no saturar el juego
            Destroy(sangre, 1f); 
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
        // Si tenemos configurada la mancha fija de muerte...
        if (manchaSangrePrefab != null)
        {
            Instantiate(manchaSangrePrefab, transform.position, Quaternion.identity);
        }

        if (Combo.Instancia != null)
        {
            Combo.Instancia.RegistrarBaja();
        }
        
        Debug.Log(gameObject.name + " HA MUERTO.");
        Destroy(gameObject); // El pez desaparece
    }
}