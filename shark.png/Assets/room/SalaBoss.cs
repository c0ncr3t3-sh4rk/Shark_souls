using UnityEngine;
using SharkSouls.Dungeon;

namespace SharkSouls.Dungeon
{
    public class SalaBossTrigger : MonoBehaviour
    {
        [Header("Referencias de la Sala")]
        public SalaBase salaPadre;                  // Script SalaBase del padre
        public BoxCollider2D boundsOriginales;      // Collider normal de la sala
        private BoxCollider2D boundsBoss;           // Este mismo collider actúa como los límites de la pantalla fija

        private bool combateIniciado = false;
        private CamaraSala camaraSala;
        private BossMako bossActual;

        private void Awake()
        {
            boundsBoss = GetComponent<BoxCollider2D>();

            if (salaPadre == null) // si no esta pos lo busca y tall
            {
                salaPadre = GetComponentInParent<SalaBase>();
            }

            if (salaPadre != null && salaPadre.GetComponent<BossUtils>() == null) // si no tiene pos se lo pone, aqui hay capas de chapuza pero a quien le importa ya
            {
                salaPadre.gameObject.AddComponent<BossUtils>();
            }

            camaraSala = FindFirstObjectByType<CamaraSala>();

            // El Boss debe estar colocado como hijo de la sala.
            bossActual = salaPadre != null
                ? salaPadre.GetComponentInChildren<BossMako>(true)
                : GetComponentInChildren<BossMako>(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (combateIniciado || !other.CompareTag("Player")) return;

            IniciarCombate();
        }

        private void IniciarCombate()
        {
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