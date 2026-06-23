using System;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Aracın yakıt miktarını yönetir. Tüketim VehicleController tarafından,
    /// dolum ise yakıt bidonları tarafından tetiklenir.
    /// </summary>
    public class FuelSystem : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Max { get; private set; } = GameConfig.MaxFuel;

        /// <summary>0..1 aralığında doluluk oranı (HUD için).</summary>
        public float Fuel01 => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;
        public bool HasFuel => Current > 0f;

        public event Action<float> OnFuelChanged;  // 0..1
        public event Action OnFuelEmpty;

        private bool _emptyNotified;

        private void Awake()
        {
            Max = GameConfig.MaxFuel;
            Current = GameConfig.StartFuel;
            _emptyNotified = false;
        }

        private void Start()
        {
            OnFuelChanged?.Invoke(Fuel01);
        }

        /// <summary>Belirtilen miktarda yakıt tüketir.</summary>
        public void Consume(float amount)
        {
            if (Current <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            OnFuelChanged?.Invoke(Fuel01);

            if (Current <= 0f && !_emptyNotified)
            {
                _emptyNotified = true;
                OnFuelEmpty?.Invoke();
            }
        }

        /// <summary>Yakıt bidonu toplandığında çağrılır.</summary>
        public void AddFuel(float amount)
        {
            Current = Mathf.Min(Max, Current + amount);
            if (Current > 0f) _emptyNotified = false;
            OnFuelChanged?.Invoke(Fuel01);
        }
    }
}
