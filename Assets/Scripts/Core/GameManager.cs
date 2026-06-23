using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopClimbing
{
    public enum GameState { Playing, Paused, GameOver }

    /// <summary>
    /// Oyun (Game) sahnesinin merkezi yöneticisi.
    /// Oyun durumunu, mesafeyi, koşulan tur coin'lerini ve oyun sonu koşullarını yönetir.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Sahne Referansları (otomatik bulunur)")]
        public VehicleController player;
        public FuelSystem fuel;

        public GameState State { get; private set; } = GameState.Playing;

        // Tur istatistikleri
        public int RunCoins { get; private set; }
        public float Distance { get; private set; }   // metre

        private float _startX;
        private float _flipTimer;

        // --- Olaylar (UI bunlara abone olur) ---
        public event Action<int> OnCoinsChanged;
        public event Action<float> OnDistanceChanged;
        public event Action OnGameOver;
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (player == null) player = FindObjectOfType<VehicleController>();
            if (fuel == null && player != null) fuel = player.GetComponent<FuelSystem>();

            if (player != null) _startX = player.transform.position.x;

            // Seçili araç bilgisini yükle ve aracı kur
            if (player != null)
                player.Setup(VehicleCatalog.Get(SaveSystem.SelectedVehicle));

            // Yakıt bitti olayına abone ol
            if (fuel != null) fuel.OnFuelEmpty += HandleFuelEmpty;

            OnCoinsChanged?.Invoke(RunCoins);
            OnDistanceChanged?.Invoke(Distance);
        }

        private void Update()
        {
            if (State != GameState.Playing || player == null) return;

            // --- Mesafe takibi (yalnızca ileri gidiş sayılır) ---
            float d = (player.transform.position.x - _startX) * GameConfig.DistanceUnitsToMeters;
            if (d > Distance)
            {
                Distance = d;
                OnDistanceChanged?.Invoke(Distance);
            }

            // --- Kafa yere değme kontrolü ---
            // SADECE sürücünün kafası katı zemine değince oyun biter; aksi halde
            // (araç yan yatsa, takla atsa bile kafa değmediği sürece) yarış devam eder.
            if (player.IsHeadTouchingGround())
            {
                _flipTimer += Time.deltaTime;
                if (_flipTimer >= GameConfig.FlipTimeToGameOver)
                    TriggerGameOver("Sürücünün kafası yere değdi!");
            }
            else
            {
                _flipTimer = 0f;
            }
        }

        // ---------------- Coin ----------------
        public void CollectCoin(int value)
        {
            RunCoins += value;
            OnCoinsChanged?.Invoke(RunCoins);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        }

        // ---------------- Yakıt ----------------
        public void RefuelPickup(float amount)
        {
            if (fuel != null) fuel.AddFuel(amount);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFuel();
        }

        private void HandleFuelEmpty()
        {
            TriggerGameOver("Yakıt bitti!");
        }

        // ---------------- Oyun sonu ----------------
        public void TriggerGameOver(string reason)
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;

            // Tur coin'lerini toplam coin'e ekle ve rekoru kaydet
            SaveSystem.AddCoins(RunCoins);
            SaveSystem.BestDistance = Distance;

            if (player != null) player.OnGameOver();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopEngine();
                AudioManager.Instance.PlayCrash();
            }

            Debug.Log($"[GameManager] Oyun bitti: {reason} | Mesafe: {Distance:0}m | Coin: {RunCoins}");
            OnGameOver?.Invoke();
            OnStateChanged?.Invoke(State);
        }

        // ---------------- Pause ----------------
        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (AudioManager.Instance != null) AudioManager.Instance.StopEngine();
            OnStateChanged?.Invoke(State);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            OnStateChanged?.Invoke(State);
        }

        // ---------------- Sahne geçişleri ----------------
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConfig.GameScene);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConfig.MainMenuScene);
        }

        private void OnDestroy()
        {
            if (fuel != null) fuel.OnFuelEmpty -= HandleFuelEmpty;
            Time.timeScale = 1f;
        }
    }
}
