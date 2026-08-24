using UnityEngine;
using SharkSouls.Dungeon;

namespace SharkSouls.Dungeon
{
    public class SalaBossTrigger : MonoBehaviour
    {
        [Header("Referencias de la Sala")]
        public SalaBase salaPadre;                   // Script SalaBase del padre
        public BoxCollider2D boundsOriginales;      // Collider normal de la sala (opcional)

        [Header("Configuración del Boss")]
        public GameObject prefabBoss;               // Prefab del Marrajo
        public Transform puntoSpawnBoss;            // Punto de aparición
        [Min(0f)] public float margenActivacion = 1.5f;

        private bool combateIniciado = false;
        private bool bossSpawneado = false;
        private GameObject bossInstanciado;
        private BoxCollider2D miColliderPantallaFija;
        private CamaraSala camaraSala;

        private void Awake()
        {
            miColliderPantallaFija = GetComponent<BoxCollider2D>();

            if (salaPadre == null)
            {
                salaPadre = GetComponentInParent<SalaBase>();
            }

            camaraSala = FindFirstObjectByType<CamaraSala>();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (combateIniciado || !other.CompareTag("Player")) return;

            Bounds limites = miColliderPantallaFija != null
                ? miColliderPantallaFija.bounds
                : GetComponent<Collider2D>().bounds;
            Vector2 centroJugador = other.bounds.center;

            bool dentroX = centroJugador.x > limites.min.x + margenActivacion &&
                           centroJugador.x < limites.max.x - margenActivacion;
            bool dentroY = centroJugador.y > limites.min.y + margenActivacion &&
                           centroJugador.y < limites.max.y - margenActivacion;

            if (dentroX && dentroY)
                IniciarCombate();
        }

        private void IniciarCombate()
        {
            combateIniciado = true;
            Debug.Log("[BossBounds] ¡Jugador dentro! Activando EdgeCollider y congelando cámara.");

            // 1. ACTIVAR EL EDGE COLLIDER DESDE LA CÁMARA
            if (camaraSala != null)
                camaraSala.ActivarColliderCamara(true);
            else
                Debug.LogError("[BossBounds] No se encontró CamaraSala.");

            if (salaPadre != null)
            {
                if (boundsOriginales == null)
                {
                    boundsOriginales = salaPadre.boundsCamara;
                }

                // 2. FIJAR CÁMARA Y CERRAR PUERTAS
                if (miColliderPantallaFija != null)
                {
                    salaPadre.boundsCamara = miColliderPantallaFija;
                }
                salaPadre.CerrarPuertas();
            }

            // 3. SPAWNEAR AL BOSS
            if (prefabBoss != null && puntoSpawnBoss != null)
            {
                bossInstanciado = Instantiate(prefabBoss, puntoSpawnBoss.position, puntoSpawnBoss.rotation);
                bossSpawneado = true;

                if (salaPadre != null)
                    bossInstanciado.transform.SetParent(salaPadre.transform);
            }
            else
            {
                Debug.LogError("[BossBounds] No se puede iniciar el boss: falta prefabBoss o puntoSpawnBoss.");
            }
        }

        private void Update()
        {
            if (!combateIniciado) return;

            // Detección de Muerte del Boss
            if (bossSpawneado && bossInstanciado == null)
            {
                FinalizarCombateYCompletarSala();
            }
        }

        private void FinalizarCombateYCompletarSala()
        {
            Debug.Log("[BossBounds] ¡Boss muerto! Desactivando EdgeCollider y liberando puertas.");

            // 1. DESACTIVAR EL EDGE COLLIDER DESDE LA CÁMARA
            if (camaraSala != null)
                camaraSala.ActivarColliderCamara(false);

            if (salaPadre != null)
            {
                if (boundsOriginales != null)
                {
                    salaPadre.boundsCamara = boundsOriginales;
                }

                salaPadre.salaCompletada = true;
                salaPadre.AbrirPuertas();

                GeneradorMazmorra mazmorra = FindFirstObjectByType<GeneradorMazmorra>();
                if (mazmorra != null)
                {
                    if (mazmorra.prefabTragaperras != null)
                    {
                        Instantiate(mazmorra.prefabTragaperras, salaPadre.transform.position, Quaternion.identity);
                    }
                    mazmorra.SalaCompletadaCallback();
                }
            }

            this.enabled = false;
        }
    }
}