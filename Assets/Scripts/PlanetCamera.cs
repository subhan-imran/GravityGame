using UnityEngine;
using UnityEngine.InputSystem;

namespace GravityGame
{
    public sealed class PlanetCamera : MonoBehaviour
    {
        public Transform target;
        public PlanetGravity planet;
        [Min(0.1f)] public float distance = 7f;
        [Min(0.01f)] public float smoothTime = 0.12f;
        public float sensitivity = 0.12f;
        [Min(1f)] public float rollSpeed = 240f;
        private Vector3 heading;
        private Vector3 previousUp;
        private Vector3 velocity;
        private float pitch = 20f;
        private bool initialized;
        private SphericalPlayer player;
        private int respawnCount;

        private void OnEnable()
        {
            initialized = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable() => Cursor.lockState = CursorLockMode.None;

        private void LateUpdate()
        {
            if (target == null) return;
            if (player == null) player = target.GetComponent<SphericalPlayer>();
            if (player != null)
            {
                planet = player.planet;
                if (respawnCount != player.RespawnCount)
                {
                    initialized = false;
                    respawnCount = player.RespawnCount;
                }
            }
            if (planet == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;

            Vector3 targetUp = player != null ? target.up : planet.UpAt(target.position);
            Vector3 up = initialized ? Vector3.RotateTowards(previousUp, targetUp,
                rollSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized : targetUp;
            if (!initialized)
            {
                previousUp = up;
                heading = Vector3.ProjectOnPlane(target.forward, up).normalized;
                if (heading.sqrMagnitude < 0.001f)
                    heading = Vector3.Cross(up, Mathf.Abs(up.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                velocity = Vector3.zero;
            }
            heading = Quaternion.FromToRotation(previousUp, up) * heading;
            previousUp = up;
            Vector2 look = Mouse.current != null && Cursor.lockState == CursorLockMode.Locked
                ? Mouse.current.delta.ReadValue() : Vector2.zero;
            heading = Quaternion.AngleAxis(look.x * sensitivity, up) * heading;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -70f, 65f);
            Vector3 focus = target.position + up * 0.6f;
            Vector3 direction = Quaternion.AngleAxis(pitch, Vector3.Cross(up, heading)) * -heading;
            Vector3 desired = focus + direction * distance;
            Vector3 position = initialized
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime) : desired;
            // Clamp after smoothing too: following around a horizon must not cut through the planet.
            Vector3 offset = position - focus;
            float nearest = offset.magnitude + 0.3f;
            Vector3 safePosition = position;
            bool blocked = false;
            CheckPlanet(planet);
            if (player != null)
                foreach (var source in player.gravitySources) CheckPlanet(source);
            if (blocked)
            {
                position = safePosition;
                velocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position, up));
            initialized = true;

            void CheckPlanet(PlanetGravity source)
            {
                if (source == null || !source.isActiveAndEnabled) return;
                Collider surface = source.GetComponent<Collider>();
                if (surface != null && surface.Raycast(new Ray(focus, offset.normalized), out RaycastHit hit, nearest))
                {
                    nearest = hit.distance;
                    safePosition = hit.point + hit.normal * 0.3f;
                    blocked = true;
                }
            }
        }
    }
}
