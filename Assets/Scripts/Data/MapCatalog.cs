using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Satın alınabilir bir harita (tema): oyun içi renk paleti + arazi engebe hissi.
    /// Seçili harita oyun başında uygulanır (Biome + TerrainGenerator).
    /// </summary>
    [System.Serializable]
    public class MapDefinition
    {
        public string id;
        public string displayName;
        public int price;
        public BiomePalette palette;   // gökyüzü/zemin/tepe renkleri
        public float terrainAmp = 1f;  // arazi engebe çarpanı (zorluk/karakter)
    }

    /// <summary>
    /// Oyundaki tüm haritaların sabit listesi (kod tarafında; asset bağı kopmaz).
    /// 0. harita ücretsiz ve daima sahiplenilmiştir.
    /// </summary>
    public static class MapCatalog
    {
        private static MapDefinition[] _maps;

        public static MapDefinition[] All
        {
            get
            {
                if (_maps == null) _maps = Build();
                return _maps;
            }
        }

        public static int Count => All.Length;

        public static MapDefinition Get(int index)
        {
            var all = All;
            if (index < 0 || index >= all.Length) index = 0;
            return all[index];
        }

        private static MapDefinition[] Build()
        {
            return new[]
            {
                new MapDefinition
                {
                    id = "meadow", displayName = "Yeşil Vadi", price = 0, terrainAmp = 0.9f,
                    palette = new BiomePalette
                    {
                        skyTop = new Color(0.55f, 0.80f, 0.98f), skyBottom = new Color(0.85f, 0.95f, 0.80f),
                        ground = new Color(0.45f, 0.70f, 0.30f), groundTop = new Color(0.30f, 0.55f, 0.18f),
                        hill = new Color(0.60f, 0.78f, 0.45f)
                    }
                },
                new MapDefinition
                {
                    id = "snow", displayName = "Karlı Zirve", price = 2000, terrainAmp = 1.05f,
                    palette = new BiomePalette
                    {
                        skyTop = new Color(0.40f, 0.55f, 0.75f), skyBottom = new Color(0.80f, 0.88f, 0.95f),
                        ground = new Color(0.82f, 0.88f, 0.95f), groundTop = new Color(0.96f, 0.98f, 1f),
                        hill = new Color(0.60f, 0.70f, 0.85f)
                    }
                },
                new MapDefinition
                {
                    id = "desert", displayName = "Çöl Kumulları", price = 3500, terrainAmp = 1.0f,
                    palette = new BiomePalette
                    {
                        skyTop = new Color(0.95f, 0.75f, 0.45f), skyBottom = new Color(0.98f, 0.90f, 0.62f),
                        ground = new Color(0.90f, 0.72f, 0.40f), groundTop = new Color(0.80f, 0.56f, 0.26f),
                        hill = new Color(0.85f, 0.65f, 0.40f)
                    }
                },
                new MapDefinition
                {
                    id = "volcano", displayName = "Volkan", price = 5000, terrainAmp = 1.2f,
                    palette = new BiomePalette
                    {
                        skyTop = new Color(0.35f, 0.12f, 0.12f), skyBottom = new Color(0.85f, 0.40f, 0.15f),
                        ground = new Color(0.22f, 0.16f, 0.16f), groundTop = new Color(0.85f, 0.30f, 0.10f),
                        hill = new Color(0.35f, 0.22f, 0.22f)
                    }
                },
                new MapDefinition
                {
                    id = "night", displayName = "Ay Işığı", price = 7000, terrainAmp = 1.1f,
                    palette = new BiomePalette
                    {
                        skyTop = new Color(0.06f, 0.07f, 0.16f), skyBottom = new Color(0.18f, 0.20f, 0.34f),
                        ground = new Color(0.14f, 0.16f, 0.24f), groundTop = new Color(0.32f, 0.37f, 0.52f),
                        hill = new Color(0.12f, 0.14f, 0.24f)
                    }
                },
            };
        }
    }
}
