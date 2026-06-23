using UnityEngine;
using UnityEngine.EventSystems;

namespace TopClimbing
{
    /// <summary>
    /// Ekrandaki gaz/fren butonu. Basılı tutulurken InputManager'a girdi verir.
    /// Hem dokunma hem fare ile çalışır.
    /// </summary>
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum ButtonAction { Gas, Brake }
        public ButtonAction action = ButtonAction.Gas;

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);

        private void Set(bool held)
        {
            if (InputManager.Instance == null) return;
            if (action == ButtonAction.Gas) InputManager.Instance.SetGas(held);
            else InputManager.Instance.SetBrake(held);
        }

        private void OnDisable() => Set(false);
    }
}
