using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharkSouls.Utils;

namespace SharkSouls.Dungeon
{
    [System.Serializable]
    public struct EnemigoConfig
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float peso;
        public int coste;
        [Tooltip("Máximo de este enemigo por sala (0 = sin límite)")]
        public int maximoPorSala;
    }

    public class GeneradorEnemigos : MonoBehaviour
    {
        [Header("Configuración de Enemigos")]
        public List<EnemigoConfig> enemigosDisponibles;

        [Header("Sistema de Presupuesto")]
        [Tooltip("Presupuesto base de puntos para la primera sala")]
        public int presupuestoBase = 100;
        
        [Tooltip("Puntos adicionales añadidos al presupuesto total por sala completada")]
        public int incrementoPresupuestoPorSala = 20;

        [Tooltip("Puntos máximos que pueden existir al mismo tiempo (Límite FPS)")]
        public int presupuestoSimultaneoMaximo = 50;

        [Tooltip("Cuando la cantidad de enemigos vivos en la sala baje de este valor (inclusive), se activan los refuerzos")]
        public int umbralRefuerzos = 20;

        [Header("Configuración de Oleadas")]
        [Tooltip("Máximo de enemigos a spawnear a la vez")]
        public int maxEnemigosPorGrupo = 5;
        [Tooltip("Tiempo de espera entre la aparición de grupos (Oleada Principal)")]
        public float tiempoEntreGrupos = 0.2f;
        [Tooltip("Tiempo de espera antes de spawnear una mini-oleada de refuerzos")]
        public float delayRefuerzos = 2f;

        [Header("Configuración de Entorno")]
        [Tooltip("Capa que los enemigos no pueden pisar al aparecer (ej. Paredes)")]
        public LayerMask capaObstaculos;
        [Tooltip("Radio mínimo alrededor del jugador donde no pueden aparecer enemigos")]
        public float radioSeguridadJugador = 7f;

        private const int MAX_INTENTOS_POSICION = 30;
        private const float MARGEN_BORDES = 2f;

        [HideInInspector] public bool spawnCompletado = false;
        
        private int presupuestoTotalRestante;
        public int presupuestoSimultaneoActual { get; private set; }
        
        private Dictionary<GameObject, int> conteoSimultaneoPorTipo = new Dictionary<GameObject, int>();
        private SalaBase salaAsignada;
        private Coroutine rutinaActual;

        public void SpawnEnemigos(SalaBase sala, int salasCompletadas, int dificultad)
        {
            if (enemigosDisponibles == null || enemigosDisponibles.Count == 0)
            {
                spawnCompletado = true;
                return;
            }

            salaAsignada = sala;
            spawnCompletado = false;
            presupuestoSimultaneoActual = 0;
            conteoSimultaneoPorTipo.Clear();
            
            presupuestoTotalRestante = Mathf.RoundToInt((presupuestoBase + (incrementoPresupuestoPorSala * salasCompletadas)) * dificultad);
            
            if (rutinaActual != null) StopCoroutine(rutinaActual);
            rutinaActual = StartCoroutine(RutinaSpawn());
        }

        private IEnumerator RutinaSpawn()
        {
            Bounds bounds = salaAsignada.boundsCamara != null
                ? salaAsignada.boundsCamara.bounds
                : new Bounds(salaAsignada.transform.position, new Vector3(10f, 10f, 1f));

            float radioSqr = radioSeguridadJugador * radioSeguridadJugador;
            bool esPrimerGrupo = true;

            while (presupuestoTotalRestante > 0)
            {
                if (salaAsignada.CantidadEnemigosVivos == 0)
                {
                    presupuestoSimultaneoActual = 0;
                    conteoSimultaneoPorTipo.Clear();
                }

                if (salaAsignada.CantidadEnemigosVivos <= umbralRefuerzos || presupuestoSimultaneoActual == 0)
                {
                    if (!esPrimerGrupo)
                    {
                        yield return new WaitForSeconds(delayRefuerzos);
                    }

                    bool noSePudoSpawnearNada = false;

                    while (presupuestoTotalRestante > 0 && presupuestoSimultaneoActual < presupuestoSimultaneoMaximo)
                    {
                        Transform playerT = null;
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null) playerT = player.transform;

                        bool spawnRealizado = false;
                        int spawneadosEnEsteGrupo = 0;
                        int limiteGrupo = esPrimerGrupo ? int.MaxValue : maxEnemigosPorGrupo;

                        while (spawneadosEnEsteGrupo < limiteGrupo && presupuestoTotalRestante > 0 && presupuestoSimultaneoActual < presupuestoSimultaneoMaximo)
                        {
                            EnemigoConfig? configElegida = SeleccionarEnemigoValido();
                            if (configElegida == null) break; 

                            EnemigoConfig config = configElegida.Value;
                            Vector3 spawnPos = BuscarPosicionValida(bounds, playerT, radioSqr);
                            
                            GameObject enemigoGO = SimpleObjectPool.Instance.Get(config.prefab, spawnPos, Quaternion.identity, salaAsignada.transform);

                            VidaEnemigo vida = enemigoGO.GetComponent<VidaEnemigo>();
                            if (vida != null)
                            {
                                vida.salaAsignada = salaAsignada;
                                vida.costePresupuesto = config.coste;
                                vida.prefabOrigen = config.prefab;
                            }

                            salaAsignada.RegistrarEnemigo(enemigoGO);

                            presupuestoTotalRestante -= config.coste;
                            presupuestoSimultaneoActual += config.coste;
                            
                            if (conteoSimultaneoPorTipo.ContainsKey(config.prefab))
                                conteoSimultaneoPorTipo[config.prefab]++;
                            else
                                conteoSimultaneoPorTipo[config.prefab] = 1;

                            StartCoroutine(AparecerConEfecto(enemigoGO));

                            spawnRealizado = true;
                            spawneadosEnEsteGrupo++;
                        }

                        if (esPrimerGrupo && spawnRealizado)
                        {
                            salaAsignada.ChequearPuertasYSpawneo();
                            esPrimerGrupo = false;
                        }

                        if (spawnRealizado && presupuestoTotalRestante > 0)
                        {
                            yield return new WaitForSeconds(tiempoEntreGrupos);
                        }
                        else if (!spawnRealizado)
                        {
                            noSePudoSpawnearNada = true;
                            break;
                        }
                    }

                    if (noSePudoSpawnearNada && presupuestoTotalRestante > 0)
                    {
                        if (presupuestoSimultaneoActual == 0)
                        {
                            presupuestoTotalRestante = 0; 
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
            }

            spawnCompletado = true;
            salaAsignada.ChequearFinSala();
        }

        private IEnumerator AparecerConEfecto(GameObject enemigo)
        {
            Collider2D col = enemigo.GetComponentInChildren<Collider2D>();
            if (col != null) col.enabled = false;

            Vector3 escalaOriginal = enemigo.transform.localScale;
            enemigo.transform.localScale = Vector3.zero;
            
            SpriteRenderer sr = enemigo.GetComponentInChildren<SpriteRenderer>();
            Color colorOriginal = sr != null ? sr.color : Color.white;

            float tiempoEfecto = 0.6f;
            float t = 0;

            while (t < tiempoEfecto)
            {
                if (enemigo == null || !enemigo.activeInHierarchy) yield break;

                t += Time.deltaTime;
                float normalizado = t / tiempoEfecto;
                enemigo.transform.localScale = Vector3.Lerp(Vector3.zero, escalaOriginal, normalizado);
                
                if (sr != null)
                {
                    float alpha = Mathf.Lerp(0.2f, 1f, normalizado);
                    Color parpadeo = (Mathf.FloorToInt(t * 15) % 2 == 0) ? Color.white : colorOriginal;
                    parpadeo.a = alpha;
                    sr.color = parpadeo;
                }

                yield return null;
            }

            if (enemigo != null && enemigo.activeInHierarchy)
            {
                enemigo.transform.localScale = escalaOriginal;
                if (sr != null) sr.color = colorOriginal;

                if (col != null) col.enabled = true;
            }
        }

        public void LiberarPresupuestoSimultaneo(int coste, GameObject prefabOrigen)
        {
            presupuestoSimultaneoActual -= coste;
            if (presupuestoSimultaneoActual < 0) presupuestoSimultaneoActual = 0;

            if (prefabOrigen != null && conteoSimultaneoPorTipo.ContainsKey(prefabOrigen))
            {
                conteoSimultaneoPorTipo[prefabOrigen]--;
                if (conteoSimultaneoPorTipo[prefabOrigen] < 0) 
                    conteoSimultaneoPorTipo[prefabOrigen] = 0;
            }
        }

        private EnemigoConfig? SeleccionarEnemigoValido()
        {
            List<EnemigoConfig> validos = new List<EnemigoConfig>();
            float pesoTotal = 0f;

            int minCostValido = int.MaxValue;
            EnemigoConfig? cheapestValido = null;

            foreach (var config in enemigosDisponibles)
            {
                if (config.prefab == null) continue;
                if (config.coste <= 0) continue;

                if (presupuestoSimultaneoActual + config.coste > presupuestoSimultaneoMaximo) continue;

                if (conteoSimultaneoPorTipo.TryGetValue(config.prefab, out int spawneados))
                {
                    if (config.maximoPorSala > 0 && spawneados >= config.maximoPorSala) continue;
                }

                if (config.coste < minCostValido)
                {
                    minCostValido = config.coste;
                    cheapestValido = config;
                }

                if (config.coste > presupuestoTotalRestante) continue;

                validos.Add(config);
                pesoTotal += Mathf.Max(0.01f, config.peso);
            }

            if (validos.Count == 0)
            {
                if (cheapestValido != null && presupuestoTotalRestante > 0)
                {
                    return cheapestValido;
                }
                return null;
            }

            float randomVal = Random.Range(0f, pesoTotal);
            float acumulado = 0f;

            foreach (var config in validos)
            {
                acumulado += Mathf.Max(0.01f, config.peso);
                if (randomVal <= acumulado)
                    return config;
            }

            return validos[validos.Count - 1];
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

                bool lejosDelJugador = playerT == null || (pos - playerT.position).sqrMagnitude >= radioSqr;
                
                if (lejosDelJugador)
                {
                    Collider2D obstaculo = Physics2D.OverlapCircle(pos, 0.5f, capaObstaculos);
                    if (obstaculo == null)
                    {
                        return pos;
                    }
                }
            }

            return new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        }
    }
}
