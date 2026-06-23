using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Pause paneli: Devam Et, Ayarlar ve Ana Menü.
    /// GameManager durum değişimine abone olarak otomatik açılır/kapanır.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private GameObject _panel;
        private SettingsUI _settings;

        private void Start()
        {
            Transform root = transform;
            _panel = UIHelper.FindDeep(root, "PausePanel")?.gameObject;
            var settingsTr = UIHelper.FindDeep(root, "SettingsPanel");
            if (settingsTr != null) _settings = settingsTr.GetComponent<SettingsUI>();

            UIHelper.Bind(root, "ResumeButton", Resume);
            UIHelper.Bind(root, "PauseSettingsButton", OpenSettings);
            UIHelper.Bind(root, "PauseMenuButton", GoMenu);

            if (_settings != null)
            {
                _settings.OnClose = () => { if (_panel != null) _panel.SetActive(true); };
                _settings.gameObject.SetActive(false);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleState;

            if (_panel != null) _panel.SetActive(false);
        }

        private void HandleState(GameState state)
        {
            if (_panel != null) _panel.SetActive(state == GameState.Paused);
        }

        private void Resume()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        private void OpenSettings()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_settings != null) { _settings.gameObject.SetActive(true); _settings.Refresh(); }
        }

        private void GoMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleState;
        }
    }
}
