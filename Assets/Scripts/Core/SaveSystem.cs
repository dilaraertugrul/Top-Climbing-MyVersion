using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Tüm kalıcı verilerin (coin, sahip olunan/seçili araç, upgrade seviyeleri,
    /// ses ayarları, en iyi mesafe) PlayerPrefs üzerinden okunup yazıldığı statik katman.
    /// </summary>
    public static class SaveSystem
    {
        // ---------------- Coin ----------------
        public static int TotalCoins
        {
            get => PlayerPrefs.GetInt(SaveKeys.TotalCoins, 0);
            set { PlayerPrefs.SetInt(SaveKeys.TotalCoins, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static void AddCoins(int amount)
        {
            TotalCoins = TotalCoins + amount;
        }

        /// <summary>Yeterli coin varsa harcar ve true döner.</summary>
        public static bool TrySpendCoins(int amount)
        {
            if (TotalCoins < amount) return false;
            TotalCoins -= amount;
            return true;
        }

        // ---------------- Araçlar ----------------
        public static bool IsVehicleOwned(int index)
        {
            // 0 numaralı araç (başlangıç aracı) daima sahiplenilmiştir.
            if (index == 0) return true;
            return PlayerPrefs.GetInt(SaveKeys.OwnedVehiclePrefix + index, 0) == 1;
        }

        public static void SetVehicleOwned(int index, bool owned)
        {
            PlayerPrefs.SetInt(SaveKeys.OwnedVehiclePrefix + index, owned ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static int SelectedVehicle
        {
            get => PlayerPrefs.GetInt(SaveKeys.SelectedVehicle, 0);
            set { PlayerPrefs.SetInt(SaveKeys.SelectedVehicle, value); PlayerPrefs.Save(); }
        }

        // ---------------- Haritalar ----------------
        public static bool IsMapOwned(int index)
        {
            // 0 numaralı harita (başlangıç) daima sahiplenilmiştir.
            if (index == 0) return true;
            return PlayerPrefs.GetInt(SaveKeys.OwnedMapPrefix + index, 0) == 1;
        }

        public static void SetMapOwned(int index, bool owned)
        {
            PlayerPrefs.SetInt(SaveKeys.OwnedMapPrefix + index, owned ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static int SelectedMap
        {
            get => PlayerPrefs.GetInt(SaveKeys.SelectedMap, 0);
            set { PlayerPrefs.SetInt(SaveKeys.SelectedMap, value); PlayerPrefs.Save(); }
        }

        // ---------------- Upgrade ----------------
        public static int GetUpgradeLevel(UpgradeType type)
        {
            return PlayerPrefs.GetInt(SaveKeys.UpgradePrefix + (int)type, 0);
        }

        public static void SetUpgradeLevel(UpgradeType type, int level)
        {
            PlayerPrefs.SetInt(SaveKeys.UpgradePrefix + (int)type, Mathf.Clamp(level, 0, UpgradeCatalog.MaxLevel));
            PlayerPrefs.Save();
        }

        // ---------------- Ses ayarları ----------------
        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(SaveKeys.MusicOn, 1) == 1;
            set { PlayerPrefs.SetInt(SaveKeys.MusicOn, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SaveKeys.SfxOn, 1) == 1;
            set { PlayerPrefs.SetInt(SaveKeys.SfxOn, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // ---------------- Rekor mesafe ----------------
        public static float BestDistance
        {
            get => PlayerPrefs.GetFloat(SaveKeys.BestDistance, 0f);
            set { if (value > BestDistance) { PlayerPrefs.SetFloat(SaveKeys.BestDistance, value); PlayerPrefs.Save(); } }
        }

        /// <summary>Tüm kayıtları sıfırlar (test / "verileri sil" için).</summary>
        public static void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
