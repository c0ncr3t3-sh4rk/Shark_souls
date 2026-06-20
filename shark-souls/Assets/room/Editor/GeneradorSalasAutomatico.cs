#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SharkSouls.Dungeon;
using System.Linq;
using System.Collections.Generic;

namespace SharkSouls.Dungeon.Editor
{
    public class GeneradorSalasAutomatico : UnityEditor.Editor
    {
        [MenuItem("SharkSouls/AUTO-GENERAR 5 Salas")]
        public static void GenerarSalas()
        {
            // 1. Buscar la textura llamada "Wall"
            string[] guids = AssetDatabase.FindAssets("Wall t:Texture2D");
            Sprite wallSprite = null;
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach(var asset in assets) 
                {
                    if (asset is Sprite s) 
                    {
                        wallSprite = s;
                        break;
                    }
                }
            }
            
            if (wallSprite == null)
            {
                Debug.LogWarning("No se encontró un Sprite en la textura 'Wall'. Se generarán cuadrados en blanco.");
            }

            // 2. Generar las 5 salas base requeridas
            GenerarPrefab("Sala Start", CapaSala.Superficie, TipoSala.Start, wallSprite);
            GenerarPrefab("Sala Boss", CapaSala.Profundo, TipoSala.Boss, wallSprite);
            GenerarPrefab("Sala Normal Superficie", CapaSala.Superficie, TipoSala.Normal, wallSprite);
            GenerarPrefab("Sala Normal Medio", CapaSala.Medio, TipoSala.Normal, wallSprite);
            GenerarPrefab("Sala Normal Profundo", CapaSala.Profundo, TipoSala.Normal, wallSprite);
            
            AssetDatabase.SaveAssets();
            Debug.Log("<color=cyan>¡Las 5 salas indispensables de 20x20 se han generado correctamente con físicas y gráficos!</color>");
        }
        
        private static void GenerarPrefab(string nombre, CapaSala capa, TipoSala tipo, Sprite wallSprite)
        {
            GameObject salaGO = new GameObject(nombre);
            SalaBase salaScript = salaGO.AddComponent<SalaBase>();
            
            salaScript.capa = capa;
            salaScript.tipo = tipo;
            salaScript.estiloTag = "PorDefecto";
            salaScript.celdasOcupadas = new List<Vector2Int> { Vector2Int.zero };
            salaScript.conectores = new List<ConectorPuerta>();

            // Tamaño requerido de 20
            float tamano = 20f;
            float grosorPared = 1f;
            float huecoPuerta = 4f; // El espacio por donde pasará el jugador

            // Collider de la cámara
            BoxCollider2D colliderCamara = salaGO.AddComponent<BoxCollider2D>();
            colliderCamara.isTrigger = true;
            colliderCamara.size = new Vector2(tamano, tamano);
            salaScript.boundsCamara = colliderCamara;

            // Generar los 4 lados de la sala
            CrearLado(salaGO, "Arriba", new Vector2(0, tamano/2f), new Vector2(tamano, grosorPared), wallSprite, huecoPuerta, true);
            CrearLado(salaGO, "Abajo", new Vector2(0, -tamano/2f), new Vector2(tamano, grosorPared), wallSprite, huecoPuerta, true);
            CrearLado(salaGO, "Izquierda", new Vector2(-tamano/2f, 0), new Vector2(grosorPared, tamano), wallSprite, huecoPuerta, false);
            CrearLado(salaGO, "Derecha", new Vector2(tamano/2f, 0), new Vector2(grosorPared, tamano), wallSprite, huecoPuerta, false);

            if (!System.IO.Directory.Exists(Application.dataPath + "/room/Prefabs"))
                System.IO.Directory.CreateDirectory(Application.dataPath + "/room/Prefabs");

            string prefabPath = $"Assets/room/Prefabs/{nombre}.prefab";
            // Borramos el prefab anterior si existe para sobreescribirlo limpio
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            PrefabUtility.SaveAsPrefabAssetAndConnect(salaGO, prefabPath, InteractionMode.AutomatedAction);
            DestroyImmediate(salaGO);
        }

        private static void CrearLado(GameObject padre, string direccion, Vector2 posicion, Vector2 escalaPared, Sprite sprite, float huecoPuerta, bool horizontal)
        {
            SalaBase s = padre.GetComponent<SalaBase>();

            // 1. PARED SÓLIDA (Aparece cuando NO hay puerta, tapando todo el borde)
            GameObject pared = new GameObject($"Pared_{direccion}");
            pared.transform.SetParent(padre.transform);
            pared.transform.localPosition = posicion;
            AplicarSpriteYColision(pared, escalaPared, sprite);

            // 2. PARED CON PUERTA (Aparece cuando SÍ hay puerta. Son 2 trozos con un hueco en el centro)
            GameObject puerta = new GameObject($"Puerta_{direccion}");
            puerta.transform.SetParent(padre.transform);
            puerta.transform.localPosition = Vector3.zero; // Local 0,0 porque sus hijos tienen el offset real

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
            Vector2Int dir = Vector2Int.zero;
            if (direccion == "Arriba") dir = Vector2Int.up;
            else if (direccion == "Abajo") dir = Vector2Int.down;
            else if (direccion == "Izquierda") dir = Vector2Int.left;
            else if (direccion == "Derecha") dir = Vector2Int.right;

            ConectorPuerta conector = new ConectorPuerta
            {
                celdaLocal = Vector2Int.zero,
                direccion = dir,
                visualPuerta = puerta,
                visualPared = pared
            };
            s.conectores.Add(conector);
        }

        private static void AplicarSpriteYColision(GameObject obj, Vector2 tamanoDeseadoMundo, Sprite sprite)
        {
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Calcular cuánto hay que escalar el objeto para que mida exactamente "tamanoDeseadoMundo" en unidades de Unity
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 1f;
            
            // Si por algún motivo el sprite mide 0 (error muy raro), evitamos dividir por 0
            if (spriteWidth == 0) spriteWidth = 1f;
            if (spriteHeight == 0) spriteHeight = 1f;

            obj.transform.localScale = new Vector3(tamanoDeseadoMundo.x / spriteWidth, tamanoDeseadoMundo.y / spriteHeight, 1f);

            // Colisión 2D
            obj.AddComponent<BoxCollider2D>();
        }
    }
}
#endif
