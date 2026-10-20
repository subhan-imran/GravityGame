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
        private Vector3 heading;
        private Vector3 previousUp;
        private Vector3 velocity;
        private float pitch = 20f;
        private bool initialized;

        private void OnEnable()
        {
            initialized = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable() => Cursor.lockState = CursorLockMode.None;

        private void LateUpdate()
        {
            if (target == null || planet == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;

            Vector3 up = planet.UpAt(target.position);
            if (!initialized)
            {
                previousUp = up;
                heading = Vector3.ProjectOnPlane(target.forward, up).normalized;
                velocity = Vector3.zero;
            }
            heading = Quaternion.FromToRotation(previousUp, up) * heading;
            previousUp = up;
            Vector2 look = Mouse.current != null && Cursor.lockState == CursorLockMode.Locked
                ? Mouse.current.delta.ReadValue() : Vector2.zero;
            heading = Quaternion.AngleAxis(look.x * sensitivity, up) * heading;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -15f, 65f);
            Vector3 focus = target.position + up * 0.6f;
            Vector3 direction = Quaternion.AngleAxis(pitch, Vector3.Cross(up, heading)) * -heading;
            Vector3 desired = focus + direction * distance;
            Vector3 position = initialized
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime) : desired;
            // Clamp after smoothing too: following around a horizon must not cut through the planet.
            Collider surface = planet.GetComponent<Collider>();
            Vector3 offset = position - focus;
            if (surface != null && surface.Raycast(new Ray(focus, offset.normalized), out RaycastHit hit, offset.magnitude + 0.3f))
            {
                position = hit.point + hit.normal * 0.3f;
                velocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position, up));
            initialized = true;
        }
    }
}
