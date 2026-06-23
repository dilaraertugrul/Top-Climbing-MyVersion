using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing
{
    /// <summary>UI controller'larının çocuk objeleri isimle güvenle bulması için yardımcılar.</summary>
    public static class UIHelper
    {
        /// <summary>İsmi eşleşen ilk çocuğu (derinlemesine) bulur.</summary>
        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        public static T FindComp<T>(Transform root, string name) where T : Component
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<T>() : null;
        }

        /// <summary>Bir butona güvenle tıklama dinleyicisi ekler (önce temizler).</summary>
        public static void Bind(Transform root, string name, UnityEngine.Events.UnityAction action)
        {
            var btn = FindComp<Button>(root, name);
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                action?.Invoke();
            });
        }

        public static void SetText(Transform root, string name, string value)
        {
            var t = FindComp<Text>(root, name);
            if (t != null) t.text = value;
        }
    }
}
