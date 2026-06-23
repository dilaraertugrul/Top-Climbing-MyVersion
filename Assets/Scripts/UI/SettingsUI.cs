using System;
using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Ayarlar paneli: müzik ve efekt seslerini açıp kapatır.
    /// Tercihler SaveSystem (PlayerPrefs) ile saklanır ve AudioManager'a uygulanır.
    /// Hem ana menüde hem pause ekranında kullanılır.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        private Toggle _musicToggle;
        private Toggle _sfxToggle;

        /// <summary>Geri/Kapat butonuna basıldığında çağrılır (parent ayarlar).</summary>
        public Action OnClose;

        private bool _wired;

        private void Awake()
        {
            Wire();
        }

        private void Wire()
        {
            if (_wired) return;
            Transform root = transform;

            _musicToggle = UIHelper.FindComp<Toggle>(root, "MusicToggle");
            _sfxToggle = UIHelper.FindComp<Toggle>(root, "SfxToggle");

            if (_musicToggle != null)
            {
                _musicToggle.onValueChanged.RemoveAllListeners();
                _musicToggle.onValueChanged.AddListener(v =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetMusicEnabled(v);
                    else SaveSystem.MusicOn = v;
                });
            }
            if (_sfxToggle != null)
            {
                _sfxToggle.onValueChanged.RemoveAllListeners();
                _sfxToggle.onValueChanged.AddListener(v =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.SetSfxEnabled(v);
                    else SaveSystem.SfxOn = v;
                    if (v && AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                });
            }

            UIHelper.Bind(root, "CloseButton", Close);
            _wired = true;
        }

        private void OnEnable()
        {
            Wire();
            Refresh();
        }

        /// <summary>Toggle durumlarını kayıtlı tercihlere göre günceller.</summary>
        public void Refresh()
        {
            if (_musicToggle != null) _musicToggle.SetIsOnWithoutNotify(SaveSystem.MusicOn);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(SaveSystem.SfxOn);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
    }
}
