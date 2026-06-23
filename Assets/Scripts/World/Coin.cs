using UnityEngine;

namespace TopClimbing
{
    /// <summary>Toplanınca tur coin sayısını artıran para.</summary>
    public class Coin : Pickup
    {
        public int value = GameConfig.CoinValue;
        public float spinSpeed = 180f;

        private Transform _visual;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_visual == null)
                _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        private void Update()
        {
            // Hafif dönme efekti (yatayda nabız gibi daralıp genişler, hep pozitif kalır)
            if (_visual != null)
            {
                float sx = 0.6f + 0.4f * Mathf.Cos(Time.time * 3f);
                _visual.localScale = new Vector3(sx, 1f, 1f);
            }
        }

        protected override void Apply()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.CollectCoin(value);
        }
    }
}
