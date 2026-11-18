using UnityEngine;

namespace GravityGame
{
    [RequireComponent(typeof(MeshFilter))]
    public sealed class CapeFlutter : MonoBehaviour
    {
        private MeshFilter filter;
        private Mesh originalMesh, animatedMesh;
        private Vector3[] restVertices, vertices;
        private SphericalPlayer player;
        private float phase, travel;
        private int respawnCount;

        private void OnEnable()
        {
            player = GetComponentInParent<SphericalPlayer>();
            filter = GetComponent<MeshFilter>();
            originalMesh = filter.sharedMesh;
            if (originalMesh == null) return;
            // One private mesh and two reusable buffers per cape; never modify the shared asset.
            animatedMesh = Instantiate(originalMesh);
            animatedMesh.name = originalMesh.name + " (flutter instance)";
            animatedMesh.MarkDynamic();
            restVertices = originalMesh.vertices;
            vertices = new Vector3[restVertices.Length];
            filter.sharedMesh = animatedMesh;
            phase = travel = 0f;
            respawnCount = player != null ? player.RespawnCount : 0;
        }

        private void LateUpdate() => Animate(Time.deltaTime);

        private void Animate(float dt)
        {
            if (animatedMesh == null || dt <= 0f || (player != null &&
                (player.IsPaused || player.HasLost || player.HasWon))) return;
            if (player != null && respawnCount != player.RespawnCount)
            {
                phase = travel = 0f;
                respawnCount = player.RespawnCount;
            }
            float target = player != null ? Mathf.Clamp01(player.Velocity.magnitude / Mathf.Max(0.1f, player.runSpeed)) : 0f;
            travel = Mathf.Lerp(travel, target, 1f - Mathf.Exp(-6f * dt));
            phase = Mathf.Repeat(phase + dt * Mathf.Lerp(2.5f, 7f, travel), Mathf.PI * 2f);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = restVertices[i];
                float weight = Mathf.Clamp01((0.22f - point.y) / 1.06f);
                weight *= weight; // Shoulder seam stays anchored.
                point.z += weight * (Mathf.Sin(phase - weight * 3f + point.x * 4f) *
                    Mathf.Lerp(0.018f, 0.065f, travel) - travel * 0.18f);
                point.x += Mathf.Sin(phase * 0.7f - weight * 2f) * weight * Mathf.Lerp(0.01f, 0.025f, travel);
                point.y += travel * weight * 0.08f;
                vertices[i] = point;
            }
            animatedMesh.vertices = vertices;
            animatedMesh.RecalculateNormals();
            animatedMesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            if (filter != null && filter.sharedMesh == animatedMesh) filter.sharedMesh = originalMesh;
            if (animatedMesh != null)
            {
                if (Application.isPlaying) Destroy(animatedMesh); else DestroyImmediate(animatedMesh);
            }
            animatedMesh = null;
            restVertices = vertices = null;
        }
    }
}
