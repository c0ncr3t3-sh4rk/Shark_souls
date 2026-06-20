using UnityEngine;
using System.Collections.Generic;
namespace SharkSouls.Dungeon
{
    public enum CapaSala
    {
        Superficie,
        Medio,
        Profundo
    }
    public enum TipoSala
    {
        Start,
        Normal,
        Boss
    }
    public class SalaBase : MonoBehaviour
    {
        [Header("Datos de la Sala")]
        public CapaSala capa = CapaSala.Medio;
        public TipoSala tipo = TipoSala.Normal;
        public string estiloTag = "PorDefecto";

        [Header("Datos de Generación (Editor)")]
        public List<Vector2Int> celdasOcupadas = new List<Vector2Int>();
        public List<ConectorPuerta> conectores = new List<ConectorPuerta>();
        public Vector2Int posGrid;

        [Header("Límites de Cámara")]
        [Tooltip("Collider usado para delimitar hasta dónde puede llegar la cámara en esta sala.")]
        public BoxCollider2D boundsCamara;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                CamaraSala cam = Camera.main.GetComponent<CamaraSala>();
                if (cam != null && boundsCamara != null)
                {
                    cam.FijarSalaActual(this);
                }
            }
        }

        public void ConfigurarPuertasVisuales()
        {
            foreach (var conector in conectores)
            {
                if (conector.visualPuerta != null)
                {
                    conector.visualPuerta.SetActive(conector.estaConectado);
                }
                if (conector.visualPared != null)
                {
                    conector.visualPared.SetActive(!conector.estaConectado);
                }
            }
        }
    }

    [System.Serializable]
    public class ConectorPuerta
    {
        public Vector2Int celdaLocal;
        public Vector2Int direccion;
        public GameObject visualPuerta;
        public GameObject visualPared;
        public bool estaConectado;
    }
}
