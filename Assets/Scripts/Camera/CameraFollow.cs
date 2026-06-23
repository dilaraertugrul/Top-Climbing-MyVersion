using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Aracı yumuşakça takip eden kamera. SmoothDamp ile titremesiz hareket eder,
    /// hıza göre hafifçe geriye bakar (look-ahead) ve dikeyde yumuşatılır.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;          // Takip edilecek araç gövdesi
        public Vector2 offset = new Vector2(3f, 2.5f);
        public float smoothTime = 0.18f;  // Yatay yumuşatma
        public float verticalSmoothTime = 0.35f;
        public float lookAheadFactor = 0.25f; // Hıza göre ileri bakış
        public float maxLookAhead = 5f;

        // Yeni alan -> kod varsayılanı uygulanır (sahnedeki eski değeri ezmeye gerek kalmaz).
        public float viewSize = 8f;       // Kamera görüş genişliği (orthographic size)

        private Camera _cam;
        private Rigidbody2D _targetRb;
        private Vector3 _velocity;        // SmoothDamp dahili hız
        private float _yVelocity;

        private void Start()
        {
            if (target == null)
            {
                var v = FindObjectOfType<VehicleController>();
                if (v != null) target = v.transform;
            }
            if (target != null) _targetRb = target.GetComponent<Rigidbody2D>();

            // Kamerayı uzaklaştır (araç her zaman görünsün)
            _cam = GetComponent<Camera>();
            if (_cam != null && _cam.orthographic) _cam.orthographicSize = viewSize;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Hıza bağlı ileri bakış
            float lookAhead = 0f;
            if (_targetRb != null)
                lookAhead = Mathf.Clamp(_targetRb.velocity.x * lookAheadFactor, -maxLookAhead, maxLookAhead);

            float targetX = target.position.x + offset.x + lookAhead;
            float targetY = target.position.y + offset.y;

            // X ve Y'yi ayrı ayrı yumuşat (titreme engellenir)
            float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref _velocity.x, smoothTime);
            float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref _yVelocity, verticalSmoothTime);

            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }
}
