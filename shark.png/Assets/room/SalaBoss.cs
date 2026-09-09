using UnityEngine;
using SharkSouls.Dungeon;

namespace SharkSouls.Dungeon
{
    public class SalaBoss : MonoBehaviour
    {
        [Header("Referencias de la Sala")]
        public SalaBase salaPadre;                  // Script SalaBase del padre
        public BoxCollider2D boundsOriginales;      // Collider normal de la sala
        private BoxCollider2D boundsBoss;           // Este mismo collider actúa como los límites de la pantalla fija

        private bool combateIniciado = false;
        private CamaraSala camaraSala;
        private IBoss bossActual;

        private void Awake()
        {
            boundsBoss = GetComponent<BoxCollider2D>();

            if (salaPadre == null) // si no esta pos lo busca y tall
            {
                salaPadre = GetComponentInParent<SalaBase>();
            }

            if (salaPadre != null && salaPadre.GetComponent<BossUtils>() == null) // si no tiene pos se lo pone
            {
                salaPadre.gameObject.AddComponent<BossUtils>();
            }

            camaraSala = FindFirstObjectByType<CamaraSala>();

            // Buscar el boss directamente por la interfaz IBoss (funciona con cualquier tipo de boss)
            bossActual = BuscarBoss(salaPadre != null ? salaPadre.transform : transform);

            if (bossActual == null)
            {
                Debug.LogWarning("[SalaBoss] No se encontró ningún boss con la interfaz IBoss en los hijos de la sala.");
            }
        }

        private IBoss BuscarBoss(Transform raiz)
        {
            if (raiz != null)
            {
                IBoss boss = raiz.GetComponentInChildren<IBoss>(true);
                if (boss != null)
                    return boss;
            }

            // Fallback global por si el boss está en la escena fuera de la sala
            MonoBehaviour[] todos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in todos)
            {
                if (m is IBoss b)
                    return b;
            }

            return null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (combateIniciado || !other.CompareTag("Player")) return;

            IniciarCombate();
        }

        private void IniciarCombate()
        {
            if (bossActual == null)
            {
                bossActual = BuscarBoss(salaPadre != null ? salaPadre.transform : transform);
            }

            if (bossActual == null)
            {
                Debug.LogError("[SalaBoss] IniciarCombate llamado pero no se encontró ningún boss con la interfaz IBoss.");
                return;
            }

            combateIniciado = true;

            // Confinamiento (jaja) de Cámara y Cierre de Puertas
            camaraSala.ActivarColliderCamara(true);

            if (boundsOriginales == null)
                boundsOriginales = salaPadre.boundsCamara;

            salaPadre.boundsCamara = boundsBoss;

            salaPadre.CerrarPuertas();

            // Activar el Boss y suscribirnos a su evento de muerte
            bossActual.OnBossMuerto += FinalizarCombate;
            bossActual.IniciarCombate();
        }

        private void FinalizarCombate()
        {
            bossActual.OnBossMuerto -= FinalizarCombate;

            camaraSala.ActivarColliderCamara(false);

            // 2. Restaurar cámara, abrir puertas y soltar la recompensa
            salaPadre.boundsCamara = boundsOriginales;

            salaPadre.salaCompletada = true;
            salaPadre.AbrirPuertas();

            GeneradorMazmorra mazmorra = FindFirstObjectByType<GeneradorMazmorra>();
            if (mazmorra != null)
            {
                Instantiate(mazmorra.prefabTragaperras, salaPadre.transform.position, Quaternion.identity);
                mazmorra.SalaCompletadaCallback();
            }

            this.enabled = false;
        }
    }
}
