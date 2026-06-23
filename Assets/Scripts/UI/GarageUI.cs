using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Garaj ekranı: araçları tek tek gösterir, özelliklerini listeler,
    /// satın almayı ve seçmeyi sağlar. Veriler PlayerPrefs ile saklanır.
    /// </summary>
    public class GarageUI : MonoBehaviour
    {
        private int _index;

        private Text _nameText, _priceText, _coinText, _actionText;
        private Slider _powerBar, _speedBar, _weightBar;
        private Image _vehicleImage;
        private Button _actionButton;

        private void Awake()
        {
            Transform root = transform;
            _nameText = UIHelper.FindComp<Text>(root, "GarageNameText");
            _priceText = UIHelper.FindComp<Text>(root, "GaragePriceText");
            _coinText = UIHelper.FindComp<Text>(root, "GarageCoinText");
            _actionText = UIHelper.FindComp<Text>(root, "GarageActionText");
            _powerBar = UIHelper.FindComp<Slider>(root, "PowerBar");
            _speedBar = UIHelper.FindComp<Slider>(root, "SpeedBar");
            _weightBar = UIHelper.FindComp<Slider>(root, "WeightBar");
            _vehicleImage = UIHelper.FindComp<Image>(root, "GarageVehicleImage");
            _actionButton = UIHelper.FindComp<Button>(root, "GarageActionButton");

            UIHelper.Bind(root, "GaragePrevButton", () => { _index = (_index - 1 + VehicleCatalog.Count) % VehicleCatalog.Count; Refresh(); });
            UIHelper.Bind(root, "GarageNextButton", () => { _index = (_index + 1) % VehicleCatalog.Count; Refresh(); });

            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveAllListeners();
                _actionButton.onClick.AddListener(OnAction);
            }
        }

        private void OnEnable()
        {
            _index = SaveSystem.SelectedVehicle;
            Refresh();
        }

        private void Refresh()
        {
            var def = VehicleCatalog.Get(_index);
            bool owned = SaveSystem.IsVehicleOwned(_index);
            bool selected = SaveSystem.SelectedVehicle == _index;

            if (_nameText != null) _nameText.text = def.displayName;
            if (_priceText != null) _priceText.text = owned ? "Sahip Olunan" : def.price + " coin";
            if (_coinText != null) _coinText.text = "Coin: " + SaveSystem.TotalCoins;
            if (_powerBar != null) _powerBar.value = def.power;
            if (_speedBar != null) _speedBar.value = def.maxSpeed;
            if (_weightBar != null) _weightBar.value = def.weight;
            if (_vehicleImage != null) _vehicleImage.color = def.bodyColor;

            if (_actionText != null)
                _actionText.text = !owned ? "SATIN AL" : (selected ? "SEÇİLİ" : "SEÇ");
        }

        private void OnAction()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();

            var def = VehicleCatalog.Get(_index);
            bool owned = SaveSystem.IsVehicleOwned(_index);

            if (!owned)
            {
                if (SaveSystem.TrySpendCoins(def.price))
                {
                    SaveSystem.SetVehicleOwned(_index, true);
                    SaveSystem.SelectedVehicle = _index;
                    Toast("Araç satın alındı!");
                }
                else
                {
                    Toast("Yetersiz coin!");
                }
            }
            else
            {
                SaveSystem.SelectedVehicle = _index;
                Toast("Araç seçildi.");
            }
            Refresh();
        }

        private void Toast(string msg)
        {
            if (ToastMessage.Instance != null) ToastMessage.Instance.Show(msg);
        }
    }
}
