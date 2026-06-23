using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>
    /// Kısa süreli uyarı mesajı (ör. "Yetersiz coin!", "Maksimum seviye!").
    /// </summary>
    public class ToastMessage : MonoBehaviour
    {
        public static ToastMessage Instance { get; private set; }

        private Text _label;
        private CanvasGroup _group;
        private Coroutine _routine;

        private void Awake()
        {
            Instance = this;
            _label = GetComponentInChildren<Text>(true);
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void Show(string message, float duration = 1.5f)
        {
            if (_label != null) _label.text = message;
            if (_routine != null) StopCoroutine(_routine);
            gameObject.SetActive(true);
            _routine = StartCoroutine(ShowRoutine(duration));
        }

        private IEnumerator ShowRoutine(float duration)
        {
            // Belirme
            float t = 0f;
            while (t < 0.15f) { t += Time.unscaledDeltaTime; _group.alpha = t / 0.15f; yield return null; }
            _group.alpha = 1f;

            yield return new WaitForSecondsRealtime(duration);

            // Sönme
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _group.alpha = 1f - t / 0.3f; yield return null; }
            _group.alpha = 0f;
        }
    }
}
