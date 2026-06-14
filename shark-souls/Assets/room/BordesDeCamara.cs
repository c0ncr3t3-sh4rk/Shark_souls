using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BordesDeCamara : MonoBehaviour
{
    private Camera camara;
    private BoxCollider2D colisionArriba;
    private BoxCollider2D colisionAbajo;
    private BoxCollider2D colisionIzquierda;
    private BoxCollider2D colisionDerecha;

    [Header("Grosor de las paredes invisibles")]
    [SerializeField] private float grosorPared = 1f;

    private void Awake()
    {
        camara = GetComponent<Camera>();
        CrearParedes();
    }

    private void Start()
    {
        ActualizarPosicionParedes();
    }

    private void CrearParedes()
    {
        // Creamos objetos vacíos hijos de la cámara para albergar los colliders
        colisionArriba = GenerarObjetoPared("Pared_Arriba");
        colisionAbajo = GenerarObjetoPared("Pared_Abajo");
        colisionIzquierda = GenerarObjetoPared("Pared_Izquierda");
        colisionDerecha = GenerarObjetoPared("Pared_Derecha");
    }

    private BoxCollider2D GenerarObjetoPared(string nombre)
    {
        GameObject objetoPared = new GameObject(nombre);
        objetoPared.transform.SetParent(transform); // Lo hacemos hijo de la cámara
        
        BoxCollider2D col = objetoPared.AddComponent<BoxCollider2D>();
        // Aseguramos que NO sea trigger para que actúe como una pared sólida real
        col.isTrigger = false; 
        
        objetoPared.layer = LayerMask.NameToLayer("Paredes");

        return col;
    }

    private void ActualizarPosicionParedes()
    {
        if (camara == null) return;

        // Calculamos los límites de la pantalla en coordenadas del mundo 2D
        float alturaCamara = camara.orthographicSize * 2f;
        float anchoCamara = alturaCamara * camara.aspect;

        Vector2 centroCamara = (Vector2)transform.position;

        // 1. Pared Superior
        colisionArriba.size = new Vector2(anchoCamara, grosorPared);
        colisionArriba.transform.position = centroCamara + new Vector2(0f, (alturaCamara / 2f) + (grosorPared / 2f));

        // 2. Pared Inferior
        colisionAbajo.size = new Vector2(anchoCamara, grosorPared);
        colisionAbajo.transform.position = centroCamara - new Vector2(0f, (alturaCamara / 2f) + (grosorPared / 2f));

        // 3. Pared Izquierda
        colisionIzquierda.size = new Vector2(grosorPared, alturaCamara);
        colisionIzquierda.transform.position = centroCamara - new Vector2((anchoCamara / 2f) + (grosorPared / 2f), 0f);

        // 4. Pared Derecha
        colisionDerecha.size = new Vector2(grosorPared, alturaCamara);
        colisionDerecha.transform.position = centroCamara + new Vector2((anchoCamara / 2f) + (grosorPared / 2f), 0f);
    }
}