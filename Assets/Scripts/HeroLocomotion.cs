using UnityEngine;

namespace GravityGame
{
    // Animates only the art below the physics root. No Animator or extra rigidbodies needed.
    public sealed class HeroLocomotion : MonoBehaviour
    {
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        private SphericalPlayer player;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Quaternion leftArmRest, rightArmRest, leftLegRest, rightLegRest;
        private float phase, blend, runBlend, heading;
        private int respawnCount;
        private bool initialized;

        private void OnEnable()
        {
            if (!initialized)
            {
                restPosition = transform.localPosition;
                restRotation = transform.localRotation;
                leftArmRest = Rest(leftArm);
                rightArmRest = Rest(rightArm);
                leftLegRest = Rest(leftLeg);
                rightLegRest = Rest(rightLeg);
                initialized = true;
            }
            player = GetComponentInParent<SphericalPlayer>();
            ResetPose();
            respawnCount = player != null ? player.RespawnCount : 0;
        }

        private void LateUpdate() => Animate(Time.deltaTime);

        private void Animate(float dt)
        {
            if (player == null || player.IsPaused || dt <= 0f) return;
            if (respawnCount != player.RespawnCount)
            {
                ResetPose();
                respawnCount = player.RespawnCount;
            }
            bool finished = player.HasLost || player.HasWon;
            Vector3 tangent = Vector3.ProjectOnPlane(player.Velocity, player.GravityUp);
            float speed = tangent.magnitude;
            float walkingSpeed = Mathf.Max(0.1f, player.speed);
            float runningSpeed = Mathf.Max(walkingSpeed + 0.1f, player.runSpeed);
            float target = player.IsGrounded && !finished ? Mathf.Clamp01(speed / walkingSpeed) : 0f;
            float response = 1f - Mathf.Exp(-12f * dt);
            blend = Mathf.Lerp(blend, target, response);
            runBlend = Mathf.Lerp(runBlend, Mathf.InverseLerp(walkingSpeed, runningSpeed, speed), response);

            // Actual distance/speed drives the gait, so a blocked joystick does not run in place.
            phase = Mathf.Repeat(phase + dt * Mathf.Lerp(2.1f, 3.1f, runBlend) * Mathf.PI * 2f *
                Mathf.Clamp01(speed / walkingSpeed), Mathf.PI * 2f);
            if (!finished && speed > 0.15f && transform.parent != null)
            {
                Vector3 localTravel = transform.parent.InverseTransformDirection(tangent);
                float targetHeading = Mathf.Atan2(localTravel.x, localTravel.z) * Mathf.Rad2Deg;
                heading = Mathf.LerpAngle(heading, targetHeading, 1f - Mathf.Exp(-14f * dt));
            }

            float stride = Mathf.Sin(phase) * blend;
            float legs = Mathf.Lerp(23f, 39f, runBlend);
            float arms = Mathf.Lerp(19f, 34f, runBlend);
            Pose(leftLeg, leftLegRest, stride * legs);
            Pose(rightLeg, rightLegRest, -stride * legs);
            Pose(leftArm, leftArmRest, -stride * arms);
            Pose(rightArm, rightArmRest, stride * arms);
            float bob = (1f - Mathf.Cos(phase * 2f)) * 0.5f * Mathf.Lerp(0.022f, 0.045f, runBlend) * blend;
            transform.localPosition = restPosition + Vector3.up * bob;
            transform.localRotation = restRotation * Quaternion.Euler(0f, heading, 0f) *
                Quaternion.Euler(runBlend * blend * 7f, 0f, stride * 1.5f);
        }

        private void OnDisable() { if (initialized) ResetPose(); }

        private void ResetPose()
        {
            phase = blend = runBlend = heading = 0f;
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
            Pose(leftArm, leftArmRest, 0f);
            Pose(rightArm, rightArmRest, 0f);
            Pose(leftLeg, leftLegRest, 0f);
            Pose(rightLeg, rightLegRest, 0f);
        }

        private static Quaternion Rest(Transform joint) => joint != null ? joint.localRotation : Quaternion.identity;
        private static void Pose(Transform joint, Quaternion rest, float angle)
        {
            if (joint != null) joint.localRotation = rest * Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
