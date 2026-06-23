using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Upgrade ekranı: Motor, Süspansiyon ve Lastik geliştirmeleri.
    /// Coin ile satın alınır, seviyeler PlayerPrefs ile saklanır.
    /// Yetersiz coin veya maksimum seviye durumunda toast uyarısı gösterir.
    /// </summary>
    public class UpgradeUI : MonoBehaviour
    {
        private Text _coinText;

        // Her geliştirme için UI isim önekleri
        private static readonly (UpgradeType type, string prefix)[] Rows =
        {
            (UpgradeType.Engine, "Engine"),
            (UpgradeType.Suspension, "Suspension"),
            (UpgradeType.Tires, "Tires"),
        };

        private void Awake()
        {
            Transform root = transform;
            _coinText = UIHelper.FindComp<Text>(root, "UpgradeCoinText");

            foreach (var row in Rows)
            {
                var type = row.type; // closure güvenliği
                UIHelper.Bind(root, row.prefix + "BuyButton", () => Buy(type));
            }
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            Transform root = transform;
            if (_coinText != null) _coinText.text = "Coin: " + SaveSystem.TotalCoins;

            foreach (var row in Rows)
            {
                int level = SaveSystem.GetUpgradeLevel(row.type);
                int cost = UpgradeCatalog.GetCost(row.type, level);

                UIHelper.SetText(root, row.prefix + "LevelText", "Lv " + level + "/" + UpgradeCatalog.MaxLevel);
                UIHelper.SetText(root, row.prefix + "CostText", cost < 0 ? "MAKS" : cost + " coin");

                // Seviye göstergesi (varsa slider)
                var bar = UIHelper.FindComp<Slider>(root, row.prefix + "LevelBar");
                if (bar != null) bar.value = (float)level / UpgradeCatalog.MaxLevel;
            }
        }

        private void Buy(UpgradeType type)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();

            int level = SaveSystem.GetUpgradeLevel(type);
            int cost = UpgradeCatalog.GetCost(type, level);

            if (cost < 0)
            {
                Toast("Maksimum seviye!");
                return;
            }
            if (!SaveSystem.TrySpendCoins(cost))
            {
                Toast("Yetersiz coin!");
                return;
            }

            SaveSystem.SetUpgradeLevel(type, level + 1);
            Toast(UpgradeCatalog.DisplayName(type) + " geliştirildi!");
            Refresh();
        }

        private void Toast(string msg)
        {
            if (ToastMessage.Instance != null) ToastMessage.Instance.Show(msg);
        }
    }
}
