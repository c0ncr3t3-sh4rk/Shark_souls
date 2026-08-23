using UnityEngine;
using SharkSouls.Dungeon;

public class CamaraSala : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform jugador;

    [Header("Configuración de Paneo")]
    public float suavizado = 0.3f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    private SalaBase salaActual;
    private Vector3 velocidadReferencia = Vector3.zero;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
    }

    public void FijarSalaActual(SalaBase nuevaSala)
    {
        salaActual = nuevaSala;
    }

    private void LateUpdate()
    {
        if (jugador == null) return;

        Vector3 posicionDeseada = jugador.position + offset;

        if (salaActual != null && salaActual.boundsCamara != null)
        {
            posicionDeseada = RestringirPosicionDentroDeBounds(posicionDeseada, salaActual.boundsCamara);
        }

        transform.position = Vector3.SmoothDamp(transform.position, posicionDeseada, ref velocidadReferencia, suavizado);
    }


    private Vector3 RestringirPosicionDentroDeBounds(Vector3 posicion, BoxCollider2D bounds)
    {
        if (cam == null) return posicion;

        float altoCamara = cam.orthographicSize;
        float anchoCamara = cam.orthographicSize * cam.aspect;

        Bounds b = bounds.bounds;

        float minX = b.min.x + anchoCamara;
        float maxX = b.max.x - anchoCamara;
        float minY = b.min.y + altoCamara;
        float maxY = b.max.y - altoCamara;

        if (maxX < minX)
        {
            minX = b.center.x;
            maxX = b.center.x;
        }
        if (maxY < minY)
        {
            minY = b.center.y;
            maxY = b.center.y;
        }

        posicion.x = Mathf.Clamp(posicion.x, minX, maxX);
        posicion.y = Mathf.Clamp(posicion.y, minY, maxY);

        return posicion;
    }
}