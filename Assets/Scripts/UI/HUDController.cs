using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Oyun içi HUD: yakıt göstergesi, coin, mesafe, hız ve pause butonu.
    /// Değerleri her kare GameManager / FuelSystem'den okur.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Slider _fuelSlider;
        private Image _fuelFill;
        private Text _coinText;
        private Text _distanceText;
        private Text _speedText;

        private FuelSystem _fuel;
        private VehicleController _vehicle;

        private void Start()
        {
            Transform root = transform;
            _fuelSlider = UIHelper.FindComp<Slider>(root, "FuelBar");
            _coinText = UIHelper.FindComp<Text>(root, "CoinText");
            _distanceText = UIHelper.FindComp<Text>(root, "DistanceText");
            _speedText = UIHelper.FindComp<Text>(root, "SpeedText");
            if (_fuelSlider != null) _fuelFill = UIHelper.FindComp<Image>(_fuelSlider.transform, "Fill");

            UIHelper.Bind(root, "PauseButton", OnPause);

            if (GameManager.Instance != null)
            {
                _vehicle = GameManager.Instance.player;
                _fuel = GameManager.Instance.fuel;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            if (_fuel == null) _fuel = gm.fuel;
            if (_vehicle == null) _vehicle = gm.player;

            if (_fuelSlider != null && _fuel != null)
            {
                _fuelSlider.value = _fuel.Fuel01;
                if (_fuelFill != null)
                    _fuelFill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.3f), _fuel.Fuel01);
            }

            if (_coinText != null) _coinText.text = "Coin: " + gm.RunCoins;
            if (_distanceText != null) _distanceText.text = Mathf.FloorToInt(gm.Distance) + " m";
            if (_speedText != null && _vehicle != null) _speedText.text = Mathf.RoundToInt(_vehicle.SpeedKmh) + " km/s";
        }

        private void OnPause()
        {
            if (GameManager.Instance != null) GameManager.Instance.PauseGame();
        }
    }
}
