using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Haritalar ekranı: haritaları tek tek gösterir, önizleme renklerini uygular,
    /// satın almayı ve seçmeyi sağlar. Veriler PlayerPrefs ile saklanır.
    /// </summary>
    public class MapsUI : MonoBehaviour
    {
        private int _index;

        private Text _nameText, _priceText, _coinText, _actionText;
        private Image _previewSky, _previewGround, _previewHill;
        private Button _actionButton;

        private void Awake()
        {
            Transform root = transform;
            _nameText = UIHelper.FindComp<Text>(root, "MapNameText");
            _priceText = UIHelper.FindComp<Text>(root, "MapPriceText");
            _coinText = UIHelper.FindComp<Text>(root, "MapCoinText");
            _actionText = UIHelper.FindComp<Text>(root, "MapActionText");
            _previewSky = UIHelper.FindComp<Image>(root, "MapPreviewSky");
            _previewGround = UIHelper.FindComp<Image>(root, "MapPreviewGround");
            _previewHill = UIHelper.FindComp<Image>(root, "MapPreviewHill");
            _actionButton = UIHelper.FindComp<Button>(root, "MapActionButton");

            UIHelper.Bind(root, "MapPrevButton", () => { _index = (_index - 1 + MapCatalog.Count) % MapCatalog.Count; Refresh(); });
            UIHelper.Bind(root, "MapNextButton", () => { _index = (_index + 1) % MapCatalog.Count; Refresh(); });

            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveAllListeners();
                _actionButton.onClick.AddListener(OnAction);
            }
        }

        private void OnEnable()
        {
            _index = SaveSystem.SelectedMap;
            Refresh();
        }

        private void Refresh()
        {
            var map = MapCatalog.Get(_index);
            bool owned = SaveSystem.IsMapOwned(_index);
            bool selected = SaveSystem.SelectedMap == _index;

            if (_nameText != null) _nameText.text = map.displayName;
            if (_priceText != null) _priceText.text = owned ? "Sahip Olunan" : map.price + " coin";
            if (_coinText != null) _coinText.text = "Coin: " + SaveSystem.TotalCoins;
            if (_previewSky != null) _previewSky.color = map.palette.skyTop;
            if (_previewGround != null) _previewGround.color = map.palette.ground;
            if (_previewHill != null) _previewHill.color = map.palette.hill;

            if (_actionText != null)
                _actionText.text = !owned ? "SATIN AL" : (selected ? "SEÇİLİ" : "SEÇ");
        }

        private void OnAction()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();

            var map = MapCatalog.Get(_index);
            bool owned = SaveSystem.IsMapOwned(_index);

            if (!owned)
            {
                if (SaveSystem.TrySpendCoins(map.price))
                {
                    SaveSystem.SetMapOwned(_index, true);
                    SaveSystem.SelectedMap = _index;
                    Toast("Harita satın alındı!");
                }
                else
                {
                    Toast("Yetersiz coin!");
                }
            }
            else
            {
                SaveSystem.SelectedMap = _index;
                Toast("Harita seçildi.");
            }
            Refresh();
        }

        private void Toast(string msg)
        {
            if (ToastMessage.Instance != null) ToastMessage.Instance.Show(msg);
        }
    }
}
