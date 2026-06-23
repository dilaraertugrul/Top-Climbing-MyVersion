using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>Tek bir paralaks arka plan katmanı.</summary>
    [System.Serializable]
    public class ParallaxLayer
    {
        public SpriteRenderer renderer;
        [Tooltip("0 = kamerayla kilitli (en uzak/en yavaş), 1 = oyuncuyla birebir (tam kayar)")]
        [Range(0f, 1f)] public float factor = 0.5f;
        public float tileWidth = 20f;
        public float baseY = 0f;
        public bool isHill = false; // true: biome 'hill' rengi, false: 'sky' tonu
        [Tooltip("Kameradan bağımsız kendi süzülme hızı (birim/sn). Bulutlar için >0.")]
        public float autoScrollSpeed = 0f;
    }

    /// <summary>
    /// Mesafeye göre biome rengini uygular ve paralaks katmanlarını sonsuz kaydırır.
    /// Kamera arka plan rengini gökyüzü tonuna ayarlar.
    /// </summary>
    public class BackgroundManager : MonoBehaviour
    {
        public Camera targetCamera;
        public List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private Transform _player;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            var v = FindObjectOfType<VehicleController>();
            if (v != null) _player = v.transform;

            ResizeLayersToCamera();
        }

        /// <summary>
        /// Döşeli (Tiled) katmanları kamera görüş genişliğine göre boyutlandırır.
        /// Genişlik tile'ın tam katı olduğundan tekrar kusursuz kalır; kenarda boşluk olmaz.
        /// </summary>
        private void ResizeLayersToCamera()
        {
            if (targetCamera == null) return;
            float viewWidth = 2f * targetCamera.orthographicSize * targetCamera.aspect;
            foreach (var layer in layers)
            {
                if (layer == null || layer.renderer == null) continue;
                if (layer.renderer.drawMode == SpriteDrawMode.Simple) continue;
                float tw = Mathf.Max(0.01f, layer.tileWidth);
                int n = Mathf.CeilToInt(viewWidth / tw) + 2; // görüşten geniş + tampon
                layer.renderer.size = new Vector2(tw * n, layer.renderer.size.y);
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            float distance = _player != null ? _player.position.x : targetCamera.transform.position.x;
            BiomePalette palette = Biome.Evaluate(distance);

            // Gökyüzü: kamera arka plan rengi
            targetCamera.backgroundColor = palette.skyTop;

            float camX = targetCamera.transform.position.x;
            float camY = targetCamera.transform.position.y;

            foreach (var layer in layers)
            {
                if (layer == null || layer.renderer == null) continue;

                // Renk: tepe silüetleri biome 'hill', diğerleri gökyüzü alt tonu
                layer.renderer.color = layer.isHill ? palette.hill : palette.skyBottom;

                // Sonsuz paralaks: katman, ekranda kamera hızının 'factor' katı kadar kayar.
                // factor=0 -> kamerayla kilitli (uzak), factor=1 -> dünyaya kilitli (tam kayar).
                // autoScroll: katmana kameradan bağımsız sürekli süzülme ekler (bulutlar).
                // Yalnızca tileWidth'in kalanı (mod) görünür -> dikişsiz, kusursuz tekrar.
                float tw = Mathf.Max(0.01f, layer.tileWidth);
                float auto = layer.autoScrollSpeed * Time.time;
                float shift = Mathf.Repeat(camX * layer.factor + auto, tw);
                float x = camX - shift;
                layer.renderer.transform.position = new Vector3(x, camY + layer.baseY, layer.renderer.transform.position.z);
            }
        }
    }
}
