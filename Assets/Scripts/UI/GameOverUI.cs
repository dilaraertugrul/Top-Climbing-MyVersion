using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Game Over paneli: gidilen mesafe ve kazanılan coin gösterilir.
    /// Yeniden Başlat, Upgrade ve Ana Menü butonları bulunur.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private GameObject _panel;

        private void Start()
        {
            Transform root = transform;
            _panel = UIHelper.FindDeep(root, "GameOverPanel")?.gameObject;

            UIHelper.Bind(root, "RestartButton", Restart);
            UIHelper.Bind(root, "UpgradeButton", GoUpgrade);
            UIHelper.Bind(root, "GameOverMenuButton", GoMenu);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += Show;

            if (_panel != null) _panel.SetActive(false);
        }

        private void Show()
        {
            if (_panel != null) _panel.SetActive(true);

            var gm = GameManager.Instance;
            if (gm == null) return;
            UIHelper.SetText(transform, "GO_DistanceText", "Mesafe: " + Mathf.FloorToInt(gm.Distance) + " m");
            UIHelper.SetText(transform, "GO_CoinText", "Coin: +" + gm.RunCoins);
            UIHelper.SetText(transform, "GO_BestText", "Rekor: " + Mathf.FloorToInt(SaveSystem.BestDistance) + " m");
        }

        private void Restart()
        {
            if (GameManager.Instance != null) GameManager.Instance.Restart();
        }

        private void GoUpgrade()
        {
            // Ana menüye dön ve upgrade panelinin açılmasını işaretle
            PlayerPrefs.SetInt("tc_open_upgrade", 1);
            PlayerPrefs.Save();
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }

        private void GoMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= Show;
        }
    }
}
