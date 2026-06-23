namespace TopClimbing
{
    /// <summary>
    /// Oyun genelinde kullanılan sabit değerler ve denge (balance) ayarları.
    /// Tek yerden yönetmek için burada toplanmıştır.
    /// </summary>
    public static class GameConfig
    {
        // --- Sahne isimleri (Build Settings ile birebir aynı olmalı) ---
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        // --- Yakıt ---
        public const float StartFuel = 100f;          // Başlangıç yakıtı
        public const float MaxFuel = 100f;             // Maksimum yakıt
        public const float IdleFuelPerSecond = 0.6f;   // Boştayken yakıt tüketimi
        public const float GasFuelPerSecond = 4.5f;    // Gaz verildiğinde ek tüketim
        public const float FuelCanAmount = 35f;        // Bir bidonun doldurduğu yakıt

        // --- Devrilme / düşme (Game Over) ---
        public const float FlipAngleThreshold = 90f;    // Bu açıdan fazla yatınca (yan/ters) "devrildi" sayılır
        public const float FlipTimeToGameOver = 0f;     // Kafa zemine değdiği İLK kare oyun biter (anında)

        // --- Coin ---
        public const int CoinValue = 1;                 // Bir coin'in değeri

        // --- Mesafe ---
        public const float DistanceUnitsToMeters = 1f;  // Dünya birimi -> metre çevrimi

        // --- Biome (arka plan) geçiş mesafeleri (metre) ---
        public const float Biome1End = 400f;   // Çiçekli yeşil bölge
        public const float Biome2End = 900f;   // Gökyüzü / bulutlu bölge
        // 900m sonrası: volkanik bölge
    }

    /// <summary>
    /// PlayerPrefs anahtarlarının merkezi listesi. Yazım hatalarını önler.
    /// </summary>
    public static class SaveKeys
    {
        public const string TotalCoins = "tc_total_coins";
        public const string SelectedVehicle = "tc_selected_vehicle";
        public const string OwnedVehiclePrefix = "tc_owned_vehicle_"; // + index
        public const string SelectedMap = "tc_selected_map";
        public const string OwnedMapPrefix = "tc_owned_map_";          // + index
        public const string UpgradePrefix = "tc_upgrade_";            // + UpgradeType
        public const string MusicOn = "tc_music_on";
        public const string SfxOn = "tc_sfx_on";
        public const string BestDistance = "tc_best_distance";
    }
}
