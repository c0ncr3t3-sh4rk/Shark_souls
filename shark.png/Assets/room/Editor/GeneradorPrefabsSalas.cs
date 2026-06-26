#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SharkSouls.Dungeon;
using System.IO;
using System.Collections.Generic;

namespace SharkSouls.Dungeon.Editor
{
    public class GeneradorPrefabsSalas : EditorWindow
    {
        private string nombreSala = "Nueva Sala";
        private CapaSala capa = CapaSala.Medio;
        private TipoSala tipo = TipoSala.Normal;
        private string estiloTag = "PorDefecto";
        private Sprite wallSprite;

        private float tamanoCeldaX = 20f;
        private float tamanoCeldaY = 20f;

        // Grid máximo de 5x5 para pintar formas
        private bool[,] shapeGrid = new bool[5, 5];
        private int centerX = 2;
        private int centerY = 2; // El centro siempre es 2,2

        [MenuItem("SharkSouls/Generador de Salas")]
        public static void MostrarVentana()
        {
            GetWindow<GeneradorPrefabsSalas>("Generador Salas");
        }

        private void OnEnable()
        {
            shapeGrid[centerX, centerY] = true; // El centro siempre debe estar activo
        }

        private void OnGUI()
        {
            GUILayout.Label("Ajustes de la Sala", EditorStyles.boldLabel);

            nombreSala = EditorGUILayout.TextField("Nombre", nombreSala);
            capa = (CapaSala)EditorGUILayout.EnumPopup("Capa", capa);
            tipo = (TipoSala)EditorGUILayout.EnumPopup("Tipo", tipo);
            estiloTag = EditorGUILayout.TextField("Tag Estilo", estiloTag);
            wallSprite = (Sprite)EditorGUILayout.ObjectField("Textura Pared", wallSprite, typeof(Sprite), false);

            GUILayout.Space(10);
            GUILayout.Label("Forma de la Sala (Diseña tu L, Cuadrado, etc)", EditorStyles.boldLabel);
            
            // Dibujar grid de toggles
            for (int y = 0; y < 5; y++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int x = 0; x < 5; x++)
                {
                    // Evitar desactivar el centro
                    if (x == centerX && y == centerY)
                    {
                        GUI.enabled = false;
                        GUILayout.Toggle(true, "C", GUILayout.Width(30), GUILayout.Height(30));
                        GUI.enabled = true;
                    }
                    else
                    {
                        shapeGrid[x, y] = GUILayout.Toggle(shapeGrid[x, y], "", GUILayout.Width(30), GUILayout.Height(30));
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Generar Prefab de Sala Multi-Celda", GUILayout.Height(40)))
            {
                GenerarSala();
            }
        }

        private void GenerarSala()
        {
            GameObject salaGO = new GameObject(nombreSala);
            SalaBase salaScript = salaGO.AddComponent<SalaBase>();
            
            salaScript.capa = capa;
            salaScript.tipo = tipo;
            salaScript.estiloTag = estiloTag;
            salaScript.celdasOcupadas.Clear();
            salaScript.conectores.Clear();

            float grosorPared = 1f;
            float huecoPuerta = 4f;

            // Recorrer el grid para construir
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (shapeGrid[x, y])
                    {
                        // Coordenadas relativas al centro
                        int localX = x - centerX;
                        int localY = -(y - centerY); // Invertir Y porque en la UI el Y=0 es arriba
                        
                        Vector2Int celda = new Vector2Int(localX, localY);
                        salaScript.celdasOcupadas.Add(celda);

                        Vector2 centroFisico = new Vector2(localX * tamanoCeldaX, localY * tamanoCeldaY);

                        // Comprobar bordes
                        if (EsBorde(x, y - 1)) // Arriba (en UI Y-1 es arriba)
                            GenerarLadoYConector(salaGO, salaScript, celda, Vector2Int.up, centroFisico + new Vector2(0, tamanoCeldaY / 2f), new Vector2(tamanoCeldaX, grosorPared), wallSprite, huecoPuerta, true);

                        if (EsBorde(x, y + 1)) // Abajo
                            GenerarLadoYConector(salaGO, salaScript, celda, Vector2Int.down, centroFisico + new Vector2(0, -tamanoCeldaY / 2f), new Vector2(tamanoCeldaX, grosorPared), wallSprite, huecoPuerta, true);

                        if (EsBorde(x - 1, y)) // Izquierda
                            GenerarLadoYConector(salaGO, salaScript, celda, Vector2Int.left, centroFisico + new Vector2(-tamanoCeldaX / 2f, 0), new Vector2(grosorPared, tamanoCeldaY), wallSprite, huecoPuerta, false);

                        if (EsBorde(x + 1, y)) // Derecha
                            GenerarLadoYConector(salaGO, salaScript, celda, Vector2Int.right, centroFisico + new Vector2(tamanoCeldaX / 2f, 0), new Vector2(grosorPared, tamanoCeldaY), wallSprite, huecoPuerta, false);
                    }
                }
            }

            // Calcular bounds de cámara única que abarca todas las celdas ocupadas
            if (salaScript.celdasOcupadas.Count > 0)
            {
                int minCellX = int.MaxValue;
                int maxCellX = int.MinValue;
                int minCellY = int.MaxValue;
                int maxCellY = int.MinValue;

                foreach (var celda in salaScript.celdasOcupadas)
                {
                    if (celda.x < minCellX) minCellX = celda.x;
                    if (celda.x > maxCellX) maxCellX = celda.x;
                    if (celda.y < minCellY) minCellY = celda.y;
                    if (celda.y > maxCellY) maxCellY = celda.y;
                }

                float width = (maxCellX - minCellX + 1) * tamanoCeldaX;
                float height = (maxCellY - minCellY + 1) * tamanoCeldaY;
                float centerBoundingX = (minCellX + maxCellX) / 2f * tamanoCeldaX;
                float centerBoundingY = (minCellY + maxCellY) / 2f * tamanoCeldaY;

                BoxCollider2D colliderCamara = salaGO.AddComponent<BoxCollider2D>();
                colliderCamara.isTrigger = true;
                colliderCamara.size = new Vector2(width, height);
                colliderCamara.offset = new Vector2(centerBoundingX, centerBoundingY);
                salaScript.boundsCamara = colliderCamara;
            }

            // Guardar prefab
            string path = "Assets/room/Prefabs";
            if (!Directory.Exists(Application.dataPath + "/room/Prefabs"))
                Directory.CreateDirectory(Application.dataPath + "/room/Prefabs");

            string prefabPath = $"{path}/{nombreSala}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAssetAndConnect(salaGO, prefabPath, InteractionMode.UserAction);
            Debug.Log($"<color=green>Sala '{nombreSala}' generada (Multi-Celda) en {prefabPath}</color>");

            Selection.activeGameObject = salaGO;
        }

