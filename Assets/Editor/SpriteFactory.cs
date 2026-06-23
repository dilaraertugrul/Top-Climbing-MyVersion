using System.IO;
using UnityEditor;
using UnityEngine;

namespace TopClimbing.EditorTools
{
    /// <summary>
    /// Hazır görsel olmadan, prosedürel olarak PNG sprite'lar üretir ve
    /// Assets/Art klasörüne kaydeder. Tüm sprite'lar gerçek dosya olarak oluşur.
    /// </summary>
    public static class SpriteFactory
    {
        public const string ArtFolder = "Assets/Art";

        // ---------- Genel kaydetme ----------
        private static Sprite SaveSprite(string name, Texture2D tex, int ppu = 100, Vector2? pivot = null,
                                         Vector4? border = null, bool fullRect = false)
        {
            if (!Directory.Exists(ArtFolder)) Directory.CreateDirectory(ArtFolder);
            string path = $"{ArtFolder}/{name}.png";

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot ?? new Vector2(0.5f, 0.5f);
            if (border.HasValue) settings.spriteBorder = border.Value;
            if (fullRect) settings.spriteMeshType = SpriteMeshType.FullRect; // Tiled çizim için gerekli
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var clear = new Color[w * h];
            t.SetPixels(clear); // tamamı şeffaf
            return t;
        }

        // ---------- Çizim yardımcıları ----------
        private static void FillRect(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(t.height - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(t.width - 1, x1); x++)
                    t.SetPixel(x, y, c);
        }

