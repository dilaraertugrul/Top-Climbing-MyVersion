using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Sahneler arası kalıcı (DontDestroyOnLoad) ses yöneticisi.
    /// - Ana menüde müzikleri playlist mantığıyla sırayla çalar.
    /// - Buton / coin / yakıt / çarpışma efektlerini yönetir.
    /// - Motor sesini araç hızına göre pitch'ler.
    /// - Müzik ve efekt aç/kapa tercihlerini SaveSystem ile uygular.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _engineSource;

        private readonly List<AudioClip> _playlist = new List<AudioClip>();
        private int _trackIndex;

        private AudioClip _coinClip, _buttonClip, _fuelClip, _crashClip;

        private void Awake()
        {
            // Tekil (singleton) garantisi
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildSources();
            BuildClips();
            ApplyPreferences();
        }

        private void BuildSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = false;        // Parça bitince sıradakine geçeceğiz
            _musicSource.playOnAwake = false;
            _musicSource.volume = 0.5f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.loop = true;
            _engineSource.playOnAwake = false;
            _engineSource.volume = 0f;
        }

        private void BuildClips()
        {
            // --- Müzik playlist'i (melodi + bas, akor ilerlemeli; dolgun ve neşeli) ---
            // Meadow: C - G - Am - F (parlak majör)
            _playlist.Add(ProceduralAudio.BuildSong("Track_Meadow",
                new[] { "E4","G4","C5","G4", "D4","G4","B4","G4", "C4","E4","A4","E4", "C4","F4","A4","F4" },
                new[] { "C3","C3","G3","G3","A3","A3","F3","F3" }, 122f));
            // Sky: Am - F - C - G (havadar)
            _playlist.Add(ProceduralAudio.BuildSong("Track_Sky",
                new[] { "A4","C5","E5","C5", "A4","C5","F5","A4", "E4","G4","C5","G4", "D4","G4","B4","D5" },
                new[] { "A3","A3","F3","F3","C3","C3","G3","G3" }, 134f));
            // Volcano: Am - G - F - E (dramatik / Endülüs)
            _playlist.Add(ProceduralAudio.BuildSong("Track_Volcano",
                new[] { "A4","E4","C5","A4", "G4","D4","B4","G4", "F4","C4","A4","F4", "E4","B4","E4","B4" },
                new[] { "A2","A2","G2","G2","F2","F2","E2","E2" }, 110f));

            // --- Efektler ---
            _coinClip = ProceduralAudio.BuildBlip("Sfx_Coin", 880f, 1320f, 0.12f);
            _buttonClip = ProceduralAudio.BuildBlip("Sfx_Button", 660f, 440f, 0.08f);
            _fuelClip = ProceduralAudio.BuildBlip("Sfx_Fuel", 330f, 660f, 0.25f);
            _crashClip = ProceduralAudio.BuildBlip("Sfx_Crash", 200f, 60f, 0.4f, 0.5f);

            // --- Motor ---
            _engineSource.clip = ProceduralAudio.BuildEngineLoop("Sfx_Engine");
        }

        private void Update()
        {
            // Playlist: müzik kaynağı durduysa ve müzik açıksa sıradaki parçayı başlat.
            if (SaveSystem.MusicOn && _playlist.Count > 0 && !_musicSource.isPlaying && _musicSource.clip != null)
            {
                NextTrack();
            }
        }

        // ---------------- Müzik ----------------
        public void StartPlaylist()
        {
            if (_playlist.Count == 0) return;
            _trackIndex = 0;
            _musicSource.clip = _playlist[_trackIndex];
            if (SaveSystem.MusicOn) _musicSource.Play();
        }

        private void NextTrack()
        {
            _trackIndex = (_trackIndex + 1) % _playlist.Count;
            _musicSource.clip = _playlist[_trackIndex];
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        // ---------------- Efektler ----------------
        public void PlayCoin() => PlaySfx(_coinClip);
        public void PlayButton() => PlaySfx(_buttonClip);
        public void PlayFuel() => PlaySfx(_fuelClip);
        public void PlayCrash() => PlaySfx(_crashClip);

        private void PlaySfx(AudioClip clip)
        {
            if (!SaveSystem.SfxOn || clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        // ---------------- Motor ----------------
        /// <summary>Motor sesini gaz miktarına (0..1) ve hıza göre ayarlar.</summary>
        public void UpdateEngine(float throttle01, float speed01)
        {
            if (!SaveSystem.SfxOn)
            {
                if (_engineSource.isPlaying) _engineSource.Stop();
                return;
            }
            if (!_engineSource.isPlaying) _engineSource.Play();
            float target = Mathf.Lerp(0.04f, 0.22f, Mathf.Clamp01(throttle01 * 0.6f + speed01 * 0.4f));
            _engineSource.volume = Mathf.Lerp(_engineSource.volume, target, Time.deltaTime * 6f);
            _engineSource.pitch = Mathf.Lerp(0.7f, 2.2f, Mathf.Clamp01(speed01));
        }

        public void StopEngine()
        {
            if (_engineSource != null) _engineSource.Stop();
        }

        // ---------------- Tercihler ----------------
        public void ApplyPreferences()
        {
            if (!SaveSystem.MusicOn) _musicSource.Stop();
            if (!SaveSystem.SfxOn && _engineSource != null) _engineSource.Stop();
        }

        public void SetMusicEnabled(bool on)
        {
            SaveSystem.MusicOn = on;
            if (on)
            {
                if (_musicSource.clip == null) StartPlaylist();
                else _musicSource.Play();
            }
            else _musicSource.Stop();
        }

        public void SetSfxEnabled(bool on)
        {
            SaveSystem.SfxOn = on;
            if (!on && _engineSource != null) _engineSource.Stop();
        }
    }
}