        private bool EsBorde(int x, int y)
        {
            if (x < 0 || x >= 5 || y < 0 || y >= 5) return true; // Fuera del grid = espacio vacío = necesitamos pared
            return !shapeGrid[x, y]; // Si no hay sala en esa celda, es borde y necesitamos pared
        }

        private void GenerarLadoYConector(GameObject padre, SalaBase salaScript, Vector2Int celda, Vector2Int dir, Vector2 posicion, Vector2 escalaPared, Sprite sprite, float huecoPuerta, bool horizontal)
        {
            string strDir = dir == Vector2Int.up ? "Arriba" : (dir == Vector2Int.down ? "Abajo" : (dir == Vector2Int.left ? "Izquierda" : "Derecha"));
            string baseName = $"{celda.x}_{celda.y}_{strDir}";

            // 1. PARED SÓLIDA
            GameObject pared = new GameObject($"Pared_{baseName}");
            pared.transform.SetParent(padre.transform);
            pared.transform.localPosition = posicion;
            AplicarSpriteYColision(pared, escalaPared, sprite);

            // 2. PUERTA
            GameObject puerta = new GameObject($"Puerta_{baseName}");
            puerta.transform.SetParent(padre.transform);
            puerta.transform.localPosition = Vector3.zero;

            float longitudTrozos = horizontal ? (escalaPared.x - huecoPuerta) / 2f : (escalaPared.y - huecoPuerta) / 2f;
            float grosor = horizontal ? escalaPared.y : escalaPared.x;

            float offsetTrozos = huecoPuerta / 2f + longitudTrozos / 2f;
            Vector2 offsetPositivo = horizontal ? new Vector2(posicion.x + offsetTrozos, posicion.y) : new Vector2(posicion.x, posicion.y + offsetTrozos);
            Vector2 offsetNegativo = horizontal ? new Vector2(posicion.x - offsetTrozos, posicion.y) : new Vector2(posicion.x, posicion.y - offsetTrozos);
            Vector2 escalaTrozos = horizontal ? new Vector2(longitudTrozos, grosor) : new Vector2(grosor, longitudTrozos);

            GameObject trozo1 = new GameObject("TrozoMuro_1");
            trozo1.transform.SetParent(puerta.transform);
            trozo1.transform.localPosition = offsetPositivo;
            AplicarSpriteYColision(trozo1, escalaTrozos, sprite);

            GameObject trozo2 = new GameObject("TrozoMuro_2");
            trozo2.transform.SetParent(puerta.transform);
            trozo2.transform.localPosition = offsetNegativo;
            AplicarSpriteYColision(trozo2, escalaTrozos, sprite);

            // Enlazar al script
            ConectorPuerta conector = new ConectorPuerta
            {
                celdaLocal = celda,
                direccion = dir,
                visualPuerta = puerta,
                visualPared = pared
            };
            salaScript.conectores.Add(conector);
        }

        private void AplicarSpriteYColision(GameObject obj, Vector2 tamanoDeseadoMundo, Sprite sprite)
        {
            if (sprite != null)
            {
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                float spriteWidth = sprite.bounds.size.x == 0 ? 1f : sprite.bounds.size.x;
                float spriteHeight = sprite.bounds.size.y == 0 ? 1f : sprite.bounds.size.y;
                obj.transform.localScale = new Vector3(tamanoDeseadoMundo.x / spriteWidth, tamanoDeseadoMundo.y / spriteHeight, 1f);
            }
            else
            {
                obj.transform.localScale = new Vector3(tamanoDeseadoMundo.x, tamanoDeseadoMundo.y, 1f);
            }

            obj.AddComponent<BoxCollider2D>();
        }
    }
}
#endif
