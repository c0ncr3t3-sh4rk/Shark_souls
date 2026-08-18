using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SharkSouls.Dungeon
{
    public class GeneradorMazmorra : MonoBehaviour
    {
        [System.Serializable]
        public struct SalaConfig
        {
            [Tooltip("Prefab de la sala normal")]
            public GameObject prefab;
            [Tooltip("Peso o ratio de aparición (a mayor peso, más probable). Deja en 0 para usar peso por defecto = 1.")]
            [Range(0f, 100f)]
            public float peso;
        }

        [System.Serializable]
        public class SalaEspecialConfig
        {
            [Tooltip("Prefab de la sala especial")]
            public GameObject prefab;
            [Tooltip("Peso o ratio de aparición")]
            [Range(0f, 100f)]
            public float peso;
            [Tooltip("Número máximo de veces que puede aparecer en la mazmorra")]
            public int maxApariciones = 1;
            [Tooltip("Indica si esta sala especial debe generar enemigos o es segura")]
            public bool generaEnemigos = true;
            [HideInInspector]
            public int aparicionesRestantes;
        }

        [Header("Prefabs de Salas")]
        [Tooltip("Lista de posibles prefabs para la sala de inicio. Se elegirá uno al azar.")]
        public List<SalaConfig> prefabsStart;
        [Tooltip("Lista de posibles prefabs para la sala del Boss. Se elegirá uno al azar.")]
        public List<SalaConfig> prefabsBoss;
        [Tooltip("Lista de prefabs de salas normales con sus pesos de probabilidad.")]
        public List<SalaConfig> prefabsNormales;
        [Tooltip("Lista de prefabs de salas especiales (ej. tiendas, tesoros).")]
        public List<SalaEspecialConfig> prefabsEspeciales;

        [Header("Dificultad y Enemigos")]
        [Tooltip("Multiplicador de dificultad. 1 = Normal, 2 = Doble de enemigos, 3 = Triple...")]
        [Range(1, 10)]
        public int dificultadGlobal = 1;

        [HideInInspector] public int salasCompletadas = 0;

        [Header("Configuración del Grid")]
        public int numSalas = 15;
        public float tamanoCeldaX = 20f;
        public float tamanoCeldaY = 20f;

        [Header("Reglas Generales")]
        [Tooltip("Probabilidad de que el camino se ramifique creando callejones sin salida")]
        [Range(0f, 1f)]
        public float probabilidadRamificacion = 0.4f;

        [Tooltip("Probabilidad de crear caminos extra (bucles) entre salas adyacentes")]
        [Range(0f, 1f)]
        public float probabilidadBucle = 0.05f;

        [Header("Recompensas")]
        public GameObject prefabTragaperras;

        private Dictionary<Vector2Int, SalaBase> gridSalas = new Dictionary<Vector2Int, SalaBase>();
        private HashSet<Vector2Int> posicionesOcupadas = new HashSet<Vector2Int>();
        private Dictionary<Vector2Int, List<Vector2Int>> conexiones = new Dictionary<Vector2Int, List<Vector2Int>>();

        private void Start()
        {
            if (prefabsEspeciales != null)
            {
                foreach (var especial in prefabsEspeciales)
                    especial.aparicionesRestantes = especial.maxApariciones;
            }

            GenerarMazmorra();
        }

        public void GenerarMazmorra()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            gridSalas.Clear();
            posicionesOcupadas.Clear();
            conexiones.Clear();
            salasCompletadas = 0;

            if (prefabsEspeciales != null)
            {
                foreach (var especial in prefabsEspeciales)
                    especial.aparicionesRestantes = especial.maxApariciones;
            }

            Vector2Int posActual = Vector2Int.zero;
            CrearSala(posActual, prefabsStart[Random.Range(0, prefabsStart.Count)].prefab, TipoSala.Start, CapaSala.Superficie);

            for (int i = 0; i < numSalas - 1; i++) 
            {
                bool colocado = false;
                
                List<GameObject> prefabsParaProbar = new List<GameObject>();

                // Agregar especiales primero si pueden spawnear
                if (prefabsEspeciales != null)
                {
                    var especialesProbar = prefabsEspeciales
                        .Where(c => c.prefab != null && c.aparicionesRestantes > 0)
                        .Select(config => {
                            float peso = Mathf.Max(0.01f, config.peso);
                            double u = Random.value;
                            double key = System.Math.Pow(u, 1.0 / peso);
                            return new { Config = config, Prefab = config.prefab, Key = key, EsEspecial = true };
                        }).ToList();

                    // Se añaden también las normales para competir en peso
                    var normalesProbar = prefabsNormales
                        .Where(c => c.prefab != null)
                        .Select(config => {
                            float peso = Mathf.Max(0.01f, config.peso);
                            if (config.peso <= 0f) peso = 10f;
                            double u = Random.value;
                            double key = System.Math.Pow(u, 1.0 / peso);
                            return new { Config = (SalaEspecialConfig)null, Prefab = config.prefab, Key = key, EsEspecial = false };
                        }).ToList();

                    var combinadas = especialesProbar.Concat(normalesProbar)
                        .OrderByDescending(x => x.Key)
                        .ToList();

                    foreach (var item in combinadas)
                    {
                        prefabsParaProbar.Add(item.Prefab);
                    }
                }
                else
                {
                    prefabsParaProbar = prefabsNormales
                        .Where(c => c.prefab != null)
                        .Select(config => {
                            float peso = Mathf.Max(0.01f, config.peso);
                            if (config.peso <= 0f) peso = 10f;
                            double u = Random.value;
                            double key = System.Math.Pow(u, 1.0 / peso);
                            return new { Prefab = config.prefab, Key = key };
                        })
                        .OrderByDescending(x => x.Key)
                        .Select(x => x.Prefab)
                        .ToList();
                }

                foreach (var prefabElegido in prefabsParaProbar)
                {
                    bool ramificar = Random.value < probabilidadRamificacion;

                    if (!ramificar)
                    {
                        CapaSala capa = gridSalas[posActual].capa;
                        List<Vector2Int> dirs = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                        
                        if (capa == CapaSala.Superficie) dirs.Remove(Vector2Int.up);
                        if (capa == CapaSala.Profundo) dirs.Remove(Vector2Int.down);

                        dirs = dirs.OrderBy(x => Random.value).ToList();
                        foreach (var dir in dirs)
                        {
                            if (IntentarEncajarPrefab(prefabElegido, posActual, dir, out Vector2Int pivotPos, out CapaSala nuevaCapa))
                            {
                                bool debeSpawnearEnemigos = true;
                                TipoSala tipoSalaAsignado = TipoSala.Normal;
                                if (prefabsEspeciales != null && prefabsEspeciales.Any(e => e.prefab == prefabElegido))
                                {
                                    tipoSalaAsignado = TipoSala.Especial;
                                    var especialConf = prefabsEspeciales.First(e => e.prefab == prefabElegido);
                                    debeSpawnearEnemigos = especialConf.generaEnemigos;
                                    especialConf.aparicionesRestantes--;
                                }

                                CrearSala(pivotPos, prefabElegido, tipoSalaAsignado, nuevaCapa, debeSpawnearEnemigos);
                                Vector2Int nuevaPos = posActual + dir;
                                Conectar(posActual, nuevaPos);
                                posActual = nuevaPos; 
                                colocado = true;
                                break;
                            }
                        }
                    }

                    if (colocado) break;

                    List<Vector2Int> celdasOcupadasBarajadas = new List<Vector2Int>(posicionesOcupadas);
                    celdasOcupadasBarajadas = celdasOcupadasBarajadas.OrderBy(x => Random.value).ToList();
                    foreach (var pos in celdasOcupadasBarajadas)
                    {
                        CapaSala capaAlt = gridSalas[pos].capa;
                        List<Vector2Int> dirsAlt = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                        
                        if (capaAlt == CapaSala.Superficie) dirsAlt.Remove(Vector2Int.up);
                        if (capaAlt == CapaSala.Profundo) dirsAlt.Remove(Vector2Int.down);

                        dirsAlt = dirsAlt.OrderBy(x => Random.value).ToList();
                        foreach (var dir in dirsAlt)
                        {
                            if (IntentarEncajarPrefab(prefabElegido, pos, dir, out Vector2Int pivotPos, out CapaSala nuevaCapa))
                            {
                                bool debeSpawnearEnemigos = true;
                                TipoSala tipoSalaAsignado = TipoSala.Normal;
                                if (prefabsEspeciales != null && prefabsEspeciales.Any(e => e.prefab == prefabElegido))
                                {
                                    tipoSalaAsignado = TipoSala.Especial;
                                    var especialConf = prefabsEspeciales.First(e => e.prefab == prefabElegido);
                                    debeSpawnearEnemigos = especialConf.generaEnemigos;
                                    especialConf.aparicionesRestantes--;
                                }

                                CrearSala(pivotPos, prefabElegido, tipoSalaAsignado, nuevaCapa, debeSpawnearEnemigos);
                                Vector2Int nuevaPos = pos + dir;
                                Conectar(pos, nuevaPos);
                                posActual = nuevaPos; 
                                colocado = true;
                                break;
                            }
                        }
                        if (colocado) break;
                    }

                    if (colocado) break;
                }

                if (!colocado)
                {
                    Debug.LogWarning($"No se pudo encontrar espacio libre para encajar ninguna sala en la iteración {i}.");
                    break;
                }
            }

            ConvertirEnBoss();

            GenerarBuclesAdicionales();

            ActualizarPuertasYCapas();
        }

        private bool IntentarEncajarPrefab(GameObject prefab, Vector2Int posPadre, Vector2Int dir, out Vector2Int pivotPos, out CapaSala nuevaCapa)
        {
            pivotPos = Vector2Int.zero;
            nuevaCapa = CapaSala.Medio;

            SalaBase salaScript = prefab.GetComponent<SalaBase>();
            if (salaScript == null) return false;

            List<Vector2Int> celdasLocal = salaScript.celdasOcupadas;
            if (celdasLocal == null || celdasLocal.Count == 0)
            {
                celdasLocal = new List<Vector2Int> { Vector2Int.zero };
            }

            Vector2Int nuevaPos = posPadre + dir;

            CapaSala capaPadre = gridSalas[posPadre].capa;
            nuevaCapa = DeterminarCapa(capaPadre, dir);

            List<Vector2Int> celdasOrdenadas = celdasLocal.OrderBy(x => Random.value).ToList();
            foreach (var celdaLocalCoincidente in celdasOrdenadas)
            {
                Vector2Int testPivot = nuevaPos - celdaLocalCoincidente;
                bool encaja = true;

                foreach (var cl in celdasLocal)
                {
                    Vector2Int cg = testPivot + cl;
                    if (gridSalas.ContainsKey(cg))
                    {
                        encaja = false;
                        break;
                    }
                }

                if (encaja)
                {
                    pivotPos = testPivot;
                    return true;
                }
            }

            return false;
        }

        private void CrearSala(Vector2Int posGrid, GameObject prefab, TipoSala tipo, CapaSala capa, bool spawnearEnemigos = true)
        {
            Vector3 posicionMundo = new Vector3(posGrid.x * tamanoCeldaX, posGrid.y * tamanoCeldaY, 0);
            GameObject inst = Instantiate(prefab, posicionMundo, Quaternion.identity, this.transform);
            inst.name = $"Sala_{tipo}_{posGrid.x}_{posGrid.y}_{capa}";
            
            SalaBase salaScript = inst.GetComponent<SalaBase>();
            if (salaScript == null) salaScript = inst.AddComponent<SalaBase>();
            
            salaScript.tipo = tipo;
            salaScript.capa = capa;
            salaScript.posGrid = posGrid;

            // Añadir LimiteSala al collider de la cámara
            if (salaScript.boundsCamara != null && salaScript.boundsCamara.gameObject.GetComponent<LimiteSala>() == null)
            {
                salaScript.boundsCamara.gameObject.AddComponent<LimiteSala>();
            }

            if (spawnearEnemigos && (tipo == TipoSala.Normal || tipo == TipoSala.Especial))
            {
                salaScript.requiereSpawneo = true;
                salaScript.dificultadAlCrear = dificultadGlobal;
            }

            List<Vector2Int> celdasLocal = salaScript.celdasOcupadas;
            if (celdasLocal == null || celdasLocal.Count == 0)
            {
                celdasLocal = new List<Vector2Int> { Vector2Int.zero };
            }

            foreach (var celdaLocal in celdasLocal)
            {
                Vector2Int celdaGlobal = posGrid + celdaLocal;
                gridSalas[celdaGlobal] = salaScript;
                posicionesOcupadas.Add(celdaGlobal);
            }
        }

        private CapaSala DeterminarCapa(CapaSala capaPadre, Vector2Int dirDesdePadre)
        {
            List<CapaSala> posibles = new List<CapaSala>();
            if (capaPadre == CapaSala.Superficie)
            {
                posibles.Add(CapaSala.Superficie);
                posibles.Add(CapaSala.Medio);
            }
            else if (capaPadre == CapaSala.Medio)
            {
                posibles.Add(CapaSala.Superficie);
                posibles.Add(CapaSala.Medio);
                posibles.Add(CapaSala.Profundo);
            }
            else if (capaPadre == CapaSala.Profundo)
            {
                posibles.Add(CapaSala.Medio);
                posibles.Add(CapaSala.Profundo);
            }

            if (dirDesdePadre == Vector2Int.down) posibles.Remove(CapaSala.Superficie);
            
            if (dirDesdePadre == Vector2Int.up) posibles.Remove(CapaSala.Profundo);

            if (posibles.Count == 0) return CapaSala.Medio; 

            return posibles[Random.Range(0, posibles.Count)];
        }

        private void ConvertirEnBoss()
        {
            List<Vector2Int> hojas = new List<Vector2Int>();
            HashSet<SalaBase> salasEvaluadas = new HashSet<SalaBase>();

            foreach (var pos in posicionesOcupadas)
            {
                if (pos == Vector2Int.zero) continue;
                if (!gridSalas.ContainsKey(pos)) continue;

                SalaBase sala = gridSalas[pos];
                if (salasEvaluadas.Contains(sala)) continue;
                salasEvaluadas.Add(sala);

                HashSet<Vector2Int> celdasDeEstaSala = new HashSet<Vector2Int>();
                Vector2Int pivot = sala.posGrid;
                foreach (var cl in sala.celdasOcupadas)
                {
                    celdasDeEstaSala.Add(pivot + cl);
                }

                Vector2Int cellConexionExterna = Vector2Int.zero;
                int conexionesExternas = 0;

                foreach (var cell in celdasDeEstaSala)
                {
                    if (conexiones.ContainsKey(cell))
                    {
                        foreach (var target in conexiones[cell])
                        {
                            if (!celdasDeEstaSala.Contains(target))
                            {
                                conexionesExternas++;
                                cellConexionExterna = cell;
                            }
                        }
                    }
                }

                if (conexionesExternas == 1)
                {
                    if (Mathf.Abs(cellConexionExterna.x) + Mathf.Abs(cellConexionExterna.y) > 1)
                    {
                        if (!hojas.Contains(cellConexionExterna))
                        {
                            hojas.Add(cellConexionExterna);
                        }
                    }
                }
            }

            hojas = hojas.OrderBy(x => Random.value).ToList();

            List<GameObject> bossPrefabsParaProbar = prefabsBoss
                .Where(c => c.prefab != null)
                .Select(config => {
                    float peso = Mathf.Max(0.01f, config.peso);
                    if (config.peso <= 0f) peso = 10f; 
                    double u = Random.value;
                    double key = System.Math.Pow(u, 1.0 / peso);
                    return new { Prefab = config.prefab, Key = key };
                })
                .OrderByDescending(x => x.Key)
                .Select(x => x.Prefab)
                .ToList();

            bool bossColocado = false;

            foreach (var prefabBossElegido in bossPrefabsParaProbar)
            {
                SalaBase scriptBoss = prefabBossElegido.GetComponent<SalaBase>();
                List<Vector2Int> celdasLocalBoss = scriptBoss != null && scriptBoss.celdasOcupadas.Count > 0 
                    ? scriptBoss.celdasOcupadas 
                    : new List<Vector2Int> { Vector2Int.zero };

                foreach (var leafCell in hojas)
                {
                    if (!gridSalas.ContainsKey(leafCell)) continue; 

                    SalaBase salaVieja = gridSalas[leafCell];
                    Vector2Int pivotViejo = salaVieja.posGrid;
                    HashSet<Vector2Int> celdasViejas = new HashSet<Vector2Int>();
                    foreach (var cl in salaVieja.celdasOcupadas)
                    {
                        celdasViejas.Add(pivotViejo + cl);
                    }

                    List<Vector2Int> celdasOrdenadasBoss = celdasLocalBoss.OrderBy(x => Random.value).ToList();
                    foreach (var celdaLocalCoincidente in celdasOrdenadasBoss)
                    {
                        Vector2Int testPivot = leafCell - celdaLocalCoincidente;
                        bool encaja = true;

                        foreach (var cl in celdasLocalBoss)
                        {
                            Vector2Int cg = testPivot + cl;
                            if (gridSalas.ContainsKey(cg) && !celdasViejas.Contains(cg))
                            {
                                encaja = false;
                                break;
                            }
                        }

                        if (encaja)
                        {
                            CapaSala capaBoss = salaVieja.capa;

                            foreach (var celda in celdasViejas)
                            {
                                if (celda != leafCell)
                                {
                                    if (conexiones.ContainsKey(celda))
                                    {
                                        foreach (var target in conexiones[celda])
                                        {
                                            if (conexiones.ContainsKey(target))
                                            {
                                                conexiones[target].Remove(celda);
                                            }
                                        }
                                        conexiones.Remove(celda);
                                    }
                                }
                            }

                            foreach (var celda in celdasViejas)
                            {
                                gridSalas.Remove(celda);
                                posicionesOcupadas.Remove(celda);
                            }

                            Destroy(salaVieja.gameObject);

                            CrearSala(testPivot, prefabBossElegido, TipoSala.Boss, capaBoss);
                            bossColocado = true;
                            break;
                        }
                    }

                    if (bossColocado) break;
                }

                if (bossColocado) break;
            }

            if (!bossColocado)
            {
                Debug.LogWarning("No se pudo encajar ningún prefab de Boss en ninguna hoja sin solapamientos. Usando fallback de destrucción forzada.");
                if (hojas.Count > 0 && bossPrefabsParaProbar.Count > 0)
                {
                    Vector2Int fallbackCell = hojas[0];
                    if (gridSalas.ContainsKey(fallbackCell))
                    {
                        SalaBase salaVieja = gridSalas[fallbackCell];
                        Vector2Int pivotViejo = salaVieja.posGrid;
                        CapaSala capaBoss = salaVieja.capa;

                        HashSet<Vector2Int> celdasViejas = new HashSet<Vector2Int>();
                        foreach (var cl in salaVieja.celdasOcupadas)
                        {
                            celdasViejas.Add(pivotViejo + cl);
                        }

                        foreach (var celda in celdasViejas)
                        {
                            if (celda != fallbackCell)
                            {
                                if (conexiones.ContainsKey(celda))
                                {
                                    foreach (var target in conexiones[celda])
                                    {
                                        if (conexiones.ContainsKey(target))
                                        {
                                            conexiones[target].Remove(celda);
                                        }
                                    }
                                    conexiones.Remove(celda);
                                }
                            }
                        }

                        foreach (var celda in celdasViejas)
                        {
                            gridSalas.Remove(celda);
                            posicionesOcupadas.Remove(celda);
                        }
                        Destroy(salaVieja.gameObject);

                        GameObject fallbackPrefab = bossPrefabsParaProbar[0];
                        CrearSala(fallbackCell, fallbackPrefab, TipoSala.Boss, capaBoss);
                    }
                }
            }
        }

        private void Conectar(Vector2Int a, Vector2Int b)
        {
            if (!conexiones.ContainsKey(a)) conexiones[a] = new List<Vector2Int>();
            if (!conexiones.ContainsKey(b)) conexiones[b] = new List<Vector2Int>();
            
            if (!conexiones[a].Contains(b)) conexiones[a].Add(b);
            if (!conexiones[b].Contains(a)) conexiones[b].Add(a);
        }

        private bool EstanConectadas(Vector2Int a, Vector2Int b)
        {
            if (conexiones.ContainsKey(a)) return conexiones[a].Contains(b);
            return false;
        }

        private void GenerarBuclesAdicionales()
        {
            var listaPos = new List<Vector2Int>(posicionesOcupadas);
            for (int i = 0; i < listaPos.Count; i++)
            {
                for (int j = i + 1; j < listaPos.Count; j++)
                {
                    Vector2Int pos1 = listaPos[i];
                    Vector2Int pos2 = listaPos[j];

                    if (Vector2Int.Distance(pos1, pos2) == 1f && !EstanConectadas(pos1, pos2))
                    {
                        if (gridSalas[pos1] == gridSalas[pos2]) continue;

                        if (Random.value < probabilidadBucle)
                        {
                            CapaSala c1 = gridSalas[pos1].capa;
                            CapaSala c2 = gridSalas[pos2].capa;
                            
                            if ((c1 == CapaSala.Superficie && c2 == CapaSala.Profundo) ||
                                (c1 == CapaSala.Profundo && c2 == CapaSala.Superficie))
                            {
                                continue;
                            }

                            Vector2Int dir = pos2 - pos1; 
                            if (c1 == CapaSala.Superficie && dir == Vector2Int.up) continue;
                            if (c2 == CapaSala.Superficie && dir == Vector2Int.down) continue;
                            
                            if (c1 == CapaSala.Profundo && dir == Vector2Int.down) continue;
                            if (c2 == CapaSala.Profundo && dir == Vector2Int.up) continue;

                            Conectar(pos1, pos2);
                        }
                    }
                }
            }
        }

        private void ActualizarPuertasYCapas()
        {
            HashSet<SalaBase> salasUnicas = new HashSet<SalaBase>(gridSalas.Values);
            foreach (var sala in salasUnicas)
            {
                if (sala.conectores != null)
                {
                    foreach (var conector in sala.conectores)
                    {
                        conector.estaConectado = false;
                    }
                }
            }

            foreach (var kvp in gridSalas)
            {
                Vector2Int cellGlobal = kvp.Key;
                SalaBase sala = kvp.Value;
                Vector2Int posGrid = sala.posGrid;

                if (sala.conectores != null)
                {
                    foreach (var conector in sala.conectores)
                    {
                        Vector2Int conectorCellGlobal = posGrid + conector.celdaLocal;
                        if (conectorCellGlobal == cellGlobal)
                        {
                            Vector2Int targetGlobal = cellGlobal + conector.direccion;
                            if (conexiones.ContainsKey(cellGlobal) && conexiones[cellGlobal].Contains(targetGlobal))
                            {
                                conector.estaConectado = true;
                            }
                        }
                    }
                }
            }

            foreach (var sala in salasUnicas)
            {
                sala.ConfigurarPuertasVisuales();
            }
        }

        public void SalaCompletadaCallback()
        {
            salasCompletadas++;
        }
    }
}
