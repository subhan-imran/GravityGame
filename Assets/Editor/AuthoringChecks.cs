using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class AuthoringChecks
    {
        public static void RunPhysics()
        {
            EditorSceneManager.OpenScene(FirstExpedition.ScenePath);
            PrefabAuthoring.Validate(Object.FindAnyObjectByType<LevelAuthoring>());
            MobileDemo.ValidateCurrentScene();
            Debug.Log("AUTHORED_PHYSICS_CHECKS_PASSED: saved prefab level, UI states, jump chain, oxygen, six routes, pickups, checkpoint and replay.");
        }

        public static void Run()
        {
            foreach (string path in new[] { FirstExpedition.ScenePath, "Assets/Scenes/MobilePrototype.unity",
                "Assets/Scenes/TwoPlanets.unity", "Assets/Scenes/PhaseOne.unity" })
            {
                EditorSceneManager.OpenScene(path);
                var level = Object.FindAnyObjectByType<LevelAuthoring>();
                PrefabAuthoring.Validate(level);
                Require(PrefabUtility.IsPartOfPrefabInstance(level.player), "Player rig prefab");
                Require(PrefabUtility.IsPartOfPrefabInstance(level.cameraRig), "Camera in player rig");
                Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(level.player.transform.Find("Hero Visual").gameObject) == SuperheroAssets.HeroPath,
                    "Shared nested superhero prefab stays connected");
                foreach (var planet in level.player.gravitySources)
                    Require(PrefabUtility.IsPartOfPrefabInstance(planet), "Planet prefab");
                if (level.interfaceRoot == null) continue;
                CheckEdits(level);
                // Never persist the validation's temporary edits into the authored scenes.
                EditorSceneManager.OpenScene(path);
            }
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new Exception("Missing prefab script: " + path);
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                            Require(EditorUtility.IsPersistent(property.objectReferenceValue), "No scene references in prefab " + path);
                }
            }
            Debug.Log("AUTHORING_CHECKS_PASSED: saved prefab bindings, edit preservation, custom HUD, duplicated/removed stars and planets, radius/ring editing and prefab reference isolation.");
        }

        private static void CheckEdits(LevelAuthoring level)
        {
            int planets = level.player.gravitySources.Length, stars = level.objective.TotalCount;
            var extraPlanet = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAuthoring.Folder + "/PlanetAzure.prefab"), level.transform);
            var extraStar = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAuthoring.Folder + "/CollectibleStar.prefab"), level.objective.transform);
            extraStar.GetComponent<CollectibleStar>().planet = extraPlanet.GetComponent<PlanetGravity>();
            level.RefreshReferences();
            Require(level.player.gravitySources.Length == planets + 1 && level.objective.TotalCount == stars + 1, "Hierarchy additions registered");
            Require(extraStar.GetComponent<CollectibleStar>().collection == level.objective, "New pickup collection bound");
            Object.DestroyImmediate(extraStar); Object.DestroyImmediate(extraPlanet);
            level.RefreshReferences();
            Require(level.player.gravitySources.Length == planets && level.objective.TotalCount == stars, "Deleted entries removed");
            var boundary = level.startingPlanet.GetComponent<GravityBoundary>();
            var scale = level.startingPlanet.transform.localScale;
            level.startingPlanet.transform.localScale *= 1.1f;
            boundary.Refresh();
            float expected = level.startingPlanet.Radius + level.startingPlanet.influenceHeight;
            Require(Mathf.Abs(boundary.rings[0].transform.TransformPoint(boundary.rings[0].GetPosition(0)).magnitude - expected) < 0.02f, "Ring tracks radius");
            level.startingPlanet.transform.localScale = scale; boundary.Refresh();
            var anchoredStar = level.objective.stars[0];
            var anchor = anchoredStar.GetComponent<PlanetSurfaceAnchor>();
            Require(anchor != null, "Pickup surface anchor");
            Vector3 planetPosition = anchoredStar.planet.transform.position;
            Vector3 starPosition = anchoredStar.transform.position;
            anchoredStar.planet.transform.position += Vector3.right * 4f;
            anchor.ApplyPlacement();
            Require(Vector3.Distance(anchoredStar.transform.position, starPosition + Vector3.right * 4f) < 0.002f,
                "Pickup follows edited planet without changing surface height");
            anchoredStar.planet.transform.position = planetPosition; anchor.ApplyPlacement();

            var ui = level.interfaceRoot;
            var heading = ui.transform.Find("Mobile Canvas/Safe Area/Victory overlay/Dialog/Heading").GetComponent<Text>();
            heading.text = "Custom destination reached";
            var jump = (RectTransform)ui.JumpControl.transform;
            jump.anchoredPosition += new Vector2(-31f, 17f);
            Vector2 authoredPosition = jump.anchoredPosition;
            var style = Object.Instantiate(ui.settings);
            style.starCounter = "CHART {count} OF {total}";
            ui.settings = style;
            level.cameraRig.assisted = false;
            var binding = typeof(SphericalPlayer).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            binding.Invoke(level.player, null);
            ui.Initialize(); ui.BuildUI(); ui.Initialize(); ui.Refresh();
            Require(heading.text == "Custom destination reached" && jump.anchoredPosition == authoredPosition, "Authored text and layout survive initialization and creation helper");
            Require(!level.cameraRig.assisted && ui.StarCounterText == "CHART 0 OF " + stars, "Camera and dynamic UI settings honored");
            string temporary = "Assets/AuthoringCheck-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateAsset(style, temporary + ".asset");
            try
            {
                foreach (var component in level.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.SaveScene(level.gameObject.scene, temporary + ".unity", true);
                EditorSceneManager.OpenScene(temporary + ".unity");
                var reloaded = Object.FindAnyObjectByType<LevelAuthoring>();
                Require(reloaded.interfaceRoot.settings.starCounter == "CHART {count} OF {total}" &&
                    ((RectTransform)reloaded.interfaceRoot.JumpControl.transform).anchoredPosition == authoredPosition &&
                    reloaded.interfaceRoot.transform.Find("Mobile Canvas/Safe Area/Victory overlay/Dialog/Heading").GetComponent<Text>().text == "Custom destination reached",
                    "Authored text, layout and settings survive a saved scene reload");
            }
            finally
            {
                EditorSceneManager.OpenScene(FirstExpedition.ScenePath);
                AssetDatabase.DeleteAsset(temporary + ".unity");
                AssetDatabase.DeleteAsset(temporary + ".asset");
            }
        }

        private static void Require(bool value, string message)
        { if (!value) throw new Exception("Authoring checks: " + message); }
    }
}
