using UnityEngine;

namespace GravityGame
{
    /// <summary>The saved hierarchy is the level. No scene generator runs when playing.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class LevelAuthoring : MonoBehaviour
    {
        [Header("Level")]
        public string displayName = "New expedition";
        [Tooltip("Player begins at the saved Player transform on this planet.")]
        public PlanetGravity startingPlanet;
        [Header("Scene references")]
        public SphericalPlayer player;
        public PlanetCamera cameraRig;
        public StarCollection objective;
        public MobileGameUI interfaceRoot;
        [Tooltip("Include active planets and stars beneath this level automatically. Duplicate or delete them in the Hierarchy, then refresh.")]
        public bool discoverChildren = true;

        private void Awake() => RefreshReferences();

        [ContextMenu("Refresh Level References")]
        public void RefreshReferences()
        {
            if (player == null) player = GetComponentInChildren<SphericalPlayer>(true);
            if (cameraRig == null) cameraRig = GetComponentInChildren<PlanetCamera>(true);
            if (objective == null) objective = GetComponentInChildren<StarCollection>(true);
            if (interfaceRoot == null) interfaceRoot = GetComponentInChildren<MobileGameUI>(true);
            if (player == null) return;
            if (discoverChildren)
            {
                player.gravitySources = System.Array.FindAll(GetComponentsInChildren<PlanetGravity>(true),
                    source => source.enabled && source.gameObject.activeInHierarchy);
                if (objective != null)
                    objective.stars = System.Array.FindAll(GetComponentsInChildren<CollectibleStar>(true),
                        star => star.enabled && star.gameObject.activeInHierarchy);
            }
            if (startingPlanet != null) player.planet = startingPlanet;
            if (cameraRig != null)
            {
                cameraRig.target = player.transform;
                cameraRig.planet = player.planet;
                player.movementCamera = cameraRig.transform;
            }
            if (objective != null)
            {
                objective.player = player;
                player.stars = objective;
                foreach (var star in objective.stars)
                    if (star != null) star.collection = objective;
            }
            if (interfaceRoot != null) interfaceRoot.Bind(player, cameraRig);
        }
    }
}
