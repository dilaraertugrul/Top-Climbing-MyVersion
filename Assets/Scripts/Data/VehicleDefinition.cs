using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Bir aracın temel (upgrade'siz) özelliklerini tanımlar.
    /// Garaj ekranında listelenir, oyunda araç bu değerlerle kurulur.
    /// </summary>
    [System.Serializable]
    public class VehicleDefinition
    {
        public string id;            // Benzersiz kimlik
        public string displayName;   // Ekranda gösterilen isim
        public int price;            // Satın alma fiyatı (coin)

        [Header("Temel Özellikler")]
        public float power = 1f;     // Motor gücü (tork çarpanı) - 0..1 normalize edilmiş gösterim için
        public float maxSpeed = 1f;  // Maksimum hız çarpanı
        public float weight = 1f;    // Ağırlık (gövde kütlesi çarpanı)

        [Header("Fizik Ham Değerleri")]
        public float bodyMass = 1.2f;
        public float baseMotorTorque = 2200f; // Temel motor torku
        public float baseMotorSpeed = 1400f;  // Temel motor hızı (deg/s)

        [Header("Görsel")]
        public Color bodyColor = Color.red;
        public float bodyScale = 1f;

        public VehicleDefinition() { }
    }

    /// <summary>
    /// Oyundaki tüm araçların sabit listesi. Kod tarafında tutulur ki
    /// asset referans bağı kopması gibi hatalar yaşanmasın.
    /// </summary>
    public static class VehicleCatalog
    {
        private static VehicleDefinition[] _vehicles;

        public static VehicleDefinition[] All
        {
            get
            {
                if (_vehicles == null)
                    _vehicles = Build();
                return _vehicles;
            }
        }

        public static int Count => All.Length;

        public static VehicleDefinition Get(int index)
        {
            var all = All;
            if (index < 0 || index >= all.Length) index = 0;
            return all[index];
        }

        private static VehicleDefinition[] Build()
        {
            return new[]
            {
                new VehicleDefinition
                {
                    id = "jeep", displayName = "Çayır Jip", price = 0,
                    power = 0.5f, maxSpeed = 0.5f, weight = 0.6f,
                    bodyMass = 2.4f, baseMotorTorque = 1500f, baseMotorSpeed = 1300f,
                    bodyColor = new Color(0.85f, 0.25f, 0.25f), bodyScale = 1f
                },
                new VehicleDefinition
                {
                    id = "buggy", displayName = "Kum Buggy", price = 1500,
                    power = 0.7f, maxSpeed = 0.75f, weight = 0.45f,
                    bodyMass = 1.9f, baseMotorTorque = 1750f, baseMotorSpeed = 1700f,
                    bodyColor = new Color(0.95f, 0.7f, 0.15f), bodyScale = 0.95f
                },
                new VehicleDefinition
                {
                    id = "truck", displayName = "Dağ Kamyonu", price = 4000,
                    power = 1f, maxSpeed = 0.65f, weight = 1f,
                    bodyMass = 3.2f, baseMotorTorque = 2500f, baseMotorSpeed = 1450f,
                    bodyColor = new Color(0.3f, 0.55f, 0.85f), bodyScale = 1.15f
                },
                new VehicleDefinition
                {
                    id = "racer", displayName = "Volkan Racer", price = 8000,
                    power = 0.9f, maxSpeed = 1f, weight = 0.7f,
                    bodyMass = 2.2f, baseMotorTorque = 2050f, baseMotorSpeed = 2100f,
                    bodyColor = new Color(0.55f, 0.3f, 0.85f), bodyScale = 1f
                },
            };
        }
    }
}
