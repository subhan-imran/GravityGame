using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GravityGame
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class SphericalPlayer : MonoBehaviour
    {
        public PlanetGravity planet;
        public Transform movementCamera;
        public PlanetGravity[] gravitySources = new PlanetGravity[0];
        [Min(0f)] public float speed = 7f;
        [Min(0f)] public float runSpeed = 14f;
        [Min(0f)] public float acceleration = 35f;
        [Min(0f)] public float jumpHeight = 2f;
        [Min(0f)] public float secondJumpHeight = 3.2f;
        [FormerlySerializedAs("runningJumpHeight"), Min(0f)] public float thirdJumpHeight = 8f;
        [Min(0.1f)] public float consecutiveJumpWindow = 1.2f;
        [Range(0f, 1f)] public float airControl = 0.3f;
        [Min(1f)] public float gravityTurnSpeed = 360f;
        [Min(0.1f)] public float switchHysteresis = 3f;
        [Min(0.1f)] public float lostInSpaceDelay = 3f;
        [FormerlySerializedAs("respawnDistance"), Min(1f)] public float lossDistance = 120f;
        public bool showTravelHelp;
        public bool useMobileUI;
        public StarCollection stars;
        public bool IsGrounded { get; private set; }
        public Vector3 GravityUp { get; private set; } = Vector3.up;
        public int RespawnCount { get; private set; }
        public bool HasLost { get; private set; }
        public bool HasWon { get; private set; }
        public bool IsPaused { get; private set; }
        public Vector2 MovementInput => input;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public float SpaceTimeRemaining => Mathf.Max(0f, lostInSpaceDelay - outsideTime);
        public bool IsInGravityField { get; private set; }
        public bool IsRunning => runRequested && input.sqrMagnitude > 0.01f;
        public int JumpChain { get; private set; }
        public int NextJump => JumpChain > 0 && JumpChain < 3 ? JumpChain + 1 : 1;
        public bool EscapeJumpReady => IsGrounded && NextJump == 3;
        public float JumpWindowRemaining => Mathf.Max(0f, consecutiveJumpWindow - comboGroundedTime);

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector2 input;
        private float jumpBuffer;
        private float groundingLock;
        private bool runRequested;
        private Vector2 touchMovement;
        private bool touchRunning;
        private float outsideTime;
        private PlanetGravity checkpointPlanet;
        private Vector3 checkpointLocalPosition;
        private Quaternion checkpointLocalRotation;
        private PlanetGravity startingPlanet;
        private Vector3 startingLocalPosition;
        private Quaternion startingLocalRotation;
        private float comboGroundedTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            GravityUp = planet != null ? planet.UpAt(body.position) : transform.up;
            IsInGravityField = planet != null && planet.Contains(body.position);
            SaveCheckpoint();
            startingPlanet = checkpointPlanet;
            startingLocalPosition = checkpointLocalPosition;
            startingLocalRotation = checkpointLocalRotation;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            input = Vector2.zero;
            runRequested = false;
            if (IsPaused || HasLost || HasWon)
            {
                if (!IsPaused && keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    if (HasWon) RestartRun(); else Respawn();
                }
                return;
            }
            ApplyTouchMovement();
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame)
            {
                Respawn();
                return;
            }
            Vector2 keyboardMove = Vector2.zero;
            keyboardMove.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            keyboardMove.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            if (keyboardMove.sqrMagnitude > 0f)
            {
                input = Vector2.ClampMagnitude(keyboardMove, 1f);
                runRequested = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
            if (keyboard.spaceKey.wasPressedThisFrame) RequestJump();
        }

        public void SetTouchMovement(Vector2 value)
        {
            touchMovement = HasLost || HasWon || IsPaused ? Vector2.zero : Vector2.ClampMagnitude(value, 1f);
            ApplyTouchMovement();
        }

        private void ApplyTouchMovement()
        {
            float magnitude = touchMovement.magnitude;
            touchRunning = magnitude >= (touchRunning ? 0.75f : 0.85f);
            input = touchRunning ? touchMovement.normalized : touchMovement;
            runRequested = touchRunning;
        }

        public void RequestJump()
        {
            if (!HasLost && !HasWon && !IsPaused) jumpBuffer = 0.15f;
        }

        public void ClearInput()
        {
            input = touchMovement = Vector2.zero;
            runRequested = touchRunning = false;
            jumpBuffer = 0f;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            ClearInput();
        }

        private void FixedUpdate()
        {
            if (HasLost || HasWon || IsPaused) return;
            float dt = Time.fixedDeltaTime;
            groundingLock = Mathf.Max(0f, groundingLock - dt);
            PlanetGravity closest = ClosestPlanet(body.position, true);
            if (closest == null)
            {
                outsideTime += dt;
                PlanetGravity nearest = ClosestPlanet(body.position, false);
                if (outsideTime >= lostInSpaceDelay || (nearest != null && nearest.SurfaceDistance(body.position) > lossDistance))
                {
                    LoseInSpace();
                    return;
                }
            }
            else outsideTime = 0f;

            if (closest != null && (!IsGrounded || planet == null || !planet.Contains(body.position)) &&
                (planet == null || !planet.Contains(body.position) || closest.SurfaceDistance(body.position) + switchHysteresis < planet.SurfaceDistance(body.position)))
                SwitchPlanet(closest);

            if (planet == null) return;
            IsInGravityField = planet.Contains(body.position);
            Vector3 radialUp = planet.UpAt(body.position);
            Quaternion current = body.rotation;
            Quaternion desiredRotation = IsInGravityField
                ? Quaternion.FromToRotation(current * Vector3.up, radialUp) * current : current;
            Quaternion aligned = Quaternion.RotateTowards(current, desiredRotation, gravityTurnSpeed * dt);
            GravityUp = aligned * Vector3.up;
            body.MoveRotation(aligned);

            Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, transform.lossyScale);
            float halfHeight = capsule.height * Mathf.Abs(transform.lossyScale.y) * 0.5f;
            Collider surface = planet.GetComponent<Collider>();
            IsGrounded = IsInGravityField && groundingLock <= 0f && surface != null &&
                Vector3.Dot(body.linearVelocity, radialUp) < 1f &&
                surface.Raycast(new Ray(center, -radialUp), out _, halfHeight + 0.15f);
            if (IsGrounded && Vector3.Dot(GravityUp, radialUp) > 0.98f) SaveCheckpoint();
            // Only time spent on the ground consumes the chain window. Pause freezes it.
            if (IsGrounded && JumpChain > 0)
            {
                comboGroundedTime += dt;
                if (JumpChain == 3 || comboGroundedTime > consecutiveJumpWindow) ResetJumpChain();
            }

            Vector3 forward = Vector3.ProjectOnPlane(movementCamera != null
                ? movementCamera.forward : aligned * Vector3.forward, GravityUp).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = aligned * Vector3.forward;
            Vector3 right = Vector3.Cross(GravityUp, forward).normalized;
            float targetSpeed = IsRunning ? runSpeed : speed;
            Vector3 desired = (forward * input.y + right * input.x) * targetSpeed;
            Vector3 tangentVelocity = Vector3.ProjectOnPlane(body.linearVelocity, GravityUp);
            // Only steer in a gravity field. No input or motor braking changes a ballistic space flight.
            if (IsGrounded || (IsInGravityField && input.sqrMagnitude > 0.01f))
            {
                if (!IsGrounded) desired = desired.normalized * Mathf.Max(targetSpeed, tangentVelocity.magnitude);
                Vector3 change = Vector3.ClampMagnitude(desired - tangentVelocity,
                    acceleration * (IsGrounded ? 1f : airControl) * dt);
                body.AddForce(change, ForceMode.VelocityChange);
            }
            body.AddForce(planet.AccelerationAt(body.position), ForceMode.Acceleration);

            if (IsGrounded && jumpBuffer > 0f)
            {
                JumpChain = NextJump;
                comboGroundedTime = 0f;
                float height = JumpChain == 3 ? Mathf.Max(secondJumpHeight, thirdJumpHeight)
                    : JumpChain == 2 ? Mathf.Max(jumpHeight, secondJumpHeight) : jumpHeight;
                float energy = planet.acceleration * height;
                if (JumpChain < 3)
                {
                    // Running around a small sphere adds outward motion. Compensate at takeoff so
                    // the first two jumps keep their intended height even at full running speed.
                    float radius = Vector3.Distance(body.position, planet.transform.position);
                    float tangentSpeedSquared = Vector3.ProjectOnPlane(body.linearVelocity, radialUp).sqrMagnitude;
                    float ratio = radius / (radius + height);
                    energy += 0.5f * tangentSpeedSquared * (ratio * ratio - 1f);
                }
                float jumpSpeed = Mathf.Sqrt(2f * Mathf.Max(0.1f, energy));
                body.AddForce(radialUp * (jumpSpeed - Vector3.Dot(body.linearVelocity, radialUp)), ForceMode.VelocityChange);
                jumpBuffer = 0f;
                groundingLock = 0.2f;
                IsGrounded = false;
            }
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
        }

        private PlanetGravity ClosestPlanet(Vector3 position, bool inRangeOnly)
        {
            PlanetGravity best = null;
            float bestDistance = float.PositiveInfinity;
            Consider(planet);
            foreach (var source in gravitySources) Consider(source);
            return best;

            void Consider(PlanetGravity source)
            {
                if (source == null || !source.isActiveAndEnabled || (inRangeOnly && !source.Contains(position))) return;
                float distance = source.SurfaceDistance(position);
                if (distance < bestDistance) { best = source; bestDistance = distance; }
            }
        }

        private void SwitchPlanet(PlanetGravity destination)
        {
            if (destination == null || destination == planet) return;
            ResetJumpChain();
            planet = destination;
            IsGrounded = false;
            groundingLock = 0.1f;
            jumpBuffer = 0f;
        }

        private void SaveCheckpoint()
        {
            if (planet == null) return;
            checkpointPlanet = planet;
            checkpointLocalPosition = planet.transform.InverseTransformPoint(body.position + planet.UpAt(body.position) * 0.1f);
            checkpointLocalRotation = Quaternion.Inverse(planet.transform.rotation) * body.rotation;
        }

        private void LoseInSpace()
        {
            if (HasLost) return;
            HasLost = true;
            ResetJumpChain();
            ClearInput();
            IsGrounded = IsInGravityField = false;
            input = Vector2.zero;
            runRequested = false;
            jumpBuffer = 0f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Respawn()
        {
            if (body == null || checkpointPlanet == null) return;
            if (HasWon) { RestartRun(); return; }
            HasLost = false;
            ResetJumpChain();
            ClearInput();
            body.isKinematic = false;
            planet = checkpointPlanet;
            body.position = planet.transform.TransformPoint(checkpointLocalPosition);
            body.rotation = planet.transform.rotation * checkpointLocalRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            GravityUp = planet.UpAt(body.position);
            body.rotation = Quaternion.FromToRotation(body.rotation * Vector3.up, GravityUp) * body.rotation;
            // R can run between physics steps; make the new pose visible to the camera immediately.
            transform.SetPositionAndRotation(body.position, body.rotation);
            Physics.SyncTransforms();
            input = Vector2.zero;
            runRequested = false;
            jumpBuffer = outsideTime = 0f;
            groundingLock = 0.15f;
            IsGrounded = false;
            IsInGravityField = planet.Contains(body.position);
            RespawnCount++;
            Cursor.lockState = useMobileUI || Application.isMobilePlatform ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void RestartRun()
        {
            if (startingPlanet == null) return;
            HasWon = false;
            if (stars != null) stars.ResetProgress();
            checkpointPlanet = startingPlanet;
            checkpointLocalPosition = startingLocalPosition;
            checkpointLocalRotation = startingLocalRotation;
            Respawn();
        }

        private void ResetJumpChain()
        {
            JumpChain = 0;
            comboGroundedTime = 0f;
        }

        internal void CompleteStarRun()
        {
            if (HasWon || HasLost || IsPaused || stars == null || !stars.AllCollected) return;
            HasWon = true;
            ClearInput();
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (useMobileUI) return;
            if (HasLost)
            {
                var panel = new Rect((Screen.width - 440f) * 0.5f, (Screen.height - 180f) * 0.5f, 440f, 180f);
                GUI.Box(panel, "");
                var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 42f), "Lost in space", titleStyle);
                var messageStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(panel.x, panel.y + 62f, panel.width, 32f), "You didn't reach another planet. Try a different jump.", messageStyle);
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 112f, 300f, 42f), "Retry from last landing (R)")) Respawn();
                return;
            }
            if (!showTravelHelp) return;
            string name = IsInGravityField && planet != null ? planet.name + " gravity" : "Space - " + SpaceTimeRemaining.ToString("0.0") + "s to reach gravity";
            string jumpStatus = IsGrounded ? (EscapeJumpReady ? "Third jump ready - press Space" : "Jump " + NextJump + "/3 - jump again soon after landing") : "Airborne - carry your momentum";
            GUI.Box(new Rect(16, 16, 600, 132), "TWO PLANET DEMO  |  " + name +
                "\nWASD / arrows: move   Hold Shift: run   Space: jump   Mouse: orbit" +
                "\n" + jumpStatus +
                "\nChain three jumps; the third can clear the gravity rings. Shift still runs." +
                "\nCarry your momentum through space; entering the next field pulls you in.   R: respawn" +
                "\nEscape: release mouse   Click: capture   Checkpoint: last landing");
        }
    }
}
