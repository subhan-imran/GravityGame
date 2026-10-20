using UnityEngine;

namespace GravityGame
{
    public sealed class PlanetGravity : MonoBehaviour
    {
        [Min(0f)] public float acceleration = 25f;

        public Vector3 UpAt(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return offset.sqrMagnitude > 0.000001f ? offset.normalized : transform.up;
        }

        public Vector3 AccelerationAt(Vector3 position) => -UpAt(position) * acceleration;
    }
}
