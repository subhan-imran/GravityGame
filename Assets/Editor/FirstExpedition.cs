using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class FirstExpedition
    {
        public const string ScenePath = "Assets/Scenes/FirstExpedition.unity";

        [MenuItem("GravityGame/Create First Expedition Level")]
        public static void CreateLevel()
        {
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (System.IO.File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Recreate First Expedition?",
                    "This replaces the authored First Expedition scene. Save a copy first if you have edited it.", "Recreate", "Cancel")) return;
            }
            var template = EditorSceneManager.OpenScene("Assets/Scenes/MobilePrototype.unity");
            // Save a copy before editing: the tested two-planet mobile scene stays intact.
            if (!EditorSceneManager.SaveScene(template, ScenePath, true)) throw new Exception("Could not copy the mobile scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            var player = ui.player;
            var home = player.gravitySources[0];
            var ember = player.gravitySources[1];
            var verdant = TwoPlanetDemo.CreatePlanet("Verdant", new Vector3(32f, 24f, 20f), 9f,
                new Color(0.12f, 0.62f, 0.38f));
            player.gravitySources = new[] { home, ember, verdant };
            var stars = new List<CollectibleStar>(player.stars.stars)
            {
                StarDemo.Create(player.stars, ember, new Vector3(-0.55f, 0.65f, 0.52f), "Ember - far ridge"),
                StarDemo.Create(player.stars, verdant, new Vector3(-0.75f, 0.5f, 0.4f), "Verdant - overlook"),
                StarDemo.Create(player.stars, verdant, new Vector3(0.5f, -0.65f, -0.57f), "Verdant - far side")
            };
            player.stars.stars = stars.ToArray();
            ui.BuildUI();
            ValidateLayout(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_EXPEDITION_CREATED: three planets, six stars, free exploration; mobile template preserved.");
        }

        public static void ValidateLayout(SphericalPlayer player)
        {
            Require(player.gravitySources.Length == 3 && player.stars.TotalCount == 6, "Three planets and six stars");
            var unique = new HashSet<CollectibleStar>();
            foreach (var source in player.gravitySources)
            {
                int count = 0;
                foreach (var star in player.stars.stars)
                {
                    Require(star != null && star.collection == player.stars, "Valid objective references");
                    if (star.planet != source) continue;
                    count++;
                    Require(unique.Add(star), "No duplicate pickups");
                    Require(Mathf.Abs(source.SurfaceDistance(star.transform.position) - 1.35f) < 0.01f,
                        "Every star is reachable from the surface of " + source.name);
                }
                Require(count == 2, "Two stars on " + source.name);
                foreach (var other in player.gravitySources)
                {
                    if (source == other) continue;
                    float gap = Vector3.Distance(source.transform.position, other.transform.position) -
                        source.Radius - source.influenceHeight - other.Radius - other.influenceHeight;
                    Require(gap > 1f && gap < 5f, "A short gap of space separates each route");
                }
            }
            Require(unique.Count == 6, "All stars belong to registered planets");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("First Expedition layout: " + message);
        }
    }
}
