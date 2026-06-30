using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharkSouls.Dungeon
{
    [System.Serializable]
    public struct EnemigoConfig
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float peso;
    }

    public class GeneradorEnemigos : MonoBehaviour
    {
        [Header("Configuración de Enemigos")]
        public List<EnemigoConfig> enemigosDisponibles;

        [Header("Configuración de Dificultad")]
        [Tooltip("Cantidad de enemigos mínima por sala en la sala 1")]
        public int enemigosBase = 2;

        [Tooltip("Cantidad adicional de enemigos por cada sala completada (ej. 0.5 = 1 enemigo más cada 2 salas)")]
        public float incrementoPorSala = 0.5f;

        [Tooltip("Radio mínimo alrededor del jugador donde no pueden aparecer enemigos")]
        public float radioSeguridadJugador = 7f;

        [Header("Configuración de Oleadas")]
        [Tooltip("Máximo de enemigos a spawnear a la vez")]
        public int maxEnemigosPorGrupo = 5;
        [Tooltip("Tiempo de espera entre la aparición de grupos")]
        public float tiempoEntreGrupos = 0.2f;

        private const int MAX_INTENTOS_POSICION = 30;
        private const float MARGEN_BORDES = 2f;

        public void SpawnEnemigos(SalaBase sala, int salasCompletadas, int dificultad)
        {
            if (enemigosDisponibles == null || enemigosDisponibles.Count == 0) return;
            StartCoroutine(RutinaSpawn(sala, salasCompletadas, dificultad));
        }

        private IEnumerator RutinaSpawn(SalaBase sala, int salasCompletadas, int dificultad)
        {
            int cantidadTotal = Mathf.Max(1, Mathf.RoundToInt((enemigosBase + incrementoPorSala * salasCompletadas) * dificultad));

            Bounds bounds = sala.boundsCamara != null
                ? sala.boundsCamara.bounds
                : new Bounds(sala.transform.position, new Vector3(10f, 10f, 1f));

            float radioSqr = radioSeguridadJugador * radioSeguridadJugador;
            int cantidadRestante = cantidadTotal;

            while (cantidadRestante > 0)
            {
                int aSpawnear = Mathf.Min(maxEnemigosPorGrupo, cantidadRestante);
                
                Transform playerT = null;
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerT = player.transform;

                for (int i = 0; i < aSpawnear; i++)
                {
                    GameObject prefab = SeleccionarEnemigoPonderado();
                    if (prefab == null) continue;

                    Vector3 spawnPos = BuscarPosicionValida(bounds, playerT, radioSqr);
                    GameObject enemigoGO = Instantiate(prefab, spawnPos, Quaternion.identity, sala.transform);

                    VidaEnemigo vida = enemigoGO.GetComponent<VidaEnemigo>();
                    if (vida != null)
                        vida.salaAsignada = sala;

                    sala.RegistrarEnemigo(enemigoGO);
                }

                // Cerramos las puertas si es el primer grupo de enemigos
                if (cantidadRestante == cantidadTotal)
                {
                    sala.ChequearPuertasYSpawneo();
                }

                cantidadRestante -= aSpawnear;

                if (cantidadRestante > 0)
                {
                    yield return new WaitForSeconds(tiempoEntreGrupos);
                }
            }
        }

        private Vector3 BuscarPosicionValida(Bounds bounds, Transform playerT, float radioSqr)
        {
            float minX = bounds.min.x + MARGEN_BORDES;
            float maxX = bounds.max.x - MARGEN_BORDES;
            float minY = bounds.min.y + MARGEN_BORDES;
            float maxY = bounds.max.y - MARGEN_BORDES;

            for (int intento = 0; intento < MAX_INTENTOS_POSICION; intento++)
            {
                Vector3 pos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);

                if (playerT == null || (pos - playerT.position).sqrMagnitude >= radioSqr)
                    return pos;
            }

            return new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        }

        private GameObject SeleccionarEnemigoPonderado()
        {
            float pesoTotal = 0f;
            foreach (var config in enemigosDisponibles)
            {
                if (config.prefab != null)
                    pesoTotal += Mathf.Max(0.01f, config.peso);
            }

            if (pesoTotal <= 0f) return null;

            float randomVal = Random.Range(0f, pesoTotal);
            float acumulado = 0f;

            foreach (var config in enemigosDisponibles)
            {
                if (config.prefab == null) continue;
                acumulado += Mathf.Max(0.01f, config.peso);
                if (randomVal <= acumulado)
                    return config.prefab;
            }

            return null;
        }
    }
}
