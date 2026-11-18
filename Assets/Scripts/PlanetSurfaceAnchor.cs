using UnityEngine;

namespace GravityGame
{
    // Keeps pickups at a world-space height when a designer moves or resizes a planet.
    [ExecuteAlways, RequireComponent(typeof(CollectibleStar))]
    public sealed class PlanetSurfaceAnchor : MonoBehaviour
    {
        [Tooltip("Direction relative to the planet. You can also drag the star in the Scene view.")]
        public Vector3 surfaceDirection;
        public float heightAboveSurface = 1.35f;
        public bool followPlanet = true;
        private PlanetGravity previousPlanet;
        private Vector3 previousPosition, previousPlanetPosition;
        private Quaternion previousPlanetRotation;
        private float previousRadius;

        private void OnEnable() => CapturePlacement();
        private void OnValidate()
        {
            if (surfaceDirection.sqrMagnitude > 0.001f) ApplyPlacement();
        }
        private void Update()
        {
            if (!followPlanet) return;
            var planet = GetComponent<CollectibleStar>().planet;
            if (planet == null) return;
            if (planet != previousPlanet) { CapturePlacement(); return; }
            bool changed = planet.transform.position != previousPlanetPosition ||
                planet.transform.rotation != previousPlanetRotation || planet.Radius != previousRadius;
            if (changed) ApplyPlacement();
            else if (transform.position != previousPosition) CapturePlacement();
        }

        [ContextMenu("Capture Placement From Scene")]
        public void CapturePlacement()
        {
            var planet = GetComponent<CollectibleStar>().planet;
            if (planet == null) return;
            surfaceDirection = planet.transform.InverseTransformDirection(planet.UpAt(transform.position));
            heightAboveSurface = planet.SurfaceDistance(transform.position);
            Remember(planet);
        }

        [ContextMenu("Apply Surface Placement")]
        public void ApplyPlacement()
        {
            var planet = GetComponent<CollectibleStar>().planet;
            if (planet == null || surfaceDirection.sqrMagnitude < 0.001f) return;
            Vector3 up = planet.transform.TransformDirection(surfaceDirection.normalized);
            transform.SetPositionAndRotation(planet.transform.position + up * (planet.Radius + heightAboveSurface),
                Quaternion.FromToRotation(Vector3.up, up));
            Remember(planet);
        }

        private void Remember(PlanetGravity planet)
        {
            previousPlanet = planet; previousPosition = transform.position;
            previousPlanetPosition = planet.transform.position; previousPlanetRotation = planet.transform.rotation;
            previousRadius = planet.Radius;
        }
    }
}
