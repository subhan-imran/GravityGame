using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    // A short focused check, deliberately separate from the full mobile/gameplay suite.
    public static class HeroAnimationChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Run()
        {
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var body = player.GetComponent<Rigidbody>();
            var motion = player.GetComponentInChildren<HeroLocomotion>();
            var cape = player.GetComponentInChildren<CapeFlutter>();
            var filter = cape.GetComponent<MeshFilter>();
            var shared = filter.sharedMesh;
            var originalVertices = shared.vertices;
            var originalMode = Physics.simulationMode;
            var rootScale = player.transform.localScale;
            float dt = 0.02f;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Invoke(player, "Awake");
                Invoke(motion, "OnEnable");
                Invoke(cape, "OnEnable");
                Require(filter.sharedMesh != shared, "Cape owns a private dynamic mesh");
                Step(40);
                Require(player.IsGrounded, "Player starts grounded");

                player.SetTouchMovement(new Vector2(0f, 0.7f));
                float walk = MaxStride(75);
                Require(walk > 8f && !player.IsRunning, "Partial joystick walks and swings limbs");
                player.SetTouchMovement(Vector2.up);
                float run = MaxStride(75);
                Require(player.IsRunning && run > walk + 5f, "Full joystick produces a larger running stride");
                Require(Mathf.Abs(Angle(motion.leftArm) + Angle(motion.rightArm)) < 0.01f &&
                    Mathf.Abs(Angle(motion.leftLeg) + Angle(motion.rightLeg)) < 0.01f,
                    "Opposing arms/legs stay in phase");
                Require(Vector3.Distance(filter.sharedMesh.vertices[originalVertices.Length - 1],
                    originalVertices[originalVertices.Length - 1]) > 0.01f, "Cape ripples while running");
                Require((filter.sharedMesh.vertices[0] - originalVertices[0]).sqrMagnitude < 0.000001f,
                    "Cape stays attached at shoulder seam");

                Quaternion frozen = motion.leftLeg.localRotation;
                var frozenVertices = filter.sharedMesh.vertices;
                player.SetPaused(true);
                Step(10);
                Require(Quaternion.Angle(frozen, motion.leftLeg.localRotation) < 0.001f &&
                    SameVertices(frozenVertices, filter.sharedMesh.vertices), "Pause freezes limbs and cape");
                player.SetPaused(false);

                player.ClearInput();
                body.linearVelocity = Vector3.zero;
                for (int i = 0; i < 50; i++) VisualStep();
                Require(Mathf.Abs(Angle(motion.leftLeg)) < 0.1f, "Stationary hero settles instead of running in place");

                // Face actual sideways travel without rotating the physics/camera root.
                Quaternion rootRotation = player.transform.rotation;
                body.linearVelocity = player.transform.right * 6f;
                for (int i = 0; i < 50; i++) VisualStep();
                Require(Vector3.Dot(Vector3.ProjectOnPlane(motion.transform.forward, player.GravityUp).normalized,
                    player.transform.right) > 0.98f && Quaternion.Angle(rootRotation, player.transform.rotation) < 0.001f,
                    "Hero faces travel independently of physics root");

                body.linearVelocity = Vector3.zero;
                player.RequestJump();
                Step(25);
                Require(!player.IsGrounded && Mathf.Abs(Angle(motion.leftLeg)) < 0.2f,
                    "Walking settles in the air instead of pedalling through space");
                player.Respawn();
                VisualStep();
                Require(Mathf.Abs(Angle(motion.leftLeg)) < 0.01f && motion.transform.localPosition.sqrMagnitude < 0.000001f,
                    "Retry restores a neutral visual pose");
                Require(player.transform.localScale == rootScale && SameVertices(originalVertices, shared.vertices),
                    "Physics root and shared cape mesh remain unchanged");
                player.GetComponentInChildren<OxygenCape>().RefreshFromPlayer();
                Require(player.GetComponentInChildren<OxygenCape>().DisplayedColor.b > 0.9f,
                    "Oxygen color still coexists with cape animation");
                Debug.Log("HERO_MOTION_CHECK_PASSED: walk stride=" + walk + " run stride=" + run +
                    "; real joystick/physics, steering, air, pause, retry and cape mesh isolation.");
            }
            finally
            {
                Invoke(motion, "OnDisable");
                Invoke(cape, "OnDisable");
                Physics.simulationMode = originalMode;
            }
            Require(filter.sharedMesh == shared, "Disabling cape restores its authored mesh");

            void VisualStep()
            {
                Invoke(motion, "Animate", dt);
                Invoke(cape, "Animate", dt);
            }
            void Step(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Invoke(player, "FixedUpdate");
                    if (!player.IsPaused) Physics.Simulate(dt);
                    VisualStep();
                }
            }
            float MaxStride(int count)
            {
                float maximum = 0f;
                for (int i = 0; i < count; i++) { Step(1); maximum = Mathf.Max(maximum, Mathf.Abs(Angle(motion.leftLeg))); }
                return maximum;
            }
        }

        private static float Angle(Transform joint) => Mathf.DeltaAngle(0f, joint.localEulerAngles.x);
        private static bool SameVertices(Vector3[] a, Vector3[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if ((a[i] - b[i]).sqrMagnitude > 0.00000001f) return false;
            return true;
        }
        private static void Invoke(object instance, string name, params object[] args) =>
            instance.GetType().GetMethod(name, Private).Invoke(instance, args);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Hero motion check: " + message);
        }
    }
}
