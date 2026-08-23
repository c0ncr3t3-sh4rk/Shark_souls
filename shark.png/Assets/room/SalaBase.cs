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
        Boss,
        Especial
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

        [Header("Estado de la Sala")]
        public bool salaCompletada = false;

        [Header("Límites de Cámara")]
        [Tooltip("Collider usado para delimitar hasta dónde puede llegar la cámara en esta sala.")]
        public BoxCollider2D boundsCamara;

        [HideInInspector] public bool requiereSpawneo;
        [Header("Generador de Enemigos Local")]
        [Tooltip("El generador de esta sala. Si está vacío, buscará uno en este mismo GameObject.")]
        public GeneradorEnemigos generadorEnemigos;
        
        [HideInInspector] public int dificultadAlCrear = 1;

        private readonly List<GameObject> enemigosVivos = new List<GameObject>();
        public int CantidadEnemigosVivos => enemigosVivos.Count;
        private bool puertasCerradas;
        private bool esperandoEntrada;
        private Transform jugadorTransform;
        private GeneradorMazmorra mazmorraRef;

        private GeneradorMazmorra Mazmorra
        {
            get
            {
                if (mazmorraRef == null)
                    mazmorraRef = FindAnyObjectByType<GeneradorMazmorra>();
                return mazmorraRef;
            }
        }

        private void Awake()
        {
            if (generadorEnemigos == null)
            {
                generadorEnemigos = GetComponent<GeneradorEnemigos>();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Player")) return;

            // Guardamos la referencia del jugador para empezar a comprobar si está dentro
            esperandoEntrada = true;
            jugadorTransform = collision.transform;

            Debug.Log($"[SalaBase] Player tocó el trigger de la sala {gameObject.name}. Esperando entrada segura...");
        }

        private void Update()
        {
            if (!esperandoEntrada || jugadorTransform == null || boundsCamara == null) return;

            Bounds b = boundsCamara.bounds;
            
            // Comprobación manual 2D ignorando el eje Z y aplicando el margen interno de 1.5 unidades
            float margen = 1.5f;
            bool dentroX = jugadorTransform.position.x > (b.min.x + margen) && jugadorTransform.position.x < (b.max.x - margen);
            bool dentroY = jugadorTransform.position.y > (b.min.y + margen) && jugadorTransform.position.y < (b.max.y - margen);

            if (!dentroX || !dentroY) return;

            // --- AHORA SÍ: El jugador está totalmente DENTRO de la sala ---
            
            // 1. Cambiamos la cámara solo cuando el jugador se ha adentrado lo suficiente
            CamaraSala cam = Camera.main.GetComponent<CamaraSala>();
            if (cam != null && boundsCamara != null)
            {
                cam.FijarSalaActual(this);
            }

            esperandoEntrada = false;

            // 2. Ejecutamos el spawn si correspondía
            if (!salaCompletada && (tipo == TipoSala.Normal || tipo == TipoSala.Especial))
            {
                Debug.Log($"[SalaBase] Jugador dentro de zona segura. Ejecutando spawn.");
                EjecutarSpawnYCerrarPuertas();
            }
        }

        private void EjecutarSpawnYCerrarPuertas()
        {
            if (requiereSpawneo && generadorEnemigos != null)
            {
                int completadas = Mazmorra != null ? Mazmorra.salasCompletadas : 0;
                generadorEnemigos.SpawnEnemigos(this, completadas, dificultadAlCrear);
                requiereSpawneo = false;
            }
            else
            {
                // Si no hay generador, cerramos las puertas directamente si hay enemigos de antes (poco probable)
                ChequearPuertasYSpawneo();
            }
        }

        public void ChequearPuertasYSpawneo()
        {
            if (enemigosVivos.Count > 0)
                CerrarPuertas();
        }

        public void RegistrarEnemigo(GameObject enemigo)
        {
            if (!enemigosVivos.Contains(enemigo))
                enemigosVivos.Add(enemigo);
        }

        public void EnemigoEliminado(GameObject enemigo)
        {
            if (enemigosVivos.Remove(enemigo))
            {
                VidaEnemigo vida = enemigo.GetComponent<VidaEnemigo>();
                if (vida != null && generadorEnemigos != null)
                {
                    generadorEnemigos.LiberarPresupuestoSimultaneo(vida.costePresupuesto, vida.prefabOrigen);
                }

                ChequearFinSala();
            }
        }

        public void ChequearFinSala()
        {
            if (salaCompletada) return;

            enemigosVivos.RemoveAll(e => e == null || !e.activeInHierarchy);

            bool spawnTerminado = generadorEnemigos == null || generadorEnemigos.spawnCompletado;

            if (spawnTerminado && enemigosVivos.Count == 0)
            {
                salaCompletada = true;

                if (puertasCerradas)
                {
                    AbrirPuertas();
                }

                if (Mazmorra != null)
                {
                    if (Mazmorra.prefabTragaperras != null)
                    {
                        Instantiate(Mazmorra.prefabTragaperras, transform.position, Quaternion.identity);
                    }
                    Mazmorra.SalaCompletadaCallback();
                }
            }
        }

        private void CerrarPuertas()
        {
            puertasCerradas = true;
            foreach (var conector in conectores)
            {
                if (conector.estaConectado && conector.visualBloqueo != null)
                    conector.visualBloqueo.SetActive(true);
            }
        }

        public void AbrirPuertas()
        {
            puertasCerradas = false;
            foreach (var conector in conectores)
            {
                if (conector.estaConectado && conector.visualBloqueo != null)
                    conector.visualBloqueo.SetActive(false);
            }
        }

        public void ConfigurarPuertasVisuales()
        {
            foreach (var conector in conectores)
            {
                if (conector.visualPuerta != null)
                    conector.visualPuerta.SetActive(conector.estaConectado);

                if (conector.visualPared != null)
                    conector.visualPared.SetActive(!conector.estaConectado);

                if (conector.visualBloqueo != null)
                    conector.visualBloqueo.SetActive(false);
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
        public GameObject visualBloqueo;
        public bool estaConectado;
    }
}
