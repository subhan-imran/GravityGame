using UnityEngine;

namespace GravityGame
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class CollectibleStar : MonoBehaviour
    {
        public StarCollection collection;
        public PlanetGravity planet;
        public Transform visual;
        [Tooltip("Unique, stable identifier within a saved world. Leave blank in standalone levels.")]
        public string persistentId;
        public bool Collected { get; private set; }
        private float animationTime;

        private void Awake() => GetComponent<SphereCollider>().isTrigger = true;

        private void Update()
        {
            if (Collected || visual == null || collection == null || collection.player == null ||
                collection.player.IsPaused || collection.player.HasWon || collection.player.HasLost) return;
            animationTime += Time.deltaTime;
            visual.localPosition = Vector3.up * (Mathf.Sin(animationTime * 2.5f) * 0.18f);
            visual.localRotation = Quaternion.Euler(0f, animationTime * 75f, 0f);
        }

        private void OnTriggerEnter(Collider other) => Collect(other);
        // Allows collection after resuming while still overlapping the pickup.
        private void OnTriggerStay(Collider other) => Collect(other);

        private void Collect(Collider other)
        {
            if (collection == null || other.attachedRigidbody == null) return;
            var collector = other.attachedRigidbody.GetComponent<SphericalPlayer>();
            collection.TryCollect(this, collector);
        }

        internal void SetCollected(bool value)
        {
            Collected = value;
            GetComponent<SphereCollider>().enabled = !value;
            if (visual != null) visual.gameObject.SetActive(!value);
            if (!value)
            {
                animationTime = 0f;
                if (visual != null) visual.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
    }
}
