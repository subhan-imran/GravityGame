using UnityEngine;

namespace GravityGame
{
    [DisallowMultipleComponent]
    public sealed class PhoneBooth : MonoBehaviour
    {
        [Tooltip("A unique ID in this world. Keep it stable when renaming or moving a booth.")]
        public string persistentId;
        public string stationName = "Phone booth";
        public PlanetGravity planet;
        [Tooltip("Ground-level marker outside the booth. The save reloads here.")]
        public Transform arrival;
        [Min(1f)] public float interactionRadius = 4.5f;

        public bool IsAvailableTo(SphericalPlayer player) =>
            isActiveAndEnabled && planet != null && player.planet == planet && player.IsGrounded &&
            !player.HasLost && !player.HasWon &&
            (player.transform.position - transform.position).sqrMagnitude <= interactionRadius * interactionRadius;

        public void GetArrivalPose(out Vector3 position, out Quaternion rotation)
        {
            Vector3 up = planet.UpAt(arrival.position);
            position = planet.transform.position + up * (planet.Radius + 1.05f);
            Vector3 forward = Vector3.ProjectOnPlane(arrival.forward, up).normalized;
            rotation = forward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(forward, up) :
                Quaternion.FromToRotation(Vector3.up, up);
        }
    }
}
