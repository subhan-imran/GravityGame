using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class PrefabAuthoring
    {
        public const string Folder = "Assets/Prefabs/Gameplay";
        public const string UIPath = "Assets/Prefabs/UI/MobileInterface.prefab";
        public const string SettingsPath = "Assets/Settings/DefaultGameUI.asset";
        private static readonly string[] Scenes = { FirstExpedition.ScenePath,
            "Assets/Scenes/MobilePrototype.unity", "Assets/Scenes/TwoPlanets.unity", "Assets/Scenes/PhaseOne.unity" };

        [MenuItem("GravityGame/Authoring/Organize Saved Scenes Into Prefabs")]
        public static void Organize()
        {
            if (!Application.isBatchMode && (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ||
                !EditorUtility.DisplayDialog("Organize saved scenes", "Groups the four saved demo scenes and connects their existing objects to reusable prefabs. Existing transforms and gameplay values are retained.", "Organize", "Cancel"))) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureFolder(Folder); EnsureFolder("Assets/Prefabs/UI");
                var settings = AssetDatabase.LoadAssetAtPath<GameUISettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<GameUISettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
                foreach (var path in Scenes) OrganizeScene(path, settings);
                AssetDatabase.SaveAssets();
                foreach (var path in Scenes)
                {
                    EditorSceneManager.OpenScene(path);
                    Validate(Object.FindAnyObjectByType<LevelAuthoring>());
                }
                Debug.Log("PREFAB_AUTHORING_MIGRATION_PASSED: four scenes, reusable planets/player/pickups/UI, saved layouts and scoped level bindings.");
            }
            finally
            {
                if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void OrganizeScene(string path, GameUISettings settings)
        {
            var scene = EditorSceneManager.OpenScene(path);
            if (Object.FindAnyObjectByType<LevelAuthoring>() != null)
            {
                Validate(Object.FindAnyObjectByType<LevelAuthoring>());
                return; // Re-running never overwrites authored prefab or scene edits.
            }
            var originalTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .ToDictionary(item => item, item => (item.position, item.rotation, item.lossyScale,
                    item.name, PrefabUtility.GetCorrespondingObjectFromOriginalSource(item)));
            var originalRects = originalTransforms.Keys.OfType<RectTransform>().ToDictionary(item => item,
                item => (item.anchorMin, item.anchorMax, item.pivot, item.anchoredPosition, item.sizeDelta));
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var camera = Object.FindAnyObjectByType<PlanetCamera>();
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            var objective = player.stars;
            var sources = player.gravitySources.Length > 0 ? player.gravitySources : new[] { player.planet };
            var level = new GameObject("Level").AddComponent<LevelAuthoring>();
            level.displayName = Path.GetFileNameWithoutExtension(path);
            level.player = player; level.cameraRig = camera; level.interfaceRoot = ui;
            level.objective = objective; level.startingPlanet = player.planet;
            var planets = Child("Planets", level.transform);
            foreach (var planet in sources)
            {
                planet.transform.SetParent(planets, true);
                var landmarks = Child("Landmarks", planet.transform);
                foreach (var item in scene.GetRootGameObjects())
                    if (item.name.StartsWith(planet.name + " landmark ", StringComparison.Ordinal))
                    {
                        item.transform.SetParent(landmarks, true);
                        Connect(item, Folder + "/SurfaceLandmark.prefab");
                    }
                var boundary = planet.gameObject.AddComponent<GravityBoundary>();
                boundary.rings = planet.GetComponentsInChildren<LineRenderer>();
                boundary.Refresh();
                Connect(planet.gameObject, Folder + "/Planet" + planet.name + ".prefab");
            }
            if (objective != null)
            {
                objective.transform.SetParent(level.transform, true);
                foreach (var star in objective.stars)
                {
                    var anchor = star.gameObject.AddComponent<PlanetSurfaceAnchor>();
                    anchor.CapturePlacement();
                    Connect(star.gameObject, Folder + "/CollectibleStar.prefab");
                }
            }
            var rig = Child("Player Rig", level.transform);
            player.transform.SetParent(rig, true); camera.transform.SetParent(rig, true);
            Connect(rig.gameObject, Folder + (ui != null ? "/MobilePlayerRig.prefab" : "/DesktopPlayerRig.prefab"));
            if (ui != null)
            {
                ui.transform.SetParent(level.transform, true);
                ui.settings = settings; ui.authoredLayout = true; ui.fitTutorialToSafeArea = false;
                ui.Tutorial.settings = settings;
                Connect(ui.gameObject, UIPath);
            }
            // Any remaining lighting or environment objects stay visible and editable beneath Level.
            foreach (var root in scene.GetRootGameObjects())
                if (root != level.gameObject) root.transform.SetParent(level.transform, true);
            level.RefreshReferences();
            foreach (var item in level.GetComponentsInChildren<Component>(true))
                if (item != null && PrefabUtility.IsPartOfPrefabInstance(item))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            foreach (var pair in originalTransforms)
            {
                var current = pair.Key;
                // Unity can remap nested prefab internals when connecting their parent.
                // Match those through their persistent source, while scene-owned roots must retain identity.
                if (current == null && pair.Value.Item5 != null)
                    current = level.GetComponentsInChildren<Transform>(true).FirstOrDefault(candidate =>
                        candidate.name == pair.Value.Item4 && PrefabUtility.GetCorrespondingObjectFromOriginalSource(candidate) == pair.Value.Item5);
                Require(current != null, "Existing object retained: " + pair.Value.Item4);
                if (pair.Key is RectTransform originalRect && current is RectTransform rect)
                {
                    // Overlay Canvas world coordinates depend on the current editor screen.
                    // The anchors, offsets and sizes beneath it are the authored layout.
                    if (rect.GetComponent<Canvas>() != null) continue;
                    var layout = originalRects[originalRect];
                    Require(Vector2.Distance(rect.anchorMin, layout.Item1) < 0.002f &&
                        Vector2.Distance(rect.anchorMax, layout.Item2) < 0.002f &&
                        Vector2.Distance(rect.pivot, layout.Item3) < 0.002f &&
                        Vector2.Distance(rect.anchoredPosition, layout.Item4) < 0.002f &&
                        Vector2.Distance(rect.sizeDelta, layout.Item5) < 0.002f,
                        "Existing UI layout retained: " + pair.Value.Item4);
                    continue;
                }
                Require(Vector3.Distance(current.position, pair.Value.Item1) < 0.002f &&
                    Quaternion.Angle(current.rotation, pair.Value.Item2) < 0.02f &&
                    Vector3.Distance(current.lossyScale, pair.Value.Item3) < 0.002f, "Existing world transforms retained: " + pair.Value.Item4);
            }
            Validate(level);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Connect(GameObject instance, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                // Save the actual hierarchy so nested hero/cape prefabs stay connected.
                // Object.Instantiate would flatten those nested prefab relationships.
                var position = instance.transform.position;
                var rotation = instance.transform.rotation;
                var players = instance.GetComponentsInChildren<SphericalPlayer>(true);
                var playerLinks = players.Select(item => (item.planet, item.gravitySources, item.stars)).ToArray();
                var cameras = instance.GetComponentsInChildren<PlanetCamera>(true);
                var cameraPlanets = cameras.Select(item => item.planet).ToArray();
                var stars = instance.GetComponentsInChildren<CollectibleStar>(true);
                var starLinks = stars.Select(item => (item.collection, item.planet)).ToArray();
                var interfaces = instance.GetComponentsInChildren<MobileGameUI>(true);
                var uiLinks = interfaces.Select(item => (item.player, item.cameraRig)).ToArray();
                try
                {
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    foreach (var player in players)
                    { player.planet = null; player.gravitySources = new PlanetGravity[0]; player.stars = null; }
                    foreach (var camera in cameras) camera.planet = null;
                    foreach (var star in stars) { star.collection = null; star.planet = null; }
                    foreach (var ui in interfaces) ui.Bind(null, null);
                    asset = PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                finally
                {
                    instance.transform.SetPositionAndRotation(position, rotation);
                    for (int i = 0; i < players.Length; i++)
                    { players[i].planet = playerLinks[i].planet; players[i].gravitySources = playerLinks[i].gravitySources; players[i].stars = playerLinks[i].stars; }
                    for (int i = 0; i < cameras.Length; i++) cameras[i].planet = cameraPlanets[i];
                    for (int i = 0; i < stars.Length; i++) { stars[i].collection = starLinks[i].collection; stars[i].planet = starLinks[i].planet; }
                    for (int i = 0; i < interfaces.Length; i++) interfaces[i].Bind(uiLinks[i].player, uiLinks[i].cameraRig);
                }
            }
            PrefabUtility.ConvertToPrefabInstance(instance, asset, new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                recordPropertyOverridesOfMatches = true,
                gameObjectsNotMatchedBecomesOverride = true,
                componentsNotMatchedBecomesOverride = true,
                changeRootNameToAssetName = false
            }, InteractionMode.AutomatedAction);
        }

        public static void Validate(LevelAuthoring level)
        {
            Require(level != null && level.player != null && level.cameraRig != null && level.startingPlanet != null, "Level bindings");
            Require(level.player.planet == level.startingPlanet && level.cameraRig.target == level.player.transform, "Player and camera links");
            Require(level.player.gravitySources.Contains(level.startingPlanet), "Starting planet belongs to this level");
            if (level.objective != null)
            {
                Require(level.objective.player == level.player && level.player.stars == level.objective, "Objective links");
                Require(level.objective.stars.Length > 0 && level.objective.stars.Distinct().Count() == level.objective.stars.Length, "Unique pickups");
                foreach (var star in level.objective.stars)
                    Require(star != null && star.collection == level.objective && star.planet != null &&
                        level.player.gravitySources.Contains(star.planet) && star.visual != null, "Star bindings");
            }
            if (level.interfaceRoot != null)
                Require(level.interfaceRoot.HasRequiredReferences() && level.interfaceRoot.authoredLayout &&
                    level.interfaceRoot.player == level.player && level.interfaceRoot.cameraRig == level.cameraRig,
                    "Saved mobile UI and scene bindings");
        }

        [MenuItem("GravityGame/Authoring/Duplicate Current Level")]
        public static void DuplicateLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = EditorUtility.SaveFilePanelInProject("Save new level", "NewExpedition", "unity", "Choose a name for your editable level", "Assets/Scenes");
            if (string.IsNullOrEmpty(path)) return;
            var scene = SceneManager.GetActiveScene();
            if (path == scene.path) { Debug.LogWarning("Choose a different filename for the new level."); return; }
            EditorSceneManager.SaveScene(scene, path, true);
            EditorSceneManager.OpenScene(path);
        }

        public static Transform Child(string name, Transform parent)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            return node;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new Exception("Prefab authoring: " + message); }
    }
}
