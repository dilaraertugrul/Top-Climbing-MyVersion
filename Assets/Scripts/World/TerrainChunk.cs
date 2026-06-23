using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopClimbing
{
    /// <summary>
    /// Tek bir arazi parçası: prosedürel mesh (dolgu) + EdgeCollider2D (zemin) +
    /// yüzey çizgisi (LineRenderer). Yeniden konumlandırılarak (recycle) tekrar kullanılır.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(EdgeCollider2D))]
    public class TerrainChunk : MonoBehaviour
    {
        public float XStart { get; private set; }
        public float Width { get; private set; }
        public float XEnd => XStart + Width;

        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private EdgeCollider2D _edge;
        private LineRenderer _line;
        private Mesh _mesh;

        private const float BottomY = -30f; // dolgunun alt sınırı

        // Bu parçaya ait toplanmamış pickup'lar (recycle'da havuza döner)
        public readonly List<GameObject> Pickups = new List<GameObject>();

        private void EnsureComponents()
        {
            if (_filter != null) return;
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _edge = GetComponent<EdgeCollider2D>();
            _line = GetComponent<LineRenderer>();
            _mesh = new Mesh { name = "TerrainChunkMesh" };
            _filter.sharedMesh = _mesh;
        }

        /// <summary>
        /// Parçayı verilen başlangıç X'inde, yükseklik fonksiyonuna göre yeniden inşa eder.
        /// </summary>
        public void Build(float xStart, float width, int segments, Func<float, float> heightFn,
                          BiomePalette palette, PhysicsMaterial2D groundMat)
        {
            EnsureComponents();
            XStart = xStart;
            Width = width;
            transform.position = new Vector3(xStart, 0f, 0f);

            int pointCount = segments + 1;
            float step = width / segments;

            var vertices = new Vector3[pointCount * 2];
            var tris = new int[segments * 6];
            var edgePoints = new Vector2[pointCount];
            var linePoints = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float localX = i * step;
                float worldX = xStart + localX;
                float topY = heightFn(worldX);

                vertices[i * 2] = new Vector3(localX, topY, 0f);       // üst
                vertices[i * 2 + 1] = new Vector3(localX, BottomY, 0f); // alt
                edgePoints[i] = new Vector2(localX, topY);
                linePoints[i] = new Vector3(localX, topY, -0.1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int t = i * 6;
                int v = i * 2;
                tris[t] = v;
                tris[t + 1] = v + 2;
                tris[t + 2] = v + 1;
                tris[t + 3] = v + 1;
                tris[t + 4] = v + 2;
                tris[t + 5] = v + 3;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();

            _edge.points = edgePoints;
            _edge.sharedMaterial = groundMat;

            // Görsel renkler
            _renderer.material.color = palette.ground;
            if (_line != null)
            {
                _line.positionCount = pointCount;
                _line.SetPositions(linePoints);
                _line.startColor = _line.endColor = palette.groundTop;
            }
        }

        public float SampleEdgeHeightLocal(int index)
        {
            if (_edge != null && index >= 0 && index < _edge.pointCount)
                return _edge.points[index].y;
            return 0f;
        }
    }
}
