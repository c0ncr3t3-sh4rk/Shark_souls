using UnityEngine;
using System.Collections;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 3;
    private int vidaActual;

    [Header("Visuales")]
    [SerializeField] private GameObject sangre;
    [SerializeField] private GameObject sangreMuerte;
    public SharkSouls.Dungeon.SalaBase salaAsignada;

    private SpriteRenderer sr;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        sr = GetComponent<SpriteRenderer>();
    }

    public void RecibirDano(int cantidad)
    {
        vidaActual -= cantidad;

        if (Combo.Instancia != null)
            Combo.Instancia.Kill();
        
        if (sangre != null)
        {
            GameObject objSangre = Instantiate(sangre, transform.position, Quaternion.identity);
            Destroy(objSangre, 1f); 
        }

        if (sr != null)
            StartCoroutine(EfectoDano());

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private IEnumerator EfectoDano()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    public void Morir()
    {
        if (sangreMuerte != null)
        {
            GameObject objSangre = Instantiate(sangreMuerte, transform.position, Quaternion.identity);
            Destroy(objSangre, 1f); 
        }

        if (Combo.Instancia != null)
            Combo.Instancia.Kill();
        
        if (salaAsignada != null)
        {
            salaAsignada.EnemigoEliminado(gameObject);
        }

        Destroy(gameObject);
    }
}