        private static void FillRoundRect(Texture2D t, int x0, int y0, int x1, int y1, int radius, Color c)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (x < 0 || y < 0 || x >= t.width || y >= t.height) continue;
                    int dx = 0, dy = 0;
                    if (x < x0 + radius) dx = x0 + radius - x;
                    else if (x > x1 - radius) dx = x - (x1 - radius);
                    if (y < y0 + radius) dy = y0 + radius - y;
                    else if (y > y1 - radius) dy = y - (y1 - radius);
                    if (dx * dx + dy * dy <= radius * radius) t.SetPixel(x, y, c);
                }
            }
        }

        private static void FillCircle(Texture2D t, int cx, int cy, float r, Color c)
        {
            for (int y = (int)(cy - r); y <= cy + r; y++)
                for (int x = (int)(cx - r); x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= t.width || y >= t.height) continue;
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) t.SetPixel(x, y, c);
                }
        }

        private static void Ring(Texture2D t, int cx, int cy, float rOuter, float rInner, Color c)
        {
            for (int y = (int)(cy - rOuter); y <= cy + rOuter; y++)
                for (int x = (int)(cx - rOuter); x <= cx + rOuter; x++)
                {
                    if (x < 0 || y < 0 || x >= t.width || y >= t.height) continue;
                    float dx = x - cx, dy = y - cy;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= rOuter * rOuter && d2 >= rInner * rInner) t.SetPixel(x, y, c);
                }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        /// <summary>Üçgen dolu boyar (eğimli kaput/cam vb. için).</summary>
        private static void FillTriangle(Texture2D t, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int minX = Mathf.Max(0, (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.Min(t.width - 1, (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.Max(0, (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.Min(t.height - 1, (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x, y);
                    float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0;
                    bool pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) t.SetPixel(x, y, col); // tüm işaretler aynı -> içeride
                }
        }

        // ---------- Oyun sprite'ları ----------
        public static Sprite CreateCarBody()
        {
            int w = 256, h = 150;
            var t = NewTex(w, h);
            Color body = Color.white;                          // tint (araç rengi) ile renklenir
            Color outline = new Color(0.12f, 0.12f, 0.14f);
            Color shade = new Color(0.55f, 0.55f, 0.58f);      // alt şasi gölgesi
            Color light = new Color(1f, 0.93f, 0.55f);         // far
            Color tail = new Color(0.9f, 0.25f, 0.2f);         // stop lambası

            // --- Ana gövde (tub) ---
            FillRoundRect(t, 14, 44, 242, 102, 18, outline);
            FillRoundRect(t, 18, 48, 238, 98, 16, body);
            // alt koyu şasi şeridi
            FillRect(t, 22, 48, 234, 60, shade);

            // --- Ön kaput eğimi (sağ, alçalan kama) ---
            FillTriangle(t, new Vector2(164, 100), new Vector2(164, 62), new Vector2(244, 86), outline);
            FillTriangle(t, new Vector2(167, 96), new Vector2(167, 66), new Vector2(236, 86), body);

            // --- Arka yükselti (sol, motor/bagaj) ---
            FillRoundRect(t, 18, 86, 86, 120, 12, outline);
            FillRoundRect(t, 22, 90, 82, 116, 10, body);

            // --- Açık roll-cage kabin (sürücü görünür) ---
            FillRect(t, 88, 98, 100, 142, outline);   // arka direk
            FillRect(t, 150, 98, 162, 142, outline);  // ön direk (cam çerçevesi)
            FillRect(t, 88, 134, 162, 144, outline);  // tavan barı
            FillTriangle(t, new Vector2(150, 142), new Vector2(150, 110), new Vector2(176, 100), outline); // ön cam eğimi

            // --- Farlar / stoplar ---
            FillCircle(t, 234, 82, 8, outline);
            FillCircle(t, 234, 82, 6, light);
            FillCircle(t, 24, 100, 6, outline);
            FillCircle(t, 24, 100, 4, tail);

            t.Apply();
            return SaveSprite("Sprite_CarBody", t, 100);
        }

        /// <summary>Hill Climb tarzı sürücü: turuncu mont, beyaz/kırmızı kask + vizör, öne uzanan kol.</summary>
        public static Sprite CreateDriver()
        {
            int w = 120, h = 150;
            var t = NewTex(w, h);
            int cx = 60;
            Color skin = new Color(0.96f, 0.78f, 0.62f);
            Color jacket = new Color(0.95f, 0.55f, 0.12f);     // turuncu mont
            Color jacketDark = new Color(0.74f, 0.40f, 0.06f);
            Color helmet = new Color(0.96f, 0.96f, 0.98f);     // beyaz kask
            Color helmetTrim = new Color(0.85f, 0.22f, 0.16f); // kırmızı bant
            Color visor = new Color(0.18f, 0.22f, 0.30f);      // koyu vizör

            // Gövde (mont) - hafif öne eğik
            FillRoundRect(t, cx - 28, 6, cx + 30, 72, 16, jacketDark);
            FillRoundRect(t, cx - 24, 10, cx + 26, 70, 14, jacket);
            // Kol öne (direksiyona uzanır)
            FillRoundRect(t, cx + 6, 26, cx + 52, 50, 11, jacketDark);
            FillRoundRect(t, cx + 8, 28, cx + 50, 48, 9, jacket);
            FillCircle(t, cx + 50, 38, 10, skin);   // el / eldiven

            // Boyun
            FillRect(t, cx - 8, 64, cx + 10, 82, skin);
            // Baş
            FillCircle(t, cx, 94, 19, skin);
            // Kask (üst kabuk + alt çene hattı)
            FillCircle(t, cx, 100, 25, helmet);
            FillRect(t, cx - 25, 90, cx + 25, 102, helmet);
            FillRect(t, cx - 25, 88, cx + 25, 93, helmetTrim);  // kırmızı bant
            Ring(t, cx, 100, 25, 22, helmetTrim);               // kask kenar çizgisi
            // Vizör (ön açıklık)
            FillRoundRect(t, cx - 20, 90, cx + 13, 102, 5, visor);
            t.Apply();
            return SaveSprite("Sprite_Driver", t, 100);
        }

        public static Sprite CreateWheel()
        {
            int s = 128;
            var t = NewTex(s, s);
            int c = s / 2;
            Color tire = new Color(0.11f, 0.11f, 0.12f);
            Color tread = new Color(0.04f, 0.04f, 0.05f);
            Color rim = new Color(0.82f, 0.82f, 0.85f);
            Color rimDark = new Color(0.45f, 0.45f, 0.48f);

            // Arazi lastiği: knobby diş blokları (dış halkada)
            for (int a = 0; a < 18; a++)
            {
                float ang = a * Mathf.PI * 2f / 18f;
                int x = c + (int)(Mathf.Cos(ang) * 56);
                int y = c + (int)(Mathf.Sin(ang) * 56);
                FillCircle(t, x, y, 7, tread);
            }
            FillCircle(t, c, c, 58, tire);          // lastik
            Ring(t, c, c, 58, 50, tread);           // dış sırt (tread bandı)
            // Jant
            FillCircle(t, c, c, 38, rimDark);
            FillCircle(t, c, c, 34, rim);
            // Jant kolları (5 kollu spor jant)
            for (int a = 0; a < 5; a++)
            {
                float ang = a * Mathf.PI * 2f / 5f;
                int x = c + (int)(Mathf.Cos(ang) * 22);
                int y = c + (int)(Mathf.Sin(ang) * 22);
                FillCircle(t, x, y, 6, rimDark);
            }
            FillCircle(t, c, c, 11, rimDark);       // göbek
            FillCircle(t, c, c, 5, rim);            // bijon
            t.Apply();
            return SaveSprite("Sprite_Wheel", t, 100);
        }

        public static Sprite CreateCoin()
        {
            int s = 80;
            var t = NewTex(s, s);
            int c = s / 2;
            FillCircle(t, c, c, 36, new Color(0.85f, 0.65f, 0.1f));   // dış altın
            FillCircle(t, c, c, 30, new Color(1f, 0.85f, 0.25f));     // iç altın
            Ring(t, c, c, 30, 26, new Color(0.95f, 0.78f, 0.2f));
            // Ortada basit yıldız/işaret
            FillRect(t, c - 3, c - 14, c + 3, c + 14, new Color(0.8f, 0.6f, 0.1f));
            FillRect(t, c - 14, c - 3, c + 14, c + 3, new Color(0.8f, 0.6f, 0.1f));
            t.Apply();
            return SaveSprite("Sprite_Coin", t, 100);
        }

        public static Sprite CreateFuelCan()
        {
            int w = 80, h = 96;
            var t = NewTex(w, h);
            Color red = new Color(0.85f, 0.2f, 0.15f);
            Color dark = new Color(0.6f, 0.12f, 0.1f);
            FillRoundRect(t, 12, 8, w - 12, h - 20, 8, red);   // gövde
            FillRoundRect(t, 16, 12, w - 16, h - 24, 6, dark); // panel
            FillRoundRect(t, 22, 18, w - 22, h - 30, 4, red);
            FillRect(t, w - 26, h - 22, w - 8, h - 10, dark);  // ağız
            FillRect(t, 24, h - 18, w - 30, h - 12, dark);     // sap
            // "F" işareti
            FillRect(t, 32, 30, 36, 64, Color.white);
            FillRect(t, 32, 60, 50, 64, Color.white);
            FillRect(t, 32, 44, 46, 48, Color.white);
            t.Apply();
            return SaveSprite("Sprite_FuelCan", t, 100);
        }

        /// <summary>Beyaz, yuvarlatılmış UI paneli (renk UI'da tint ile verilir).</summary>
        public static Sprite CreatePanel()
        {
            int s = 64;
            var t = NewTex(s, s);
            FillRoundRect(t, 0, 0, s - 1, s - 1, 16, Color.white);
            t.Apply();
            // 9-slice border: köşeler bozulmadan ölçeklenir
            return SaveSprite("Sprite_Panel", t, 100, new Vector2(0.5f, 0.5f), new Vector4(20, 20, 20, 20));
        }

        /// <summary>Düz beyaz kare (UI arka planı / dolgu).</summary>
        public static Sprite CreateWhite()
        {
            var t = NewTex(8, 8);
            FillRect(t, 0, 0, 7, 7, Color.white);
            t.Apply();
            return SaveSprite("Sprite_White", t, 100);
        }

        /// <summary>Yön oklu yuvarlak buton (gaz/fren). dir>0 sağ, dir<0 sol.</summary>
        public static Sprite CreatePedal(string name, int dir)
        {
            int s = 160;
            var t = NewTex(s, s);
            int c = s / 2;
            FillCircle(t, c, c, 72, new Color(1f, 1f, 1f, 0.9f));
            Ring(t, c, c, 72, 64, new Color(0f, 0f, 0f, 0.4f));
            // Üçgen ok
            Color arrow = new Color(0.1f, 0.1f, 0.1f);
            for (int y = -36; y <= 36; y++)
            {
                int half = 36 - Mathf.Abs(y);
                int len = (int)(half * 1.1f);
                for (int x = 0; x <= len; x++)
                {
                    int px = c + dir * (x - 14);
                    int py = c + y;
                    if (px >= 0 && py >= 0 && px < s && py < s) t.SetPixel(px, py, arrow);
                }
            }
            t.Apply();
            return SaveSprite(name, t, 100);
        }

        /// <summary>
        /// Geniş, tekrarlanabilir tepe silüeti (paralaks). Renk tint ile verilir.
        /// Profil ÜSTTE, altı tamamen dolu ve sprite çok uzun -> alt kenar daima ekran
        /// dibinin altında kalır (vadilerde "kesik düz alt" görünmez). Tam periyotlu -> kusursuz tekrar.
        /// </summary>
        public static Sprite CreateHillLayer(string name, float amplitude, float baseFreq)
        {
            int w = 2560, h = 1280;        // çok uzun: dolgu ekran dibinin altına iner
            const float baseline = 1040f;  // tepe çizgisi üst tarafa yakın; üstü gökyüzü (şeffaf)
            var t = NewTex(w, h);
            var px = t.GetPixels();        // NewTex tamamı şeffaf -> bu kopya da şeffaf
            Color fill = Color.white;
            for (int x = 0; x < w; x++)
            {
                float p = (float)x / w * Mathf.PI * 2f;
                float hh = baseline + Mathf.Sin(p * 3f) * amplitude + Mathf.Sin(p * 7f + 1f) * amplitude * 0.4f;
                int top = Mathf.Clamp(Mathf.RoundToInt(hh), 0, h);
                for (int y = 0; y < top; y++) px[y * w + x] = fill; // tabandan tepeye kadar dolu
            }
            t.SetPixels(px);
            t.Apply();
            return SaveSprite(name, t, 100, null, null, true);
        }

        /// <summary>Koyu, atmosferik ana menü arka planı: dikey degrade + yıldızlar + ay + tepe silüetleri.</summary>
        public static Sprite CreateMenuBackground()
        {
            int w = 1280, h = 720;
            var t = NewTex(w, h);
            var px = t.GetPixels();
            Color top = new Color(0.05f, 0.06f, 0.14f);     // koyu lacivert (üst)
            Color bottom = new Color(0.13f, 0.17f, 0.32f);  // ufukta biraz açık
            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / (h - 1);
                Color col = Color.Lerp(bottom, top, ty);    // alt açık -> üst koyu
                for (int x = 0; x < w; x++) px[y * w + x] = col;
            }
            t.SetPixels(px);

            // Yıldızlar (deterministik dağılım, üst kısımda)
            var rng = new System.Random(2024);
            for (int i = 0; i < 160; i++)
            {
                int sx = rng.Next(0, w);
                int sy = rng.Next(h / 3, h);
                float b = 0.55f + (float)rng.NextDouble() * 0.45f;
                Color star = new Color(b, b, b * 1.05f, 1f);
                t.SetPixel(sx, sy, star);
                if (rng.NextDouble() > 0.65) // bazıları iri/parlak
                {
                    t.SetPixel(Mathf.Min(sx + 1, w - 1), sy, star);
                    t.SetPixel(sx, Mathf.Min(sy + 1, h - 1), star);
                }
            }

            // Ay (yumuşak hâle + gövde)
            int mx = w - 220, my = h - 150;
            FillCircle(t, mx, my, 64, new Color(0.85f, 0.88f, 0.95f, 0.10f)); // hâle
            FillCircle(t, mx, my, 46, new Color(0.93f, 0.94f, 0.88f));        // gövde

            // Tepe silüetleri (iki katman -> derinlik)
            DrawHillSilhouette(t, 0.22f, 70f, 2f, new Color(0.10f, 0.12f, 0.22f));
            DrawHillSilhouette(t, 0.13f, 48f, 3f, new Color(0.05f, 0.07f, 0.15f));

            t.Apply();
            return SaveSprite("Sprite_MenuBg", t, 100);
        }

        /// <summary>Arka plana, tabandan yukarı tepe silüeti boyar (menü dekoru).</summary>
        private static void DrawHillSilhouette(Texture2D t, float baseFrac, float amp, float freq, Color col)
        {
            int w = t.width, h = t.height;
            for (int x = 0; x < w; x++)
            {
                float p = (float)x / w * Mathf.PI * 2f;
                float hh = h * baseFrac + Mathf.Sin(p * freq) * amp + Mathf.Sin(p * freq * 2.3f + 1f) * amp * 0.4f;
                int top = Mathf.Clamp((int)hh, 0, h);
                for (int y = 0; y < top; y++) t.SetPixel(x, y, col);
            }
        }

        /// <summary>Tekrarlanabilir bulut bandı (paralaks).</summary>
        public static Sprite CreateCloudLayer()
        {
            int w = 2560, h = 400;
            var t = NewTex(w, h);
            // x, y, ölçek -> farklı boyut/yükseklikte bulutlar (tekrar daha az belli olur).
            // x'ler kenarlardan (0 ve 2560) uzak tutuldu -> yatay dikiş kusursuz.
            Vector3[] clouds =
            {
                new Vector3(190, 300, 1.15f), new Vector3(560, 230, 0.8f),
                new Vector3(900, 330, 1.0f),  new Vector3(1280, 260, 1.3f),
                new Vector3(1650, 320, 0.75f),new Vector3(2010, 250, 1.1f),
                new Vector3(2360, 300, 0.9f)
            };
            Color soft = new Color(1, 1, 1, 0.92f);
            Color shade = new Color(0.86f, 0.90f, 0.96f, 0.92f); // hafif alt gölge -> hacim
            foreach (var cl in clouds)
            {
                int cx = (int)cl.x, cy = (int)cl.y; float s = cl.z;
                // alt gölge katmanı
                FillCircle(t, cx, cy - 4, 30 * s, shade);
                FillCircle(t, cx + (int)(28 * s), cy - 8, 22 * s, shade);
                // ana gövde
                FillCircle(t, cx, cy, 30 * s, soft);
                FillCircle(t, cx + (int)(28 * s), cy - 4, 23 * s, soft);
                FillCircle(t, cx - (int)(28 * s), cy - 2, 21 * s, soft);
                FillCircle(t, cx + (int)(8 * s), cy + (int)(12 * s), 25 * s, soft);
                FillCircle(t, cx - (int)(14 * s), cy + (int)(8 * s), 18 * s, soft);
            }
            t.Apply();
            return SaveSprite("Sprite_Clouds", t, 100, null, null, true);
        }
    }
}
