using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Basit, jenerik nesne havuzu. Coin ve yakıt bidonları gibi sık oluşturulup
    /// silinen nesnelerin yeniden kullanılması (geri dönüşüm) için kullanılır.
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();

        public ObjectPool(GameObject prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _available.Enqueue(go);
            }
        }

        /// <summary>Havuzdan bir nesne alır (yoksa yeni üretir) ve aktif eder.</summary>
        public GameObject Get(Vector3 position)
        {
            GameObject go = _available.Count > 0 ? _available.Dequeue() : Object.Instantiate(_prefab, _parent);
            go.transform.SetParent(_parent, true);
            go.transform.position = position;
            go.SetActive(true);
            return go;
        }

        /// <summary>Nesneyi pasifleştirip havuza geri verir. Zaten havuzdaysa (pasifse) yok sayar.</summary>
        public void Return(GameObject go)
        {
            if (go == null || !go.activeSelf) return; // idempotent: çift iade engellenir
            go.SetActive(false);
            go.transform.SetParent(_parent, false);
            _available.Enqueue(go);
        }
    }
}
