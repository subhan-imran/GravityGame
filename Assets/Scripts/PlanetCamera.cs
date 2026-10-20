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
        public bool assisted;
        [Range(-70f, 65f)] public float defaultPitch = 20f;
        [Min(1f)] public float assistTurnSpeed = 55f;
        private Vector3 heading;
        private Vector3 previousUp;
        private Vector3 velocity;
        private float pitch = 20f;
        private bool initialized;
        private SphericalPlayer player;
        private int respawnCount;
        private Vector2 touchLook;
        private float manualLookGrace;
        private Vector3 framingOffset;

        public void AddTouchLook(Vector2 delta) => touchLook += delta;
        public void ClearTouchLook() => touchLook = Vector2.zero;

        private void OnEnable()
        {
            initialized = false;
            pitch = defaultPitch;
            var controller = target != null ? target.GetComponent<SphericalPlayer>() : null;
            Cursor.lockState = Application.isMobilePlatform || (controller != null && controller.useMobileUI)
                ? CursorLockMode.None : CursorLockMode.Locked;
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
                    pitch = defaultPitch;
                    manualLookGrace = 0f;
                    respawnCount = player.RespawnCount;
                }
            }
            if (planet == null) return;
            if (player != null && player.IsPaused) { touchLook = Vector2.zero; return; }
            bool mobile = player != null && player.useMobileUI;
            bool allowLook = player == null || (!player.HasLost && !player.IsPaused);
            if (!allowLook) Cursor.lockState = CursorLockMode.None;
            if (mobile) Cursor.lockState = CursorLockMode.None;
            if (!mobile && allowLook && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;
            if (!mobile && allowLook && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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
                framingOffset = Vector3.zero;
            }
            heading = Quaternion.FromToRotation(previousUp, up) * heading;
            previousUp = up;
            Vector2 look = allowLook ? touchLook : Vector2.zero;
            touchLook = Vector2.zero;
            if (!mobile && allowLook && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
                look += Mouse.current.delta.ReadValue();
            manualLookGrace = look.sqrMagnitude > 0.01f ? 1.4f : Mathf.Max(0f, manualLookGrace - Time.deltaTime);
            heading = Quaternion.AngleAxis(look.x * sensitivity, up) * heading;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -70f, 65f);
            Vector3 desiredFraming = Vector3.zero;
            if (assisted && allowLook && player != null && manualLookGrace <= 0f)
            {
                Vector3 follow = Vector3.zero;
                if (player.IsGrounded && player.MovementInput.y > 0.4f && Mathf.Abs(player.MovementInput.x) < 0.25f)
                    follow = Vector3.ProjectOnPlane(player.Velocity, up);
                if (!player.IsGrounded)
                {
                    PlanetGravity destination = player.IsInGravityField ? player.planet : null;
                    float nearestDestination = float.PositiveInfinity;
                    if (destination == null)
                        foreach (var source in player.gravitySources)
                        {
                            if (source == null || source == planet || !source.isActiveAndEnabled) continue;
                            Vector3 to = source.transform.position - target.position;
                            if (Vector3.Dot(to.normalized, player.Velocity.normalized) < 0.15f) continue;
                            float distanceToSurface = source.SurfaceDistance(target.position);
                            if (distanceToSurface < nearestDestination) { nearestDestination = distanceToSurface; destination = source; }
                        }
                    if (destination != null)
                    {
                        Vector3 to = destination.transform.position - target.position;
                        if (Vector3.Dot(to.normalized, player.Velocity.normalized) > 0.15f)
                        {
                            follow = Vector3.ProjectOnPlane(to, up);
                            desiredFraming = Vector3.ClampMagnitude(to * 0.12f, 3f);
                        }
                    }
                }
                if (follow.sqrMagnitude > 0.1f)
                    heading = Vector3.RotateTowards(heading, follow.normalized,
                        assistTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
            }
            framingOffset = Vector3.Lerp(framingOffset, desiredFraming, 1f - Mathf.Exp(-3f * Time.deltaTime));
            Vector3 focus = target.position + up * 0.6f + framingOffset;
            KeepFocusOutside(planet);
            if (player != null) foreach (var source in player.gravitySources) KeepFocusOutside(source);
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

            void KeepFocusOutside(PlanetGravity source)
            {
                if (source == null || !source.isActiveAndEnabled) return;
                Vector3 radial = focus - source.transform.position;
                if (radial.magnitude < source.Radius + 0.25f)
                    focus = source.transform.position + (radial.sqrMagnitude > 0.001f ? radial.normalized : up) * (source.Radius + 0.25f);
            }

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
