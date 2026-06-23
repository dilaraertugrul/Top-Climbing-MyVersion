using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopClimbing.EditorTools
{
    /// <summary>
    /// Tüm oyunu tek menüden kuran üretici:
    /// görselleri üretir, prefab'ları oluşturur, MainMenu ve Game sahnelerini
    /// tüm hiyerarşi + UI ile kurar ve Build Settings'i ayarlar.
    /// </summary>
    public static class GameBuilder
    {
        // Tüm üretilen sprite'lar
        private class Art
        {
            public Sprite carBody, wheel, driver, coin, fuelCan, panel, white, pedalGas, pedalBrake;
            public Sprite hillFar, hillNear, clouds, menuBg;
        }

        private const string ScenesFolder = "Assets/Scenes";
        private const string PrefabsFolder = "Assets/Prefabs";

        // ===================================================================
        [MenuItem("Top Climbing/1) Tüm Oyunu Kur (Build All)", false, 0)]
        public static void BuildAll()
        {
            if (!EditorUtility.DisplayDialog("Top Climbing",
                "Tüm oyun kurulacak:\n- Görseller (Assets/Art)\n- Prefab'lar\n- MainMenu & Game sahneleri\n\nDevam edilsin mi?",
                "Evet, Kur", "Vazgeç"))
                return;

            EnsureFolders();
            Art art = GenerateSprites();
            UIBuilder.PanelSprite = art.panel;

            GameObject vehiclePrefab = BuildVehiclePrefab(art);
            GameObject coinPrefab = BuildCoinPrefab(art);
            GameObject fuelPrefab = BuildFuelPrefab(art);

            BuildMainMenuScene(art);
            BuildGameScene(art, vehiclePrefab, coinPrefab, fuelPrefab);

            ConfigurePlayerSettings();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Bitince ana menüyü aç
            EditorSceneManager.OpenScene($"{ScenesFolder}/MainMenu.unity");

            EditorUtility.DisplayDialog("Top Climbing",
                "Kurulum tamamlandı! ✅\n\n- Sahneler: Assets/Scenes (MainMenu, Game)\n- Görseller: Assets/Art\n- Prefab'lar: Assets/Prefabs\n\nPlay'e basıp ana menüden 'RACE' ile oynayabilirsin.\nEditörde test: Sağ/Sol ok veya A/D tuşları.",
                "Harika!");
        }

        [MenuItem("Top Climbing/Kayıtları Sıfırla (Clear Save)", false, 20)]
        public static void ClearSave()
        {
            SaveSystem.ClearAll();
            Debug.Log("[Top Climbing] Tüm kayıtlar sıfırlandı.");
        }

        // ===================================================================
        private static void EnsureFolders()
        {
            foreach (var f in new[] { "Assets/Art", PrefabsFolder, ScenesFolder })
                if (!Directory.Exists(f)) Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
        }

        private static Art GenerateSprites()
        {
            return new Art
            {
                carBody = SpriteFactory.CreateCarBody(),
                wheel = SpriteFactory.CreateWheel(),
                driver = SpriteFactory.CreateDriver(),
                coin = SpriteFactory.CreateCoin(),
                fuelCan = SpriteFactory.CreateFuelCan(),
                panel = SpriteFactory.CreatePanel(),
                white = SpriteFactory.CreateWhite(),
                pedalGas = SpriteFactory.CreatePedal("Sprite_PedalGas", 1),
                pedalBrake = SpriteFactory.CreatePedal("Sprite_PedalBrake", -1),
                hillFar = SpriteFactory.CreateHillLayer("Sprite_HillFar", 70f, 1f),
                hillNear = SpriteFactory.CreateHillLayer("Sprite_HillNear", 110f, 1.4f),
                clouds = SpriteFactory.CreateCloudLayer(),
                menuBg = SpriteFactory.CreateMenuBackground()
            };
        }

        // ===================================================================
        //  PREFAB'LAR
        // ===================================================================
        private static GameObject BuildVehiclePrefab(Art art)
        {
            var root = new GameObject("Vehicle");

            // --- Gövde (Body): rigidbody + sprite + collider + scriptler ---
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);

            var body = bodyGo.AddComponent<Rigidbody2D>();
            body.mass = 1.2f;
            body.gravityScale = 1.4f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.angularDrag = 0.1f;

            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sprite = art.carBody;
            sr.sortingOrder = 5;

            // --- Sürücü (gövdenin çocuğu: araçla birlikte döner/devrilir) ---
            var driverGo = new GameObject("Driver");
            driverGo.transform.SetParent(bodyGo.transform, false);
            driverGo.transform.localPosition = new Vector3(-0.03f, 0.55f, 0f); // açık roll-cage kabin
            var driverSr = driverGo.AddComponent<SpriteRenderer>();
            driverSr.sprite = art.driver;
            driverSr.sortingOrder = 6; // gövdenin önünde, tekerleklerin arkasında

            // --- Sürücünün kafası (katı, küçük collider): YERE DEĞİNCE oyun biter ---
            // Kafa, sürücü kaskının hizasında ve aracın en üst noktasında; dik sürüşte zemine değmez.
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(bodyGo.transform, false);
            headGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var headCol = headGo.AddComponent<CircleCollider2D>();
            headCol.radius = 0.24f;

            var bodyCol = bodyGo.AddComponent<BoxCollider2D>();
            bodyCol.size = new Vector2(2.2f, 0.6f);
            bodyCol.offset = new Vector2(0f, 0.35f); // tekerleklerin üstünde kalsın

            // Pickup yakalayıcı (geniş trigger) - coin/yakıtı güvenle toplar
            var catcher = bodyGo.AddComponent<BoxCollider2D>();
            catcher.isTrigger = true;
            catcher.size = new Vector2(3f, 2.5f);
            catcher.offset = new Vector2(0f, 0.4f);

            bodyGo.AddComponent<FuelSystem>();
            var vc = bodyGo.AddComponent<VehicleController>();
            vc.headCollider = headCol;

            // --- Tekerlekler (gövdenin kardeşi: iç içe rigidbody sorunlarını önler) ---
            var rearWheel = BuildWheel(root.transform, "RearWheel", art.wheel, new Vector3(-0.85f, -0.55f, 0f));
            var frontWheel = BuildWheel(root.transform, "FrontWheel", art.wheel, new Vector3(0.85f, -0.55f, 0f));

            // --- WheelJoint2D'ler (süspansiyon + motor) gövdeye eklenir ---
            var rearJoint = AddWheelJoint(bodyGo, rearWheel, new Vector2(-0.85f, -0.55f));
            var frontJoint = AddWheelJoint(bodyGo, frontWheel, new Vector2(0.85f, -0.55f));

            // --- Referansları ata ---
            vc.bodyRb = body;
            vc.rearWheelRb = rearWheel;
            vc.frontWheelRb = frontWheel;
            vc.rearJoint = rearJoint;
            vc.frontJoint = frontJoint;

            string path = $"{PrefabsFolder}/Vehicle.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Rigidbody2D BuildWheel(Transform parent, string name, Sprite sprite, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = 0.4f;
            rb.gravityScale = 1.4f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.angularDrag = 0.05f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 7; // gövdenin önünde -> tekerlekler tam görünür

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.6f;
            col.sharedMaterial = new PhysicsMaterial2D(name + "Mat") { friction = 2f, bounciness = 0f };
            return rb;
        }

        private static WheelJoint2D AddWheelJoint(GameObject body, Rigidbody2D wheel, Vector2 anchorOnBody)
        {
            var joint = body.AddComponent<WheelJoint2D>();
            joint.connectedBody = wheel;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = anchorOnBody;
            joint.connectedAnchor = Vector2.zero;

            var s = joint.suspension;
            s.frequency = 5f;
            s.dampingRatio = 0.7f;
            s.angle = 90f;
            joint.suspension = s;

            joint.useMotor = false;
            return joint;
        }

        private static GameObject BuildCoinPrefab(Art art)
        {
            var go = new GameObject("Coin");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.coin;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            go.AddComponent<Coin>();

            string path = $"{PrefabsFolder}/Coin.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildFuelPrefab(Art art)
        {
            var go = new GameObject("FuelCan");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.fuelCan;
            sr.sortingOrder = 1;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.95f);
            go.AddComponent<FuelCan>();

            string path = $"{PrefabsFolder}/FuelCan.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ===================================================================
        //  ORTAK SAHNE PARÇALARI
        // ===================================================================
        private static Camera MakeCamera(Color bg, bool addListener = true)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.transform.position = new Vector3(0, 0, -10f);
            if (addListener) go.AddComponent<AudioListener>();
            return cam;
        }

        private static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }

        private static void MakeEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void MakeAudioManager()
        {
            new GameObject("AudioManager", typeof(AudioManager));
        }

        // Merkez (0.5,0.5) yerleşim yardımcısı (1920x1080 referans)
        private static void Center(GameObject go, float x, float y, float w, float h)
        {
            UIBuilder.SetAnchor(UIBuilder.Rect(go),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(w, h));
        }

        private static void Anchor(GameObject go, Vector2 a, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            UIBuilder.SetAnchor(UIBuilder.Rect(go), a, a, pivot, pos, size);
        }

        // ===================================================================
        //  ANA MENÜ SAHNESİ
        // ===================================================================
        private static void BuildMainMenuScene(Art art)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MakeCamera(new Color(0.06f, 0.07f, 0.14f)); // koyu lacivert
            MakeEventSystem();
            MakeAudioManager();

            var canvas = MakeCanvas("MenuCanvas");
            Transform c = canvas.transform;

            // Arka plan: koyu atmosferik degrade (yıldız/ay/tepe)
            var bg = UIBuilder.MakeImage(c, "MenuBackground", Color.white, false);
            bg.sprite = art.menuBg; bg.type = Image.Type.Simple;
            UIBuilder.Stretch(UIBuilder.Rect(bg.gameObject));

            BuildMainPanel(c, art);
            BuildGaragePanel(c, art);
            BuildMapsPanel(c, art);
            BuildUpgradePanel(c, art);
            var settings = BuildSettingsPanel(c);

            BuildToast(c);

            // Controller'lar
            canvas.gameObject.AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{ScenesFolder}/MainMenu.unity");
        }

        private static void BuildMainPanel(Transform c, Art art)
        {
            var panel = UIBuilder.Empty(c, "MainPanel");
            UIBuilder.Stretch(UIBuilder.Rect(panel));

            var title = UIBuilder.MakeText(panel.transform, "Title", "TOP CLIMBING", 92, new Color(1f, 0.85f, 0.35f));
            Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 120));

            var subtitle = UIBuilder.MakeText(panel.transform, "Subtitle", "MACERAYA TIRMAN", 30, new Color(0.62f, 0.74f, 0.95f));
            Anchor(subtitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(800, 50));

            var coin = UIBuilder.MakeText(panel.transform, "MenuCoinText", "Coin: 0", 40, new Color(1f, 0.9f, 0.45f), TextAnchor.MiddleRight);
            Anchor(coin.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(400, 60));

            // Koyu, dingin buton paleti
            var race = UIBuilder.MakeButton(panel.transform, "RaceButton", "RACE", new Color(0.13f, 0.55f, 0.34f), 56);
            Center(race.gameObject, 0, 150, 470, 120);
            var garage = UIBuilder.MakeButton(panel.transform, "GarageButton", "GARAJ", new Color(0.16f, 0.30f, 0.52f));
            Center(garage.gameObject, 0, 22, 470, 96);
            var maps = UIBuilder.MakeButton(panel.transform, "MapsButton", "HARİTALAR", new Color(0.13f, 0.42f, 0.46f));
            Center(maps.gameObject, 0, -86, 470, 96);
            var upg = UIBuilder.MakeButton(panel.transform, "UpgradeButton", "GELİŞTİRME", new Color(0.34f, 0.26f, 0.52f));
            Center(upg.gameObject, 0, -194, 470, 96);
            var settings = UIBuilder.MakeButton(panel.transform, "SettingsButton", "AYARLAR", new Color(0.26f, 0.29f, 0.36f));
            Center(settings.gameObject, 0, -302, 470, 96);
        }

        private static void BuildGaragePanel(Transform c, Art art)
        {
            var panel = UIBuilder.MakeImage(c, "GaragePanel", new Color(0.12f, 0.16f, 0.22f, 1f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "GARAJ", 70, Color.white);
            Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(600, 100));

            var coin = UIBuilder.MakeText(panel.transform, "GarageCoinText", "Coin: 0", 40, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight);
            Anchor(coin.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(400, 60));

            var img = UIBuilder.MakeImage(panel.transform, "GarageVehicleImage", Color.red, false);
            img.sprite = art.carBody; img.type = Image.Type.Simple; img.preserveAspect = true;
            Center(img.gameObject, 0, 180, 500, 300);

            var name = UIBuilder.MakeText(panel.transform, "GarageNameText", "Araç", 50, Color.white);
            Center(name.gameObject, 0, -10, 700, 70);

            // Stat çubukları
            BuildStatBar(panel.transform, "Güç", "PowerBar", new Color(0.9f, 0.4f, 0.3f), -90);
            BuildStatBar(panel.transform, "Hız", "SpeedBar", new Color(0.3f, 0.7f, 0.9f), -150);
            BuildStatBar(panel.transform, "Ağırlık", "WeightBar", new Color(0.6f, 0.6f, 0.6f), -210);

            var price = UIBuilder.MakeText(panel.transform, "GaragePriceText", "0 coin", 40, new Color(1f, 0.9f, 0.4f));
            Center(price.gameObject, 0, -280, 500, 60);

            var prev = UIBuilder.MakeButton(panel.transform, "GaragePrevButton", "<", new Color(0.3f, 0.35f, 0.45f), 60);
            Center(prev.gameObject, -520, 150, 110, 160);
            var next = UIBuilder.MakeButton(panel.transform, "GarageNextButton", ">", new Color(0.3f, 0.35f, 0.45f), 60);
            Center(next.gameObject, 520, 150, 110, 160);

            var action = UIBuilder.MakeButton(panel.transform, "GarageActionButton", "", new Color(0.2f, 0.7f, 0.3f));
            Center(action.gameObject, 0, -360, 420, 110);
            var actionText = UIBuilder.MakeText(action.transform, "GarageActionText", "SEÇ", 44, Color.white);
            UIBuilder.Stretch(UIBuilder.Rect(actionText.gameObject));

            var back = UIBuilder.MakeButton(panel.transform, "GarageBackButton", "← Geri", new Color(0.4f, 0.45f, 0.5f), 34);
            Anchor(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 80));

            panel.gameObject.AddComponent<GarageUI>();
            panel.gameObject.SetActive(false);
        }

        private static void BuildStatBar(Transform parent, string label, string barName, Color color, float y)
        {
            var lbl = UIBuilder.MakeText(parent, barName + "Label", label, 32, Color.white, TextAnchor.MiddleRight);
            Center(lbl.gameObject, -280, y, 200, 50);
            var bar = UIBuilder.MakeBar(parent, barName, color);
            Center(bar.gameObject, 120, y, 600, 36);
        }

        private static void BuildMapsPanel(Transform c, Art art)
        {
            var panel = UIBuilder.MakeImage(c, "MapsPanel", new Color(0.10f, 0.13f, 0.18f, 1f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "HARİTALAR", 70, Color.white);
            Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(700, 100));

            var coin = UIBuilder.MakeText(panel.transform, "MapCoinText", "Coin: 0", 40, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight);
            Anchor(coin.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(400, 60));

            // --- Önizleme penceresi: gökyüzü + zemin şeridi + tepe silüeti (renkler MapsUI'dan) ---
            var sky = UIBuilder.MakeImage(panel.transform, "MapPreviewSky", new Color(0.55f, 0.8f, 0.98f), false);
            Center(sky.gameObject, 0, 150, 760, 360);

            var ground = UIBuilder.MakeImage(sky.transform, "MapPreviewGround", new Color(0.45f, 0.7f, 0.3f), false);
            var gRt = UIBuilder.Rect(ground.gameObject);
            UIBuilder.SetAnchor(gRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 120));

            var hill = UIBuilder.MakeImage(sky.transform, "MapPreviewHill", new Color(0.6f, 0.78f, 0.45f), false);
            hill.sprite = art.hillNear; hill.type = Image.Type.Simple; hill.preserveAspect = false;
            var hRt = UIBuilder.Rect(hill.gameObject);
            UIBuilder.SetAnchor(hRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(0, 150));

            var name = UIBuilder.MakeText(panel.transform, "MapNameText", "Harita", 52, Color.white);
            Center(name.gameObject, 0, -70, 700, 70);

            var price = UIBuilder.MakeText(panel.transform, "MapPriceText", "0 coin", 40, new Color(1f, 0.9f, 0.4f));
            Center(price.gameObject, 0, -140, 600, 60);

            var prev = UIBuilder.MakeButton(panel.transform, "MapPrevButton", "<", new Color(0.3f, 0.35f, 0.45f), 60);
            Center(prev.gameObject, -520, 150, 110, 160);
            var next = UIBuilder.MakeButton(panel.transform, "MapNextButton", ">", new Color(0.3f, 0.35f, 0.45f), 60);
            Center(next.gameObject, 520, 150, 110, 160);

            var action = UIBuilder.MakeButton(panel.transform, "MapActionButton", "", new Color(0.2f, 0.7f, 0.3f));
            Center(action.gameObject, 0, -250, 420, 110);
            var actionText = UIBuilder.MakeText(action.transform, "MapActionText", "SEÇ", 44, Color.white);
            UIBuilder.Stretch(UIBuilder.Rect(actionText.gameObject));

            var back = UIBuilder.MakeButton(panel.transform, "MapsBackButton", "← Geri", new Color(0.4f, 0.45f, 0.5f), 34);
            Anchor(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 80));

            panel.gameObject.AddComponent<MapsUI>();
            panel.gameObject.SetActive(false);
        }

        private static void BuildUpgradePanel(Transform c, Art art)
        {
            var panel = UIBuilder.MakeImage(c, "UpgradePanel", new Color(0.14f, 0.12f, 0.2f, 1f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "GELİŞTİRME", 70, Color.white);
            Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(700, 100));

            var coin = UIBuilder.MakeText(panel.transform, "UpgradeCoinText", "Coin: 0", 40, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight);
            Anchor(coin.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(400, 60));

            BuildUpgradeRow(panel.transform, "Engine", "MOTOR", 120);
            BuildUpgradeRow(panel.transform, "Suspension", "SÜSPANSİYON", -20);
            BuildUpgradeRow(panel.transform, "Tires", "LASTİK", -160);

            var back = UIBuilder.MakeButton(panel.transform, "UpgradeBackButton", "← Geri", new Color(0.4f, 0.45f, 0.5f), 34);
            Anchor(back.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(220, 80));

            panel.gameObject.AddComponent<UpgradeUI>();
            panel.gameObject.SetActive(false);
        }

        private static void BuildUpgradeRow(Transform parent, string prefix, string title, float y)
        {
            var row = UIBuilder.MakeImage(parent, prefix + "Row", new Color(1f, 1f, 1f, 0.08f));
            Center(row.gameObject, 0, y, 1000, 120);

            var lbl = UIBuilder.MakeText(row.transform, prefix + "Title", title, 40, Color.white, TextAnchor.MiddleLeft);
            Anchor(lbl.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, 25), new Vector2(400, 50));

            var bar = UIBuilder.MakeBar(row.transform, prefix + "LevelBar", new Color(0.4f, 0.8f, 0.5f));
            Anchor(bar.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30, -30), new Vector2(450, 26));

            var lvl = UIBuilder.MakeText(row.transform, prefix + "LevelText", "Lv 0/5", 30, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleLeft);
            Anchor(lvl.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(500, 25), new Vector2(200, 50));

            var cost = UIBuilder.MakeText(row.transform, prefix + "CostText", "0 coin", 32, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleRight);
            Anchor(cost.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220, 0), new Vector2(260, 50));

            var buy = UIBuilder.MakeButton(row.transform, prefix + "BuyButton", "+", new Color(0.2f, 0.7f, 0.3f), 50);
            Anchor(buy.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30, 0), new Vector2(150, 90));
        }

        private static SettingsUI BuildSettingsPanel(Transform c)
        {
            var panel = UIBuilder.MakeImage(c, "SettingsPanel", new Color(0.1f, 0.12f, 0.16f, 1f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "AYARLAR", 70, Color.white);
            Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(600, 100));

            var music = UIBuilder.MakeToggle(panel.transform, "MusicToggle", "Müzik", new Color(0.3f, 0.7f, 0.4f));
            Center(music.gameObject, -150, 60, 500, 70);
            var sfx = UIBuilder.MakeToggle(panel.transform, "SfxToggle", "Tuş / Efekt Sesleri", new Color(0.3f, 0.7f, 0.4f));
            Center(sfx.gameObject, -150, -40, 500, 70);

            var close = UIBuilder.MakeButton(panel.transform, "CloseButton", "KAPAT", new Color(0.4f, 0.45f, 0.5f));
            Center(close.gameObject, 0, -200, 360, 100);

            var ui = panel.gameObject.AddComponent<SettingsUI>();
            panel.gameObject.SetActive(false);
            return ui;
        }

        private static void BuildToast(Transform c)
        {
            var toast = UIBuilder.MakeImage(c, "Toast", new Color(0f, 0f, 0f, 0.8f));
            Anchor(toast.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(700, 100));
            var t = UIBuilder.MakeText(toast.transform, "ToastText", "", 38, Color.white);
            UIBuilder.Stretch(UIBuilder.Rect(t.gameObject));
            toast.gameObject.AddComponent<ToastMessage>();
        }

        // ===================================================================
        //  OYUN SAHNESİ
        // ===================================================================
        private static void BuildGameScene(Art art, GameObject vehiclePrefab, GameObject coinPrefab, GameObject fuelPrefab)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = MakeCamera(new Color(0.55f, 0.80f, 0.98f));
            MakeEventSystem();
            MakeAudioManager();
            new GameObject("InputManager", typeof(InputManager));

            // --- Araç ---
            var vehicleGo = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab);
            vehicleGo.transform.position = new Vector3(0f, 3f, 0f);
            var vc = vehicleGo.GetComponentInChildren<VehicleController>();
            var fuel = vehicleGo.GetComponentInChildren<FuelSystem>();
            Transform bodyTf = vc != null ? vc.transform : vehicleGo.transform;

            // --- Kamera takip ---
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = bodyTf;

            // --- Arka plan (paralaks) ---
            BuildBackground(cam, art);

            // --- Arazi üreteci ---
            var terrainGo = new GameObject("TerrainGenerator");
            var terrain = terrainGo.AddComponent<TerrainGenerator>();
            terrain.coinPrefab = coinPrefab;
            terrain.fuelCanPrefab = fuelPrefab;

            // --- GameManager ---
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.player = vc;
            gm.fuel = fuel;

            // --- HUD + paneller ---
            BuildHud(art);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), $"{ScenesFolder}/Game.unity");
        }

        private static void BuildBackground(Camera cam, Art art)
        {
            var root = new GameObject("Background");
            var bm = root.AddComponent<BackgroundManager>();
            bm.targetCamera = cam;
            bm.layers = new List<ParallaxLayer>();

            // factor: 0 = en uzak/en yavaş, 1 = dünyaya kilitli. Bulutlar ayrıca kendi başına süzülür.
            bm.layers.Add(MakeParallaxLayer(root.transform, "Clouds", art.clouds, -20, 0.12f, 3.8f, false, 0.55f));
            bm.layers.Add(MakeParallaxLayer(root.transform, "FarHills", art.hillFar, -18, 0.30f, -3.2f, true));
            bm.layers.Add(MakeParallaxLayer(root.transform, "NearHills", art.hillNear, -16, 0.55f, -4.2f, true));
        }

        private static ParallaxLayer MakeParallaxLayer(Transform parent, string name, Sprite sprite,
                                                       int order, float factor, float baseY, bool isHill,
                                                       float autoScroll = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            // Döşeli çizim: 3 kopya yan yana -> modulo kaydırmada kamera görüşü daima kaplanır
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(sprite.bounds.size.x * 3f, sprite.bounds.size.y);
            return new ParallaxLayer
            {
                renderer = sr,
                factor = factor,
                tileWidth = sprite.bounds.size.x,
                baseY = baseY,
                isHill = isHill,
                autoScrollSpeed = autoScroll
            };
        }

        private static void BuildHud(Art art)
        {
            var canvas = MakeCanvas("HUDCanvas");
            Transform c = canvas.transform;

            // --- Üst bilgi çubuğu ---
            var fuelLbl = UIBuilder.MakeText(c, "FuelLabel", "Yakıt", 28, Color.white, TextAnchor.MiddleLeft);
            Anchor(fuelLbl.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -30), new Vector2(120, 40));
            var fuelBar = UIBuilder.MakeBar(c, "FuelBar", new Color(0.3f, 0.85f, 0.3f));
            Anchor(fuelBar.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150, -35), new Vector2(360, 40));

            var coin = UIBuilder.MakeText(c, "CoinText", "Coin: 0", 36, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleLeft);
            Anchor(coin.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -85), new Vector2(400, 50));
            var dist = UIBuilder.MakeText(c, "DistanceText", "0 m", 36, Color.white, TextAnchor.MiddleLeft);
            Anchor(dist.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -135), new Vector2(400, 50));
            var speed = UIBuilder.MakeText(c, "SpeedText", "0 km/s", 36, Color.white, TextAnchor.MiddleLeft);
            Anchor(speed.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -185), new Vector2(400, 50));

            // --- Pause butonu ---
            var pause = UIBuilder.MakeButton(c, "PauseButton", "II", new Color(0f, 0f, 0f, 0.5f), 44);
            Anchor(pause.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(110, 110));

            // --- Kontroller (gaz/fren) ---
            BuildPedal(c, "GasButton", art.pedalGas, MobileButton.ButtonAction.Gas, new Vector2(1f, 0f), new Vector2(-50, 50));
            BuildPedal(c, "BrakeButton", art.pedalBrake, MobileButton.ButtonAction.Brake, new Vector2(0f, 0f), new Vector2(50, 50));

            canvas.gameObject.AddComponent<HUDController>();

            // --- Pause paneli ---
            BuildPausePanel(c);

            // --- Ayarlar (pause içinde kullanılır) ---
            BuildSettingsPanel(c);

            // --- Game Over paneli ---
            BuildGameOverPanel(c);

            // --- Toast ---
            BuildToast(c);

            // Controller'lar (panelleri isimle bulurlar)
            canvas.gameObject.AddComponent<PauseMenuUI>();
            canvas.gameObject.AddComponent<GameOverUI>();
        }

        private static void BuildPedal(Transform c, string name, Sprite sprite, MobileButton.ButtonAction action,
                                       Vector2 anchorCorner, Vector2 pos)
        {
            var img = UIBuilder.MakeImage(c, name, new Color(1f, 1f, 1f, 0.85f), false);
            img.sprite = sprite; img.type = Image.Type.Simple;
            img.raycastTarget = true;
            Anchor(img.gameObject, anchorCorner, anchorCorner, pos, new Vector2(220, 220));
            var btn = img.gameObject.AddComponent<MobileButton>();
            btn.action = action;
        }

        private static void BuildPausePanel(Transform c)
        {
            var panel = UIBuilder.MakeImage(c, "PausePanel", new Color(0f, 0f, 0f, 0.7f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "DURAKLATILDI", 70, Color.white);
            Center(title.gameObject, 0, 220, 700, 100);

            var resume = UIBuilder.MakeButton(panel.transform, "ResumeButton", "DEVAM ET", new Color(0.2f, 0.7f, 0.3f));
            Center(resume.gameObject, 0, 60, 460, 110);
            var settings = UIBuilder.MakeButton(panel.transform, "PauseSettingsButton", "AYARLAR", new Color(0.4f, 0.45f, 0.5f));
            Center(settings.gameObject, 0, -70, 460, 110);
            var menu = UIBuilder.MakeButton(panel.transform, "PauseMenuButton", "ANA MENÜ", new Color(0.7f, 0.35f, 0.3f));
            Center(menu.gameObject, 0, -200, 460, 110);

            panel.gameObject.SetActive(false);
        }

        private static void BuildGameOverPanel(Transform c)
        {
            var panel = UIBuilder.MakeImage(c, "GameOverPanel", new Color(0.05f, 0.05f, 0.08f, 0.85f), false);
            UIBuilder.Stretch(UIBuilder.Rect(panel.gameObject));

            var title = UIBuilder.MakeText(panel.transform, "Title", "OYUN BİTTİ", 80, new Color(1f, 0.4f, 0.3f));
            Center(title.gameObject, 0, 280, 800, 110);

            var dist = UIBuilder.MakeText(panel.transform, "GO_DistanceText", "Mesafe: 0 m", 48, Color.white);
            Center(dist.gameObject, 0, 140, 700, 70);
            var coin = UIBuilder.MakeText(panel.transform, "GO_CoinText", "Coin: +0", 48, new Color(1f, 0.9f, 0.4f));
            Center(coin.gameObject, 0, 60, 700, 70);
            var best = UIBuilder.MakeText(panel.transform, "GO_BestText", "Rekor: 0 m", 36, new Color(0.7f, 0.8f, 0.9f));
            Center(best.gameObject, 0, -10, 700, 60);

            var restart = UIBuilder.MakeButton(panel.transform, "RestartButton", "YENİDEN", new Color(0.2f, 0.7f, 0.3f));
            Center(restart.gameObject, 0, -120, 460, 110);
            var upgrade = UIBuilder.MakeButton(panel.transform, "UpgradeButton", "GELİŞTİRME", new Color(0.55f, 0.4f, 0.8f));
            Center(upgrade.gameObject, 0, -250, 460, 110);
            var menu = UIBuilder.MakeButton(panel.transform, "GameOverMenuButton", "ANA MENÜ", new Color(0.7f, 0.35f, 0.3f));
            Center(menu.gameObject, 0, -380, 460, 110);

            panel.gameObject.SetActive(false);
        }

        // ===================================================================
        //  PROJE AYARLARI
        // ===================================================================
        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "MoonWorkshop";
            PlayerSettings.productName = "Top Climbing";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Game.unity", true),
            };
        }
    }
}
