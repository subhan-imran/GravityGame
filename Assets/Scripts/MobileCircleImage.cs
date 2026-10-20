using UnityEngine;
using UnityEngine.UI;

namespace GravityGame
{
    // Mesh-based circles avoid editor-only skin sprites and ship without extra texture assets.
    public sealed class MobileCircleImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            const int segments = 80;
            mesh.AddVert(center, color, Vector2.one * 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + normal * Mathf.Max(0f, radius - 1f), color, Vector2.one * 0.5f);
                Color transparent = color;
                transparent.a = 0f;
                mesh.AddVert(center + normal * radius, transparent, Vector2.one * 0.5f);
            }
            for (int i = 0; i < segments; i++)
            {
                int inner = 1 + i * 2;
                int next = 1 + ((i + 1) % segments) * 2;
                mesh.AddTriangle(0, inner, next);
                mesh.AddTriangle(inner, inner + 1, next + 1);
                mesh.AddTriangle(inner, next + 1, next);
            }
        }
    }
}
