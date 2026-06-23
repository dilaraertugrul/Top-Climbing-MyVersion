using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopClimbing
{
    /// <summary>
    /// Ana menü yöneticisi. Panel geçişlerini (Ana / Garaj / Upgrade / Ayarlar)
    /// yönetir ve Race ile oyunu başlatır. Açılışta müzik playlist'ini başlatır.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private GameObject _mainPanel;
        private GameObject _garagePanel;
        private GameObject _mapsPanel;
        private GameObject _upgradePanel;
        private SettingsUI _settings;

        private void Start()
        {
            Transform root = transform;
            _mainPanel = UIHelper.FindDeep(root, "MainPanel")?.gameObject;
            _garagePanel = UIHelper.FindDeep(root, "GaragePanel")?.gameObject;
            _mapsPanel = UIHelper.FindDeep(root, "MapsPanel")?.gameObject;
            _upgradePanel = UIHelper.FindDeep(root, "UpgradePanel")?.gameObject;
            var settingsTr = UIHelper.FindDeep(root, "SettingsPanel");
            if (settingsTr != null) _settings = settingsTr.GetComponent<SettingsUI>();

            UIHelper.Bind(root, "RaceButton", StartRace);
            UIHelper.Bind(root, "GarageButton", () => ShowOnly(_garagePanel));
            UIHelper.Bind(root, "MapsButton", () => ShowOnly(_mapsPanel));
            UIHelper.Bind(root, "UpgradeButton", () => ShowOnly(_upgradePanel));
            UIHelper.Bind(root, "SettingsButton", OpenSettings);

            // Panellerin "Geri" butonları
            UIHelper.Bind(root, "GarageBackButton", BackToMain);
            UIHelper.Bind(root, "MapsBackButton", BackToMain);
            UIHelper.Bind(root, "UpgradeBackButton", BackToMain);

            if (_settings != null)
            {
                _settings.OnClose = BackToMain;
                _settings.gameObject.SetActive(false);
            }

            // Müzik playlist'ini başlat
            if (AudioManager.Instance != null) AudioManager.Instance.StartPlaylist();

            // Game Over -> "Upgrade'e git" akışı
            if (PlayerPrefs.GetInt("tc_open_upgrade", 0) == 1)
            {
                PlayerPrefs.SetInt("tc_open_upgrade", 0);
                PlayerPrefs.Save();
                ShowOnly(_upgradePanel);
            }
            else
            {
                BackToMain();
            }
        }

        private void ShowOnly(GameObject panel)
        {
            if (_mainPanel != null) _mainPanel.SetActive(panel == _mainPanel);
            if (_garagePanel != null) _garagePanel.SetActive(panel == _garagePanel);
            if (_mapsPanel != null) _mapsPanel.SetActive(panel == _mapsPanel);
            if (_upgradePanel != null) _upgradePanel.SetActive(panel == _upgradePanel);
            if (_settings != null && panel != _settings.gameObject) _settings.gameObject.SetActive(false);
        }

        private void BackToMain()
        {
            ShowOnly(_mainPanel);
            UIHelper.SetText(transform, "MenuCoinText", "Coin: " + SaveSystem.TotalCoins);
        }

        private void OpenSettings()
        {
            ShowOnly(null);
            if (_settings != null) { _settings.gameObject.SetActive(true); _settings.Refresh(); }
        }

        private void StartRace()
        {
            SceneManager.LoadScene(GameConfig.GameScene);
        }
    }
}
