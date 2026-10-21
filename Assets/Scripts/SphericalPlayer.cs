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

        public event System.Action<int> Jumped;
        public event System.Action<int> Landed;
        public bool LowerJumpConstrained => lowerJumpPlanet != null;

        private PlanetGravity lowerJumpPlanet;
        private float lowerJumpRadius;
        private bool landingPending;
        private int lastTakeoffStage;
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
            RestoreLowerJumpBounds();
            groundingLock = Mathf.Max(0f, groundingLock - dt);
            PlanetGravity closest = lowerJumpPlanet != null ? lowerJumpPlanet : ClosestPlanet(body.position, true);
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
            if (IsGrounded)
            {
                lowerJumpPlanet = null;
                if (landingPending)
                {
                    landingPending = false;
                    Landed?.Invoke(lastTakeoffStage);
                }
            }
            // Only time spent on the ground consumes the chain window. Pause freezes it.
            if (IsGrounded && JumpChain > 0)
            {
                comboGroundedTime += dt;
                if (JumpChain == 3 || comboGroundedTime > consecutiveJumpWindow) ResetJumpChain();
            }

            Vector3 pendingVelocityChange = Vector3.zero;
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
                pendingVelocityChange += change;
            }
            pendingVelocityChange += planet.AccelerationAt(body.position) * dt;

            if (IsGrounded && jumpBuffer > 0f)
            {
                JumpChain = NextJump;
                comboGroundedTime = 0f;
                float height = JumpChain == 3 ? Mathf.Max(secondJumpHeight, thirdJumpHeight)
                    : JumpChain == 2 ? Mathf.Max(jumpHeight, secondJumpHeight) : jumpHeight;
                float jumpSpeed = Mathf.Sqrt(2f * planet.acceleration * height);
                pendingVelocityChange += radialUp * (jumpSpeed - Vector3.Dot(body.linearVelocity, radialUp));
                lowerJumpPlanet = JumpChain < 3 ? planet : null;
                if (lowerJumpPlanet != null)
                    lowerJumpRadius = Mathf.Min(Vector3.Distance(body.position, planet.transform.position) + height,
                        LowerJumpCeiling(planet));
                landingPending = true;
                lastTakeoffStage = JumpChain;
                jumpBuffer = 0f;
                groundingLock = 0.2f;
                IsGrounded = false;
                Jumped?.Invoke(JumpChain);
            }
            Vector3 nextVelocity = body.linearVelocity + pendingVelocityChange;
            if (lowerJumpPlanet != null)
            {
                // Supply the centripetal part of surface travel during lower jumps. Running
                // cannot turn either arc into a sustained orbit instead of another landing.
                float tangentSpeedSquared = Vector3.ProjectOnPlane(nextVelocity, radialUp).sqrMagnitude;
                nextVelocity -= radialUp * (tangentSpeedSquared / Mathf.Max(0.1f,
                    Vector3.Distance(body.position, lowerJumpPlanet.transform.position))) * dt;
            }
            LimitLowerJumpVelocity(ref nextVelocity, dt);
            body.AddForce(nextVelocity - body.linearVelocity, ForceMode.VelocityChange);
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
        }

        private static float LowerJumpCeiling(PlanetGravity source) => source.Radius +
            Mathf.Max(0.05f, source.influenceHeight - Mathf.Max(0.5f, source.edgeFalloff) - 0.1f);

        private void RestoreLowerJumpBounds()
        {
            if (lowerJumpPlanet == null) return;
            if (!lowerJumpPlanet.isActiveAndEnabled) { lowerJumpPlanet = null; return; }
            lowerJumpRadius = Mathf.Min(lowerJumpRadius,
                LowerJumpCeiling(lowerJumpPlanet));
            Vector3 radial = body.position - lowerJumpPlanet.transform.position;
            // Recover from an external impulse/collision too, before gravity selection or loss checks.
            if (radial.sqrMagnitude > lowerJumpRadius * lowerJumpRadius)
                body.position = lowerJumpPlanet.transform.position + radial.normalized * lowerJumpRadius;
        }

        private void LimitLowerJumpVelocity(ref Vector3 velocity, float dt)
        {
            if (lowerJumpPlanet == null) return;
            Vector3 radial = body.position - lowerJumpPlanet.transform.position;
            Vector3 up = lowerJumpPlanet.UpAt(body.position);
            Vector3 tangent = Vector3.ProjectOnPlane(velocity, up);
            // Solve the next spherical position, including tangential travel and all motor/gravity
            // impulses. Unlike a takeoff-speed approximation, this also works while holding run.
            tangent = Vector3.ClampMagnitude(tangent, lowerJumpRadius * 0.9f / dt);
            float radialLimit = (Mathf.Sqrt(Mathf.Max(0f, lowerJumpRadius * lowerJumpRadius -
                tangent.sqrMagnitude * dt * dt)) - radial.magnitude) / dt;
            velocity = tangent + up * Mathf.Min(Vector3.Dot(velocity, up), radialLimit);
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
            landingPending = false;
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
            landingPending = false;
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
            lowerJumpPlanet = null;
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
