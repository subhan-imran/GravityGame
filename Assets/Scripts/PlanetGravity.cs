using UnityEngine;

namespace GravityGame
{
    public sealed class PlanetGravity : MonoBehaviour
    {
        [Min(0f)] public float acceleration = 25f;
        [Min(1f)] public float influenceHeight = 30f;

        public float Radius
        {
            get
            {
                var sphere = GetComponent<SphereCollider>();
                Vector3 scale = transform.lossyScale;
                return sphere != null ? sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) : 0f;
            }
        }

        public float SurfaceDistance(Vector3 position) => Vector3.Distance(position, transform.position) - Radius;
        public bool Contains(Vector3 position) => isActiveAndEnabled && SurfaceDistance(position) <= influenceHeight;

        public Vector3 UpAt(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return offset.sqrMagnitude > 0.000001f ? offset.normalized : transform.up;
        }

        public Vector3 AccelerationAt(Vector3 position) => -UpAt(position) * acceleration;
    }
}
