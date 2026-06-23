using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Gaz ve fren (geri) girdisini tek noktada toplar.
    /// Hem klavye (Editor testi) hem de ekran butonları (mobil) desteklenir.
    /// Araç bu sınıftan Gas / Brake değerlerini okur.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Gaz Rampası")]
        [Tooltip("Basılı tutarken gazın 0'dan 1'e ulaşma süresi (saniye). Büyük değer = daha yumuşak ivmelenme.")]
        [SerializeField] private float gasRampUpTime = 0.8f;
        [Tooltip("Bırakınca gazın 1'den 0'a düşme süresi (saniye).")]
        [SerializeField] private float gasReleaseTime = 0.15f;

        private bool _mobileGas;
        private bool _mobileBrake;

        /// <summary>Gaz girdisi (0..1, basılı tutarken kademeli artar).</summary>
        public float Gas { get; private set; }

        /// <summary>Fren / geri girdisi (0..1, basılı tutarken kademeli artar).</summary>
        public float Brake { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // Klavye: sağ ok / D = gaz, sol ok / A = fren-geri
            bool keyGas = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            bool keyBrake = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);

            Gas = Ramp(Gas, keyGas || _mobileGas);
            Brake = Ramp(Brake, keyBrake || _mobileBrake);
        }

        // Basılıyken hedef 1'e doğru yavaşça, bırakınca 0'a doğru hızlıca ilerler.
        private float Ramp(float current, bool held)
        {
            float target = held ? 1f : 0f;
            float duration = held ? gasRampUpTime : gasReleaseTime;
            if (duration <= 0f) return target;
            return Mathf.MoveTowards(current, target, Time.deltaTime / duration);
        }

        // --- Ekran butonları bu metotları çağırır (MobileButton) ---
        public void SetGas(bool held) => _mobileGas = held;
        public void SetBrake(bool held) => _mobileBrake = held;

        private void OnDisable()
        {
            _mobileGas = false;
            _mobileBrake = false;
        }
    }
}
