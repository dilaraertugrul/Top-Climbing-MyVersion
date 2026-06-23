using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Toplanabilir nesneler (coin, yakıt bidonu) için temel sınıf.
    /// Araç temas ettiğinde Apply() çalışır ve nesne havuza döner.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Pickup : MonoBehaviour
    {
        [HideInInspector] public ObjectPool Pool;

        protected bool _collected;

        protected virtual void OnEnable()
        {
            _collected = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected) return;
            // Aracın herhangi bir parçasına (gövde/tekerlek) temas
            var vehicle = other.GetComponentInParent<VehicleController>();
            if (vehicle == null) return;

            _collected = true;
            Apply();

            if (Pool != null) Pool.Return(gameObject);
            else gameObject.SetActive(false);
        }

        /// <summary>Toplanma etkisini uygular (alt sınıflar doldurur).</summary>
        protected abstract void Apply();
    }
}
