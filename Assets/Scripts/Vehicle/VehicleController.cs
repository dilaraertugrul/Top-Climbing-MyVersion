using UnityEngine;

namespace TopClimbing
{
    [RequireComponent(typeof(FuelSystem))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Fizik Referansları (boşsa çocuk objelerden bulunur)")]
        public Rigidbody2D bodyRb;
        public Rigidbody2D rearWheelRb;
        public Rigidbody2D frontWheelRb;
        public WheelJoint2D rearJoint;
        public WheelJoint2D frontJoint;
        [Tooltip("Sürücünün kafası: bu collider katı zemine değince oyun biter.")]
        public Collider2D headCollider;

        [Header("Sürüş Ayarları")]
        public float airTorque = 70f;               // HAVADAyken denge/takla torku (gaz=geri, fren=ileri)
        public float groundTorque = 6f;             // YERDE nazik wheelie torku (tilt ile sınırlı)
        public float groundTiltLimitDeg = 32f;       // Yerde gövde bu açıdan fazla yatıkken tork eklenmez (geri taklayı önler)
        public float maxAngularSpeedDeg = 200f;      // Gövdenin maksimum dönme hızı (deg/s)
        public Vector2 centerOfMass = new Vector2(0f, -0.55f); // Alçak AM: stabil ama büyük zıplamada devrilebilir
        public float bodyAngularDrag = 1.6f;         // Tumbling'i sönümler ama doğal kalır

        [Header("Toprak Efekti")]
        public float dirtStruggleSpeed = 3.5f;       // Bu hızın altında gaz verince zorlanıyor sayılır

        private FuelSystem _fuel;
        private VehicleDefinition _def;
        private ParticleSystem _rearDirt;
        private ParticleSystem _frontDirt;
        private Collider2D _rearWheelCol;
        private Collider2D _frontWheelCol;
        private Collider2D _bodyMainCol;          // Gövdenin katı (trigger olmayan) collider'ı
        private ContactFilter2D _groundFilter;    // Sadece katı zemin (coin/yakıt trigger'ları sayılmaz)

        private float _motorTorque;   // Upgrade sonrası tork
        private float _motorSpeed;    // Upgrade sonrası hız
        private float _maxSpeed;      // İleri hız sınırı (m/s)
        private bool _alive = true;

        // Tekerleğin sağa gitmek için saat yönünde dönmesi gerekir -> negatif açısal hız.
        private const float ForwardSign = -1f;

        public float Speed => bodyRb != null ? bodyRb.velocity.magnitude : 0f;
        public float SpeedKmh => Speed * 3.6f;

        private void Awake()
        {
            _fuel = GetComponent<FuelSystem>();
            AutoWireReferences();
            IgnoreInternalCollisions();

            if (rearWheelRb != null) _rearWheelCol = rearWheelRb.GetComponent<Collider2D>();
            if (frontWheelRb != null) _frontWheelCol = frontWheelRb.GetComponent<Collider2D>();

            // Gövdenin KATI collider'ı (geniş pickup trigger'ını değil) -> düşme tespiti
            if (bodyRb != null)
                foreach (var c in bodyRb.GetComponents<Collider2D>())
                    if (!c.isTrigger) { _bodyMainCol = c; break; }

            // Kafa collider'ı (prefab'da bağlı; değilse çocuk 'Head'den bul)
            if (headCollider == null)
            {
                var h = transform.Find("Head");
                if (h != null) headCollider = h.GetComponent<Collider2D>();
            }

            // Yalnızca katı zemine değmeyi say; coin/yakıt gibi trigger'ları yok say.
            _groundFilter = new ContactFilter2D { useTriggers = false };
            _groundFilter.SetLayerMask(Physics2D.AllLayers);

            // Tekerlek altına toprak fışkırma efekti
            if (rearWheelRb != null) _rearDirt = CreateDirtEffect(rearWheelRb.transform);
            if (frontWheelRb != null) _frontDirt = CreateDirtEffect(frontWheelRb.transform);
        }

        /// <summary>
        /// Gövde ile tekerleklerin birbirine fiziksel olarak çarpmasını engeller.
        /// (Aksi halde collider'lar üst üste binince araç zıplar/aşağı fırlar.)
        /// Tekerlekler ve gövde yalnızca zemine çarpar.
        /// </summary>
        private void IgnoreInternalCollisions()
        {
            if (bodyRb == null) return;

            var bodyCols = bodyRb.GetComponents<Collider2D>();
            var wheelCols = new System.Collections.Generic.List<Collider2D>();
            if (rearWheelRb != null) wheelCols.AddRange(rearWheelRb.GetComponents<Collider2D>());
            if (frontWheelRb != null) wheelCols.AddRange(frontWheelRb.GetComponents<Collider2D>());

            // Gövde collider'ları <-> tekerlek collider'ları arası çarpışmayı kapat
            foreach (var b in bodyCols)
                foreach (var w in wheelCols)
                    if (b != null && w != null) Physics2D.IgnoreCollision(b, w, true);

            // Tekerlekler birbirine de çarpmasın
            for (int i = 0; i < wheelCols.Count; i++)
                for (int j = i + 1; j < wheelCols.Count; j++)
                    Physics2D.IgnoreCollision(wheelCols[i], wheelCols[j], true);
        }

        /// <summary>Serileştirilmemiş referansları çocuk objelerden isimle bulur (güvenli yedek).</summary>
        private void AutoWireReferences()
        {
            if (bodyRb == null) bodyRb = GetComponent<Rigidbody2D>();
            var joints = GetComponents<WheelJoint2D>();
            foreach (var j in joints)
            {
                if (j.connectedBody == null) continue;
                if (j.connectedBody.name.ToLower().Contains("rear")) rearJoint = j;
                else if (j.connectedBody.name.ToLower().Contains("front")) frontJoint = j;
            }
            if (rearJoint != null) rearWheelRb = rearJoint.connectedBody;
            if (frontJoint != null) frontWheelRb = frontJoint.connectedBody;
        }

        /// <summary>Seçili araç tanımına ve upgrade seviyelerine göre aracı kurar.</summary>
        public void Setup(VehicleDefinition def)
        {
            _def = def;

            int engineLvl = SaveSystem.GetUpgradeLevel(UpgradeType.Engine);
            int suspLvl = SaveSystem.GetUpgradeLevel(UpgradeType.Suspension);
            int tireLvl = SaveSystem.GetUpgradeLevel(UpgradeType.Tires);

            float engineMult = UpgradeCatalog.EngineMultiplier(engineLvl);
            _motorTorque = def.baseMotorTorque * engineMult;
            _motorSpeed = def.baseMotorSpeed * engineMult;
            _maxSpeed = Mathf.Lerp(9f, 22f, def.maxSpeed) * (1f + 0.05f * engineLvl);

            // Gövde kütlesi (ağırlık) + alçak ağırlık merkezi + dönme sönümü -> devrilmeye direnç
            if (bodyRb != null)
            {
                bodyRb.mass = def.bodyMass;
                bodyRb.centerOfMass = centerOfMass;
                bodyRb.angularDrag = bodyAngularDrag;
            }

            // Görsel: renk ve ölçek
            var bodySprite = GetComponentInChildren<SpriteRenderer>();
            if (bodySprite != null) bodySprite.color = def.bodyColor;

            // Süspansiyon (frekans) ve lastik (sürtünme) upgrade'leri uygula
            float freq = UpgradeCatalog.SuspensionFrequency(suspLvl);
            ApplySuspension(rearJoint, freq);
            ApplySuspension(frontJoint, freq);

            float friction = UpgradeCatalog.TireFriction(tireLvl);
            ApplyWheelFriction(rearWheelRb, friction);
            ApplyWheelFriction(frontWheelRb, friction);
        }

        private void ApplySuspension(WheelJoint2D joint, float frequency)
        {
            if (joint == null) return;
            var s = joint.suspension;
            s.frequency = frequency;
            s.dampingRatio = 0.7f;
            s.angle = 90f; // dikey süspansiyon
            joint.suspension = s;
        }

        private void ApplyWheelFriction(Rigidbody2D wheel, float friction)
        {
            if (wheel == null) return;
            var col = wheel.GetComponent<Collider2D>();
            if (col == null) return;
            // Paylaşılan asset'i bozmamak için yeni bir materyal örneği ata.
            col.sharedMaterial = new PhysicsMaterial2D("WheelMat_Runtime") { friction = friction, bounciness = 0f };
        }

        private void Update()
        {
            // Motor sesini gaz ve hıza göre güncelle
            if (AudioManager.Instance != null && _alive)
            {
                float gas = InputManager.Instance != null ? InputManager.Instance.Gas : 0f;
                float speed01 = _maxSpeed > 0f ? Mathf.Clamp01(Speed / _maxSpeed) : 0f;
                AudioManager.Instance.UpdateEngine(gas, speed01);
            }
        }

        private void FixedUpdate()
        {
            if (!_alive) return;

            float gas = 0f, brake = 0f;
            if (InputManager.Instance != null)
            {
                gas = InputManager.Instance.Gas;
                brake = InputManager.Instance.Brake;
            }

            bool hasFuel = _fuel != null && _fuel.HasFuel;
            float input = gas - brake; // +ileri, -geri

            // --- Yakıt tüketimi ---
            if (_fuel != null)
            {
                float consume = GameConfig.IdleFuelPerSecond + gas * GameConfig.GasFuelPerSecond;
                _fuel.Consume(consume * Time.fixedDeltaTime);
            }

            // --- Tekerlek motoru ---
            ApplyMotor(rearJoint, input, hasFuel);
            ApplyMotor(frontJoint, input, hasFuel);

            // --- Gövde torku ---
            // Gaz (input>0) -> nose-up (wheelie), Fren (input<0) -> nose-down (öne yatma).
            // Havada: tam kontrol (takla/dengeleme, düz inişe yardım).
            // Yerde: yalnızca NAZİK ve TILT-SINIRLI tork -> kendi kendine geri takla atamaz.
            if (bodyRb != null && Mathf.Abs(input) > 0.01f)
            {
                bool airborne = !IsGrounded(_rearWheelCol) && !IsGrounded(_frontWheelCol);
                if (airborne)
                {
                    bodyRb.AddTorque(input * airTorque, ForceMode2D.Force);
                }
                else
                {
                    // Gövdenin dikeyden işaretli sapması (+ = burun yukarı, araç sağa bakarken).
                    float tilt = Vector2.SignedAngle(Vector2.up, transform.up);
                    // Gaz burnu kaldırırken çok yatıksa, fren burnu indirirken çok öne yatıksa tork ekleme.
                    bool block = (input > 0f && tilt > groundTiltLimitDeg) ||
                                 (input < 0f && tilt < -groundTiltLimitDeg);
                    if (!block)
                        bodyRb.AddTorque(input * groundTorque, ForceMode2D.Force);
                }
            }

            // --- İleri hız sınırı (yumuşak): momentumu ani kesmeden tavana yaklaştır ---
            if (bodyRb != null && bodyRb.velocity.x > _maxSpeed)
            {
                float vx = Mathf.Lerp(bodyRb.velocity.x, _maxSpeed, 0.08f);
                bodyRb.velocity = new Vector2(vx, bodyRb.velocity.y);
            }

            // --- Dönme hızını sınırla (kontrolsüz takla atmasın) ---
            if (bodyRb != null)
            {
                bodyRb.angularVelocity = Mathf.Clamp(bodyRb.angularVelocity, -maxAngularSpeedDeg, maxAngularSpeedDeg);
            }

            // --- Toprak efekti (zorlanırken) ---
            UpdateDirt(Mathf.Abs(input), hasFuel);
        }

        /// <summary>
        /// Araç gaz/fren altında düşük hızda (yokuş/engebede zorlanırken) ve tekerlek
        /// yere değerken, tekerlek altından toprak fışkırtır.
        /// </summary>
        private void UpdateDirt(float inputAbs, bool hasFuel)
        {
            bool struggling = hasFuel && inputAbs > 0.1f && Speed < dirtStruggleSpeed;
            EmitDirt(_rearDirt, struggling && IsGrounded(_rearWheelCol));
            EmitDirt(_frontDirt, struggling && IsGrounded(_frontWheelCol));
        }

        private bool IsGrounded(Collider2D wheelCol)
        {
            return wheelCol != null && wheelCol.IsTouchingLayers();
        }

        /// <summary>
        /// Araç katı zemine değiyor mu? (Devrilince zemine düşme tespiti.)
        /// Gövde ya da tekerleklerden biri yere değiyorsa true. Trigger'lar (coin/yakıt) sayılmaz.
        /// </summary>
        public bool IsTouchingGround()
        {
            if (_bodyMainCol != null && _bodyMainCol.IsTouching(_groundFilter)) return true;
            if (_rearWheelCol != null && _rearWheelCol.IsTouching(_groundFilter)) return true;
            if (_frontWheelCol != null && _frontWheelCol.IsTouching(_groundFilter)) return true;
            return false;
        }

        /// <summary>
        /// Sürücünün kafası katı zemine değiyor mu? Oyun bitiş koşulu (Hill Climb tarzı).
        /// Trigger'lar (coin/yakıt) sayılmaz; yalnızca gerçek zemin teması.
        /// </summary>
        public bool IsHeadTouchingGround()
        {
            return headCollider != null && headCollider.IsTouching(_groundFilter);
        }

        private void EmitDirt(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.rateOverTime = on ? 45f : 0f;
        }

        /// <summary>Tekerlek altına, çalışma anında bir toprak parçacık sistemi oluşturur.</summary>
        private ParticleSystem CreateDirtEffect(Transform wheel)
        {
            var go = new GameObject("DirtFX");
            go.transform.SetParent(wheel, false);
            go.transform.localPosition = new Vector3(0f, -0.45f, 0f); // tekerleğin altı
            // Koni varsayılan +Z'ye püskürtür; 2D'de yukarı (+Y) baksın diye -90 döndür.
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 2.2f;
            main.startSize = 0.14f;
            main.gravityModifier = 1.4f;            // toprak yere düşsün
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // geride kalsın
            main.startColor = new Color(0.45f, 0.32f, 0.2f);            // toprak rengi

            var emission = ps.emission;
            emission.rateOverTime = 0f; // gerektiğinde açılır

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 0.12f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.sortingOrder = 3;
            }

            ps.Play();
            return ps;
        }

        private void ApplyMotor(WheelJoint2D joint, float input, bool hasFuel)
        {
            if (joint == null) return;

            if (!hasFuel || Mathf.Abs(input) < 0.01f)
            {
                // Girdi yok veya yakıt yok -> serbest dönüş (coast)
                joint.useMotor = false;
                return;
            }

            joint.useMotor = true;
            var motor = joint.motor;
            // İleri (input>0) -> tekerlek saat yönünde -> negatif hız.
            motor.motorSpeed = ForwardSign * input * _motorSpeed;
            motor.maxMotorTorque = _motorTorque;
            joint.motor = motor;
        }

        /// <summary>Oyun bittiğinde motoru ve girdiyi durdurur.</summary>
        public void OnGameOver()
        {
            _alive = false;
            if (rearJoint != null) rearJoint.useMotor = false;
            if (frontJoint != null) frontJoint.useMotor = false;
        }
    }
}
