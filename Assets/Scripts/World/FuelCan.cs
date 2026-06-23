using UnityEngine;

namespace TopClimbing
{
    /// <summary>Toplanınca yakıt dolduran bidon.</summary>
    public class FuelCan : Pickup
    {
        public float amount = GameConfig.FuelCanAmount;

        protected override void Apply()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RefuelPickup(amount);
        }
    }
}
