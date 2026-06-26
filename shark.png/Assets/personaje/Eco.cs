using UnityEngine;

public class EcoDash : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color colorActual;
    private float velocidadDesvanecer;

    public void Ecos(Sprite spriteOriginal, Vector3 posicion, Quaternion rotacion, Vector3 escala, Color colorInicial, float tiempoVida, int sortingOrderOriginal)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        
        sr.sprite = spriteOriginal;
        transform.position = posicion;
        transform.rotation = rotacion;
        transform.localScale = escala;
        
        sr.sortingOrder = sortingOrderOriginal - 1; 

        colorActual = colorInicial;
        sr.color = colorActual;

        velocidadDesvanecer = colorInicial.a / tiempoVida;

        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        colorActual.a -= velocidadDesvanecer * Time.deltaTime;
        if (sr != null)
        {
            sr.color = colorActual;
        }
    }
}