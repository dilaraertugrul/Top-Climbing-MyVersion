namespace TopClimbing
{
    /// <summary>Geliştirilebilir araç bileşenleri.</summary>
    public enum UpgradeType
    {
        Engine,       // Motor  -> motor torku / hız
        Suspension,   // Süspansiyon -> yumuşaklık / denge
        Tires         // Lastik -> tutuş (sürtünme)
    }

    /// <summary>
    /// Upgrade seviyelerini, maliyetlerini ve fizik etkilerini tanımlar.
    /// Seviyeler PlayerPrefs ile saklanır (bkz. SaveSystem).
    /// </summary>
    public static class UpgradeCatalog
    {
        public const int MaxLevel = 5;

        /// <summary>Belirtilen tipte, belirtilen mevcut seviyeden bir sonraki seviyenin maliyeti.</summary>
        public static int GetCost(UpgradeType type, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return -1; // Maksimuma ulaşıldı
            // Basit artan maliyet eğrisi.
            int baseCost;
            switch (type)
            {
                case UpgradeType.Engine: baseCost = 300; break;
                case UpgradeType.Suspension: baseCost = 250; break;
                case UpgradeType.Tires: baseCost = 200; break;
                default: baseCost = 250; break;
            }
            return baseCost * (currentLevel + 1);
        }

        public static string DisplayName(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Engine: return "Motor";
                case UpgradeType.Suspension: return "Süspansiyon";
                case UpgradeType.Tires: return "Lastik";
                default: return type.ToString();
            }
        }

        // --- Fizik etkileri (0. seviye = etkisiz, çarpan 1.0) ---

        /// <summary>Motor seviyesi -> tork & hız çarpanı.</summary>
        public static float EngineMultiplier(int level)
        {
            return 1f + 0.18f * level; // her seviye %18 güç
        }

        /// <summary>Süspansiyon seviyesi -> yay frekansı (yumuşaklık).</summary>
        public static float SuspensionFrequency(int level)
        {
            return 4.5f + 0.6f * level; // daha yüksek seviye = daha sağlam/duyarlı yay
        }

        /// <summary>Lastik seviyesi -> sürtünme (tutuş) çarpanı.</summary>
        public static float TireFriction(int level)
        {
            return 1.5f + 0.5f * level;
        }
    }
}
