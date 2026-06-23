# Top Climbing — Kurulum ve Kullanım

2D fizik tabanlı, Hill Climb tarzı mobil tırmanma oyunu. Unity **2022.3 LTS** ile uyumludur
(proje Unity 6 değil 2022.3.62f3 ile kurulu; tüm kodlar bu sürümle çalışır).

## 🚀 Tek Tıkla Kurulum

1. Projeyi Unity ile aç. Scriptler derlensin (sağ altta spinner durana kadar bekle).
2. Üst menüden: **Top Climbing → 1) Tüm Oyunu Kur (Build All)**
3. Çıkan onayda **"Evet, Kur"** de.

Bu işlem otomatik olarak şunları üretir:

- **Görseller** → `Assets/Art/` (araç, tekerlek, coin, yakıt bidonu, butonlar, tepe/bulut arka planları) — hepsi gerçek PNG dosyası.
- **Prefab'lar** → `Assets/Prefabs/` (Vehicle, Coin, FuelCan)
- **Sahneler** → `Assets/Scenes/` (`MainMenu.unity`, `Game.unity`) — Hierarchy'de tüm objeleri görürsün.
- **Build Settings** sahne listesi (MainMenu = 0, Game = 1).

Kurulum bitince **MainMenu** sahnesi otomatik açılır. **Play**'e bas, **RACE** ile oyna.

> Kayıtları sıfırlamak için: **Top Climbing → Kayıtları Sıfırla**.

## 🎮 Kontroller

| Eylem | Klavye (Editor testi) | Mobil |
|------|------------------------|-------|
| Gaz / İleri | Sağ ok veya **D** | Sağ alt buton |
| Fren / Geri | Sol ok veya **A** | Sol alt buton |
| Duraklat | — | Sağ üst **II** |

Gaz verince aracın burnu havalanır, frenle düzeltilir (tamamen fizik tabanlı).

## 🧩 Sistem Mimarisi (her sistem ayrı script)

```
Assets/Scripts/
├── Core/        GameManager, SaveSystem, AudioManager, InputManager, ObjectPool, ProceduralAudio
├── Data/        GameConfig, VehicleDefinition (+VehicleCatalog), UpgradeCatalog
├── Vehicle/     VehicleController (WheelJoint2D fizik), FuelSystem
├── World/       TerrainGenerator + TerrainChunk (sonsuz arazi), BackgroundManager (paralaks+biome),
│                Biome, Pickup, Coin, FuelCan
├── Camera/      CameraFollow (SmoothDamp yumuşak takip)
└── UI/          MainMenuUI, GarageUI, UpgradeUI, SettingsUI, HUDController,
                 PauseMenuUI, GameOverUI, MobileButton, ToastMessage, UIHelper
Assets/Editor/   GameBuilder (üretici), SpriteFactory (görseller), UIBuilder (UI kurucu)
```

Namespace: tümü `TopClimbing` (editor araçları `TopClimbing.EditorTools`).

## ⚙️ Denge Ayarları

Çoğu ayar `Assets/Scripts/Data/GameConfig.cs` içinde tek yerde:
- Yakıt: başlangıç/maks, boşta ve gaz tüketimi, bidon miktarı
- Ters dönme süresi/açısı (Game Over)
- Biome geçiş mesafeleri (çiçekli → gökyüzü → volkanik)

Araç gücü/hızı/ağırlığı `VehicleCatalog`, geliştirme etkileri `UpgradeCatalog` içinde.
Arazi zorluğu `TerrainGenerator.Height()` fonksiyonunda.

## ✅ Özellik Karşılığı

- Fizik tabanlı araç (gövde + 2 tekerlek + süspansiyon, WheelJoint2D) ✓
- Sonsuz prosedürel arazi + chunk geri dönüşümü (havuz) ✓
- Biome'a göre değişen arka plan + paralaks ✓
- Yakıt sistemi + bidonlar, Coin toplama + havuz ✓
- Game Over: yakıt bitti / uzun süre ters kalma ✓
- Ana Menü / Garaj / Upgrade / Ayarlar / HUD / Pause / Game Over ✓
- PlayerPrefs kayıt: coin, araçlar, seçili araç, upgrade, ses ayarları, rekor ✓
- Playlist müzik + efektler (prosedürel üretilir, ses dosyası gerekmez) ✓
- Yumuşak kamera takibi ✓
