using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Sonsuz, prosedürel arazi üreticisi.
    /// Oyuncunun önünde parçalar üretir, arkada kalanları geri dönüştürür.
    /// Arazi üzerinde coin ve yakıt bidonu yerleştirir; bunlar havuzdan gelir.
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Parça Ayarları")]
        public int chunkCount = 8;        // Aynı anda yaşayan parça sayısı
        public float chunkWidth = 20f;    // Her parçanın genişliği (birim)
        public int segments = 64;         // Parça başına mesh çözünürlüğü (yüksek = engebeler net)
        public float viewBehind = 25f;    // Oyuncunun bu kadar arkasındaki parça geri dönüştürülür

        [Header("Pickup Prefab'ları (Editor atar)")]
        public GameObject coinPrefab;
        public GameObject fuelCanPrefab;

        [Header("Pickup Yoğunluğu")]
        public int maxCoinClustersPerChunk = 2;
        public int coinsPerCluster = 5;
        [Range(0f, 1f)] public float fuelCanChance = 0.22f;

        private Transform _player;
        private readonly List<TerrainChunk> _chunks = new List<TerrainChunk>();
        private float _spawnX;

        private ObjectPool _coinPool;
        private ObjectPool _fuelPool;
        private Transform _pickupRoot;
        private PhysicsMaterial2D _groundMaterial;
        private Material _meshMaterial;
        private float _terrainAmp = 1f;   // Seçili haritanın arazi engebe çarpanı

        private void Awake()
        {
            // Seçili haritayı uygula (renkler + arazi karakteri) - tüm Start'lardan önce.
            int mapIdx = SaveSystem.SelectedMap;
            Biome.SetActiveMap(mapIdx);
            _terrainAmp = MapCatalog.Get(mapIdx).terrainAmp;

            // Zemin için sürtünmeli fizik materyali (lastik upgrade'i bunu değiştirir)
            _groundMaterial = new PhysicsMaterial2D("GroundMat") { friction = 2f, bounciness = 0f };
            _meshMaterial = new Material(Shader.Find("Sprites/Default"));

            // Pickup havuzları
            _pickupRoot = new GameObject("Pickups").transform;
            _pickupRoot.SetParent(transform, false);
            if (coinPrefab != null) _coinPool = new ObjectPool(coinPrefab, _pickupRoot, 24);
            if (fuelCanPrefab != null) _fuelPool = new ObjectPool(fuelCanPrefab, _pickupRoot, 4);
        }

        private void Start()
        {
            var vehicle = FindObjectOfType<VehicleController>();
            if (vehicle != null) _player = vehicle.transform;

            // İlk parçaları sırayla oluştur.
            // Aracın başlangıç noktasının (x=0) SOLUNDAN başlat ki her iki tekerlek de
            // zeminin üstünde olsun (araç kenardan boşluğa düşmesin).
            float startX = -2f * chunkWidth;
            for (int i = 0; i < chunkCount; i++)
                _chunks.Add(CreateChunk(startX + i * chunkWidth));
            _spawnX = startX + chunkCount * chunkWidth;
        }

        private void Update()
        {
            if (_player == null) return;

            // Oyuncunun gerisinde kalan parçayı en öne taşı (recycle)
            for (int i = 0; i < _chunks.Count; i++)
            {
                TerrainChunk chunk = _chunks[i];
                if (chunk.XEnd < _player.position.x - viewBehind)
                {
                    RecycleChunkPickups(chunk);
                    chunk.Build(_spawnX, chunkWidth, segments, Height,
                                Biome.Evaluate(_spawnX), _groundMaterial);
                    PopulateChunk(chunk);
                    _spawnX += chunkWidth;
                }
            }
        }

        // ---------------- Yükseklik fonksiyonu (dünya X'inin saf fonksiyonu) ----------------
        // Saf fonksiyon olması parçaların kusursuz birleşmesini sağlar.
        public float Height(float x)
        {
            const float flatZone = 16f; // İlk metreler düz (güvenli başlangıç)
            if (x < flatZone) return 0f;

            float xx = x - flatZone;
            float h = 0f;
            // Büyük tepeler (tırmanış zorluğu) - uzun dalga boyu, dik değil
            h += Mathf.Sin(xx * 0.05f + 0.6f) * 4.0f;   // uzun, yüksek tepeler
            h += Mathf.Sin(xx * 0.11f + 1.3f) * 2.3f;   // orta dalgalanma
            h += Mathf.Sin(xx * 0.21f) * 1.1f;          // küçük tümsekler
            // Rastgele büyük arazi değişimi + ölçülü engebe (pürüz, fırlatmaz)
            h += (Mathf.PerlinNoise(xx * 0.028f, 12.5f) - 0.5f) * 10.5f;
            h += (Mathf.PerlinNoise(xx * 0.11f, 4.2f) - 0.5f) * 2.0f;   // engebe/bumpiness (yumuşak)

            // Mesafeyle belirgin ama daha kademeli zorlaşır
            float difficulty = 1f + Mathf.Clamp01(xx / 1000f) * 1.35f;
            float ease = Mathf.Clamp01(xx / 45f);       // yumuşak giriş
            return h * difficulty * ease * _terrainAmp; // harita engebe çarpanı
        }

        // ---------------- Parça oluşturma ----------------
        private TerrainChunk CreateChunk(float xStart)
        {
            var go = new GameObject("TerrainChunk");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _meshMaterial;
            mr.sortingOrder = -5;
            go.AddComponent<EdgeCollider2D>();

            var line = go.AddComponent<LineRenderer>();
            line.material = _meshMaterial;
            line.widthMultiplier = 0.35f;
            line.numCapVertices = 2;
            line.useWorldSpace = false;
            line.sortingOrder = -4;

            var chunk = go.AddComponent<TerrainChunk>();
            chunk.Build(xStart, chunkWidth, segments, Height, Biome.Evaluate(xStart), _groundMaterial);
            PopulateChunk(chunk);
            return chunk;
        }

        // ---------------- Pickup yerleştirme ----------------
        private void PopulateChunk(TerrainChunk chunk)
        {
            // İlk parçada (spawn bölgesi) pickup koymayalım
            if (chunk.XStart < chunkWidth) return;

            // Deterministik: aynı X aynı içerik (recycle tutarlılığı için)
            var rng = new System.Random(Mathf.RoundToInt(chunk.XStart));

            // Coin kümeleri
            int clusters = rng.Next(0, maxCoinClustersPerChunk + 1);
            for (int c = 0; c < clusters && _coinPool != null; c++)
            {
                float startLocal = (float)rng.NextDouble() * (chunkWidth - coinsPerCluster * 1.2f - 2f) + 1f;
                for (int k = 0; k < coinsPerCluster; k++)
                {
                    float localX = startLocal + k * 1.2f;
                    float worldX = chunk.XStart + localX;
                    float y = Height(worldX) + 1.7f;
                    SpawnPickup(_coinPool, chunk, new Vector3(worldX, y, 0f));
                }
            }

            // Yakıt bidonu
            if (_fuelPool != null && rng.NextDouble() < fuelCanChance)
            {
                float localX = (float)rng.NextDouble() * (chunkWidth - 2f) + 1f;
                float worldX = chunk.XStart + localX;
                float y = Height(worldX) + 1.4f;
                SpawnPickup(_fuelPool, chunk, new Vector3(worldX, y, 0f));
            }
        }

        private void SpawnPickup(ObjectPool pool, TerrainChunk chunk, Vector3 pos)
        {
            var go = pool.Get(pos);
            go.transform.SetParent(chunk.transform, true);
            var pickup = go.GetComponent<Pickup>();
            if (pickup != null) pickup.Pool = pool;
        }

        /// <summary>Parçanın üzerinde toplanmamış pickup'ları havuza geri verir.</summary>
        private void RecycleChunkPickups(TerrainChunk chunk)
        {
            // Geriye doğru gez (Return reparent yapacağı için çocuk listesi değişir)
            for (int i = chunk.transform.childCount - 1; i >= 0; i--)
            {
                var child = chunk.transform.GetChild(i);
                var pickup = child.GetComponent<Pickup>();
                if (pickup != null && pickup.Pool != null)
                    pickup.Pool.Return(child.gameObject);
            }
        }
    }
}
