using UnityEngine;
using UnityEngine.InputSystem;

namespace GravityGame
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class SphericalPlayer : MonoBehaviour
    {
        public PlanetGravity planet;
        public Transform movementCamera;
        [Min(0f)] public float speed = 7f;
        [Min(0f)] public float acceleration = 35f;
        [Min(0f)] public float jumpHeight = 2f;
        [Range(0f, 1f)] public float airControl = 0.3f;
        public bool IsGrounded { get; private set; }

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector2 input;
        private float jumpRequestedUntil = -1f;
        private float groundedAfter;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            input = Vector2.zero;
            if (keyboard == null) return;
            input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            input = Vector2.ClampMagnitude(input, 1f);
            if (keyboard.spaceKey.wasPressedThisFrame) jumpRequestedUntil = Time.time + 0.15f;
        }

        private void FixedUpdate()
        {
            if (planet == null) return;
            Vector3 up = planet.UpAt(body.position);
            Quaternion aligned = Quaternion.FromToRotation(body.rotation * Vector3.up, up) * body.rotation;
            body.MoveRotation(aligned);

            // Probe only the assigned planet so the player's own collider cannot ground it.
            Vector3 center = transform.TransformPoint(capsule.center);
            float halfHeight = capsule.height * Mathf.Abs(transform.lossyScale.y) * 0.5f;
            Collider surface = planet.GetComponent<Collider>();
            IsGrounded = Time.time >= groundedAfter && surface != null &&
                surface.Raycast(new Ray(center, -up), out _, halfHeight + 0.15f);

            Vector3 forward = Vector3.ProjectOnPlane(movementCamera != null
                ? movementCamera.forward : aligned * Vector3.forward, up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = aligned * Vector3.forward;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector3 desired = (forward * input.y + right * input.x) * speed;
            Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, up);
            Vector3 change = Vector3.ClampMagnitude(desired - tangentVelocity,
                acceleration * (IsGrounded ? 1f : airControl) * Time.fixedDeltaTime);
            body.AddForce(change, ForceMode.VelocityChange);
            body.AddForce(planet.AccelerationAt(body.position), ForceMode.Acceleration);

            if (IsGrounded && Time.time <= jumpRequestedUntil)
            {
                float jumpSpeed = Mathf.Sqrt(2f * planet.acceleration * jumpHeight);
                body.AddForce(up * (jumpSpeed - Vector3.Dot(body.linearVelocity, up)), ForceMode.VelocityChange);
                jumpRequestedUntil = -1f;
                groundedAfter = Time.time + 0.2f;
                IsGrounded = false;
            }
        }
    }
}
