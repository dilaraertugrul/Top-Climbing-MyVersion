using UnityEngine;

namespace TopClimbing
{
    /// <summary>Bir bölgenin (biome) renk paleti.</summary>
    public struct BiomePalette
    {
        public Color skyTop;
        public Color skyBottom;
        public Color ground;
        public Color groundTop; // yüzey / çim çizgisi
        public Color hill;      // uzak tepe silüeti
    }

    /// <summary>
    /// Oyun içi renkleri SEÇİLİ HARİTAYA göre sağlar. Harita oyun başında
    /// (TerrainGenerator.Awake) ayarlanır ve paleti önbelleğe alınır.
    /// </summary>
    public static class Biome
    {
        private static int _activeMap = -1;
        private static BiomePalette _active;

        /// <summary>Aktif haritayı (ve paletini) ayarlar. Sahne başında çağrılır.</summary>
        public static void SetActiveMap(int index)
        {
            _activeMap = index;
            _active = MapCatalog.Get(index).palette;
        }

        /// <summary>Seçili haritanın paletini döner (mesafe parametresi uyumluluk için tutuldu).</summary>
        public static BiomePalette Evaluate(float distance)
        {
            if (_activeMap < 0) SetActiveMap(SaveSystem.SelectedMap);
            return _active;
        }
    }
}
