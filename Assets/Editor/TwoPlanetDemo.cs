using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class TwoPlanetDemo
    {
        public static void ValidateAll()
        {
            PhaseOneDemo.Validate();
            Validate();
        }

        [MenuItem("GravityGame/Create Two Planet Demo")]
        public static void CreateDemo()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var home = CreatePlanet("Azure", Vector3.zero, 20f, new Color(0.1f, 0.42f, 0.7f));
            var moon = CreatePlanet("Ember", new Vector3(0f, 32f, 34f), 12f, new Color(0.8f, 0.28f, 0.12f));
            var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.position = Vector3.up * 21.1f;
            playerObject.GetComponent<Renderer>().sharedMaterial = Material("Player", new Color(0.95f, 0.85f, 0.25f));
            playerObject.AddComponent<Rigidbody>();
            var player = playerObject.AddComponent<SphericalPlayer>();
            player.planet = home;
            player.gravitySources = new[] { home, moon };
            player.showTravelHelp = true;

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PlanetCamera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<PlanetCamera>();
            camera.target = player.transform;
            camera.planet = home;
            camera.distance = 9f;
            player.movementCamera = camera.transform;
            camera.transform.position = new Vector3(0f, 24f, -9f);
            camera.transform.LookAt(player.transform);
            var view = cameraObject.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.015f, 0.025f, 0.06f);
            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/TwoPlanets.unity");
            AssetDatabase.SaveAssets();
        }

        private static PlanetGravity CreatePlanet(string name, Vector3 position, float radius, Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * radius * 2f;
            sphere.GetComponent<Renderer>().sharedMaterial = Material(name, color);
            var planet = sphere.AddComponent<PlanetGravity>();
            for (int i = 0; i < 28; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / 28f;
                float angle = i * 2.399963f;
                Vector3 normal = new Vector3(Mathf.Sqrt(1f - y * y) * Mathf.Cos(angle), y,
                    Mathf.Sqrt(1f - y * y) * Mathf.Sin(angle));
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = name + " landmark " + i;
                marker.transform.SetPositionAndRotation(position + normal * (radius + 0.06f), Quaternion.FromToRotation(Vector3.up, normal));
                marker.transform.localScale = new Vector3(0.5f, 0.12f, 1.6f);
                marker.GetComponent<Renderer>().sharedMaterial = Material("Landmarks", new Color(0.8f, 0.9f, 0.95f));
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            return planet;
        }

        private static Material Material(string name, Color color)
        {
            const string folder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            string path = folder + "/TwoPlanets" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void Validate()
        {
            CreateDemo();
            Physics.SyncTransforms();
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var camera = Object.FindAnyObjectByType<PlanetCamera>();
            var home = player.gravitySources[0];
            var moon = player.gravitySources[1];
            var body = player.GetComponent<Rigidbody>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SphericalPlayer).GetMethod("Awake", flags).Invoke(player, null);
            MethodInfo step = typeof(SphericalPlayer).GetMethod("FixedUpdate", flags);
            MethodInfo cameraStep = typeof(PlanetCamera).GetMethod("LateUpdate", flags);
            SimulationMode previousMode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Step(100);
                Require(player.IsGrounded && player.planet == home, "Initial landing");
                Require(player.TryTransfer(), "Outbound launch");
                Vector3 before = player.GravityUp;
                Step(1);
                Require(Vector3.Angle(before, player.GravityUp) <= player.gravityTurnSpeed * Time.fixedDeltaTime + 0.1f, "Bounded gravity rotation");
                Step(250);
                Require(player.planet == moon && player.IsGrounded, "Outbound landing");
                Require(Mathf.Abs(Vector3.Distance(body.position, moon.transform.position) - 13f) < 0.2f, "Moon surface radius");
                player.Respawn();
                Require(player.planet == moon && body.linearVelocity.sqrMagnitude < 0.001f, "Moon checkpoint and velocity reset");
                Step(30);
                Require(player.TryTransfer(), "Return launch");
                Step(250);
                Require(player.planet == home && player.IsGrounded, "Return landing");

                // In the overlap, a two-unit advantage must not cross the three-unit switch margin.
                Vector3 route = (moon.transform.position - home.transform.position).normalized;
                float boundary = (Vector3.Distance(home.transform.position, moon.transform.position) + home.Radius - moon.Radius) * 0.5f;
                player.SwitchPlanet(moon);
                body.position = home.transform.position + route * (boundary - 1f);
                body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                Step(2);
                Require(player.planet == moon, "Gravity boundary hysteresis");

                // An airborne player entering the other field switches without pressing E.
                player.SwitchPlanet(home);
                body.position = moon.transform.position - route * (moon.Radius + 3f);
                body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                Step(2);
                Require(player.planet == moon, "Automatic gravity capture");
                Step(250);
                Require(player.IsGrounded, "Automatic capture landing");

                int count = player.RespawnCount;
                body.position = new Vector3(70f, 46f, 0f);
                body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                Step(Mathf.CeilToInt(player.lostInSpaceDelay / Time.fixedDeltaTime) + 5);
                Require(player.RespawnCount == count + 1 && player.planet == moon, "Lost-in-space timeout and checkpoint");
                Step(30);
                body.position = Vector3.one * 500f;
                body.linearVelocity = Vector3.one * 100f;
                Physics.SyncTransforms();
                Step(1);
                Require(player.RespawnCount == count + 2 && body.linearVelocity.magnitude < 1f, "Far-distance respawn");
                body.position = Vector3.one * 250f;
                Physics.SyncTransforms();
                player.Respawn();
                cameraStep.Invoke(camera, null);
                Require(camera.planet == moon && Vector3.Distance(camera.transform.position, body.position) < 15f, "Camera respawn reset");
                foreach (var source in player.gravitySources)
                    Require(Vector3.Distance(camera.transform.position, source.transform.position) > source.Radius, "Camera outside both planets");
                Debug.Log("TWO_PLANET_VALIDATION_PASSED: round trip, bounded gravity turn, boundary hysteresis, automatic capture, checkpoint, timeout/distance respawn, camera reset and obstruction.");
            }
            finally { Physics.simulationMode = previousMode; }

            void Step(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    step.Invoke(player, null);
                    Physics.Simulate(Time.fixedDeltaTime);
                    cameraStep.Invoke(camera, null);
                    Require(float.IsFinite(camera.transform.position.x) && float.IsFinite(player.GravityUp.x), "Finite camera and gravity");
                    foreach (var source in player.gravitySources)
                        Require(Vector3.Distance(camera.transform.position, source.transform.position) >= source.Radius, "Camera clearance during travel");
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Two planet validation failed: " + message);
        }
    }
}
