/*
esta clase es muy random, no voy a mentir hice toda esta logica dentro de bossmako pero claro cuando llegue
a la segunda fase pues como que me parecio mal volver a calcular todo y cambie toda la logica y en plan movidas gente 
le pedi al copilot que me sacara la logica por que me daba demasiada pereza y me quito todos los comentarios pero basicamente
busca los puntos de referencia de la sala con el tag (justo lo he visto abajo cacho cabron lo ha puesto en private con un String lo mato)
y luego segun su posicion al centro de los mismos pues decide las direcciones que cada uno puede tener activas en el segundo metodo
*/

using System.Collections.Generic;
using UnityEngine;

public class PuntoEmbestida
{
    public Transform punto;
    public Vector2[] direcciones;

    public PuntoEmbestida(Transform punto, Vector2[] direcciones)
    {
        this.punto = punto;
        this.direcciones = direcciones;
    }
}

public class BossUtils : MonoBehaviour
{
    [SerializeField] private string tagPuntosReferencia = "PuntosReferenciaMako";
    [SerializeField] private float umbralEsquinaX = 16f;
    [SerializeField] private float umbralEsquinaY = 6f;
    [SerializeField] private float umbralLateral = 9f;

    public PuntoEmbestida[] GetPuntosEmbestida()
    {
        List<Transform> puntos = ObtenerPuntosReferencia();
        if (puntos.Count == 0)
            return new PuntoEmbestida[0];

        Vector3 centroSala = Vector3.zero;
        foreach (Transform punto in puntos)
            centroSala += punto.position;
        centroSala /= puntos.Count;

        PuntoEmbestida[] resultado = new PuntoEmbestida[puntos.Count];
        for (int i = 0; i < puntos.Count; i++)
        {
            resultado[i] = new PuntoEmbestida(
                puntos[i],
                CalcularDirecciones(puntos[i].position, centroSala)
            );
        }

        return resultado;
    }

    private List<Transform> ObtenerPuntosReferencia()
    {
        List<Transform> puntos = new List<Transform>();
        Transform[] descendientes = GetComponentsInChildren<Transform>(true);

        foreach (Transform descendiente in descendientes)
        {
            if (descendiente != transform && descendiente.CompareTag(tagPuntosReferencia))
                puntos.Add(descendiente);
        }
        
        // DESATIVAR LOS PUNTOS EN EL EDITOR :V

        return puntos;
    }

    private Vector2[] CalcularDirecciones(Vector3 posicion, Vector3 centroSala) // yanderev tiembla
    {
        Vector2 diferencia = posicion - centroSala;

        if (diferencia.x < -umbralEsquinaX && diferencia.y > umbralEsquinaY)
            return new[] { new Vector2(1f, -1f).normalized };
        if (diferencia.x > umbralEsquinaX && diferencia.y > umbralEsquinaY)
            return new[] { new Vector2(-1f, -1f).normalized };
        if (diferencia.x < -umbralEsquinaX && diferencia.y < -umbralEsquinaY)
            return new[] { new Vector2(1f, 1f).normalized };
        if (diferencia.x > umbralEsquinaX && diferencia.y < -umbralEsquinaY)
            return new[] { new Vector2(-1f, 1f).normalized };
        if (diferencia.x < -umbralEsquinaX)
            return new[] { Vector2.right };
        if (diferencia.x > umbralEsquinaX)
            return new[] { Vector2.left };
        if (diferencia.x > umbralLateral && diferencia.y > 0f)
            return new[] { Vector2.down, new Vector2(-1f, -1f).normalized };
        if (diferencia.x > umbralLateral && diferencia.y < 0f)
            return new[] { Vector2.up, new Vector2(-1f, 1f).normalized };
        if (diferencia.x < -umbralLateral && diferencia.y > 0f)
            return new[] { Vector2.down, new Vector2(1f, -1f).normalized };
        if (diferencia.x < -umbralLateral && diferencia.y < 0f)
            return new[] { Vector2.up, new Vector2(1f, 1f).normalized };
        if (diferencia.y > 0f)
            return new[]
            {
                Vector2.down,
                new Vector2(1f, -1f).normalized,
                new Vector2(-1f, -1f).normalized
            };

        return new[]
        {
            Vector2.up,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized
        };
    }
}
