using UnityEngine;
using System.Collections.Generic;

namespace SharkSouls.Utils
{
    public class SimpleObjectPool : MonoBehaviour
    {
        private static SimpleObjectPool _instance;
        public static SimpleObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SimpleObjectPool");
                    _instance = go.AddComponent<SimpleObjectPool>();
                    DontDestroyOnLoad(go); // Para que persista entre escenas
                }
                return _instance;
            }
        }

        private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!poolDictionary.ContainsKey(prefab))
            {
                poolDictionary[prefab] = new Queue<GameObject>();
            }

            GameObject objToSpawn = null;

            while (poolDictionary[prefab].Count > 0)
            {
                GameObject obj = poolDictionary[prefab].Dequeue();
                if (obj != null)
                {
                    objToSpawn = obj;
                    break;
                }
            }

            if (objToSpawn == null)
            {
                objToSpawn = Instantiate(prefab);
                instanceToPrefabMap[objToSpawn] = prefab;
            }

            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.transform.SetParent(parent);
            objToSpawn.SetActive(true);

            return objToSpawn;
        }

        public void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;

            instance.SetActive(false);
            
            // Movemos el objeto devuelto como hijo del pool para limpiar la jerarquía
            instance.transform.SetParent(transform);

            if (instanceToPrefabMap.TryGetValue(instance, out GameObject prefab))
            {
                if (!poolDictionary.ContainsKey(prefab))
                {
                    poolDictionary[prefab] = new Queue<GameObject>();
                }
                poolDictionary[prefab].Enqueue(instance);
            }
            else
            {
                // Si el objeto no fue creado por este Pool (fallback), lo destruimos
                Destroy(instance);
            }
        }
    }
}
