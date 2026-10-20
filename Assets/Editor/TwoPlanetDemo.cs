using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
            planet.influenceHeight = 6f;
            var boundaryMaterial = Material(name + "GravityBoundary", Color.Lerp(color, Color.white, 0.55f), true);
            for (int plane = 0; plane < 3; plane++)
            {
                var ring = new GameObject(name + " gravity boundary " + plane, typeof(LineRenderer));
                ring.transform.SetParent(sphere.transform, false);
                var line = ring.GetComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = true;
                line.widthMultiplier = 0.035f;
                line.sharedMaterial = boundaryMaterial;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.positionCount = 128;
                float localRadius = (radius + planet.influenceHeight) / (radius * 2f);
                for (int point = 0; point < line.positionCount; point++)
                {
                    float angle = point * Mathf.PI * 2f / line.positionCount;
                    float x = Mathf.Cos(angle) * localRadius;
                    float y = Mathf.Sin(angle) * localRadius;
                    line.SetPosition(point, plane == 0 ? new Vector3(x, y, 0f)
                        : plane == 1 ? new Vector3(x, 0f, y) : new Vector3(0f, x, y));
                }
            }
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

        private static Material Material(string name, Color color, bool unlit = false)
        {
            const string folder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            string path = folder + "/TwoPlanets" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
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
            Vector3 spawn = body.position;
            Quaternion spawnRotation = body.rotation;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var awake = typeof(SphericalPlayer).GetMethod("Awake", flags);
            awake.Invoke(player, null);
            var step = typeof(SphericalPlayer).GetMethod("FixedUpdate", flags);
            var update = typeof(SphericalPlayer).GetMethod("Update", flags);
            var cameraStep = typeof(PlanetCamera).GetMethod("LateUpdate", flags);
            var previousMode = Physics.simulationMode;
            var previousInputMode = InputSystem.settings.updateMode;
            const string editInputFlag = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
            var isFeatureEnabled = typeof(InputSettings).GetMethod("IsFeatureEnabled", flags);
            bool previousEditInput = (bool)isFeatureEnabled.Invoke(InputSystem.settings, new object[] { editInputFlag });
            var previousKeyboard = Keyboard.current;
            Keyboard keyboard = null;
            bool sawSpace = false;
            bool previousSpace = false;
            Vector3 previousVelocity = Vector3.zero;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
                // Button edge events need player update counters; editor-only updates do not advance device counters.
                InputSystem.settings.SetInternalFeatureFlag(editInputFlag, true);
                keyboard = InputSystem.AddDevice<Keyboard>();
                keyboard.MakeCurrent();
                SetKeys();
                Step(100);
                Require(player.IsGrounded && player.planet == home, "Initial landing");
                Require(Vector3.Distance(home.transform.position, moon.transform.position) >
                    home.Radius + home.influenceHeight + moon.Radius + moon.influenceHeight, "Separate gravity fields with a space gap");
                Require(home.AccelerationAt(home.transform.position + Vector3.up * (home.Radius + home.influenceHeight + 0.1f)) == Vector3.zero,
                    "Zero force outside a gravity field");

                SetKeys(Key.Space);
                Step(1);
                SetKeys();
                float normalPeak = 0f;
                for (int i = 0; i < 150; i++)
                {
                    Step(1);
                    normalPeak = Mathf.Max(normalPeak, home.SurfaceDistance(body.position));
                }
                Require(normalPeak > 2.5f && normalPeak < home.influenceHeight && player.IsGrounded && player.planet == home,
                    "Normal jump stays inside home gravity");
                SetKeys(Key.RightShift, Key.Space);
                Step(1);
                Require(Vector3.Dot(body.linearVelocity, player.GravityUp) < 12f, "Shift without a run-up gives a normal jump");
                SetKeys();
                Step(150);
                SetKeys(Key.W);
                Step(30);
                float walkingSpeed = Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude;
                SetKeys(Key.W, Key.RightShift);
                Step(30);
                float runningSpeed = Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude;
                Require(walkingSpeed > 5f && runningSpeed > walkingSpeed + 4f, "Right Shift increases actual running speed");

                // Reproduce a player-controlled run-up from the scene's starting position.
                SetKeys();
                body.position = spawn;
                body.rotation = spawnRotation;
                player.planet = home;
                awake.Invoke(player, null);
                player.Respawn();
                Physics.SyncTransforms();
                Step(50);
                int countBeforeFlight = player.RespawnCount;
                SetKeys(Key.W, Key.LeftShift);
                Step(40);
                Debug.Log("RUN_SPEED_CHECK: walk=" + walkingSpeed + " rightShift=" + runningSpeed +
                    " leftShift=" + Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude);
                Require(Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude > player.speed + 4f,
                    "Left Shift increases actual running speed");
                SetKeys(Key.W, Key.LeftShift, Key.Space);
                Step(1);
                for (int i = 0; i < 5 && Vector3.Dot(body.linearVelocity, player.GravityUp) <= 15f; i++) Step(1);
                Require(Vector3.Dot(body.linearVelocity, player.GravityUp) > 15f, "Running jump is higher than a normal jump");
                // Release every key: the successful crossing must come from takeoff momentum.
                SetKeys();
                for (int i = 0; i < 450 && !(player.IsGrounded && player.planet == moon); i++) Step(1);
                Debug.Log("RUN_JUMP_FLIGHT: position=" + body.position + " velocity=" + body.linearVelocity +
                    " planet=" + player.planet.name + " space=" + sawSpace + " respawns=" + (player.RespawnCount - countBeforeFlight));
                Require(sawSpace && player.planet == moon && player.IsGrounded && player.RespawnCount == countBeforeFlight,
                    "Run and jump escapes, coasts through space, enters the next field and lands without a direct launch");
                Step(50); // Let the gradual orientation turn settle before checking the landing checkpoint.
                Require(Mathf.Abs(Vector3.Distance(body.position, moon.transform.position) - 13f) < 0.2f, "Moon surface radius");
                player.Respawn();
                Require(player.planet == moon && body.linearVelocity.sqrMagnitude < 0.001f, "Moon checkpoint and velocity reset");
                Step(30);

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
                Require(camera.planet == moon && Vector3.Distance(camera.transform.position, body.position) < 15f, "Immediate camera respawn reset");
                Debug.Log("TWO_PLANET_VALIDATION_PASSED: both Shift keys, normal/run-up jumps, finite fields, zero-gravity momentum, ballistic capture and landing, checkpoints, timeout/distance respawn, camera clearance/reset.");
            }
            finally
            {
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
                InputSystem.settings.updateMode = previousInputMode;
                InputSystem.settings.SetInternalFeatureFlag(editInputFlag, previousEditInput);
                Physics.simulationMode = previousMode;
            }

            void SetKeys(params Key[] keys)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
                InputSystem.Update();
                update.Invoke(player, null);
            }

            void Step(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var previousPlanet = player.planet;
                    Vector3 upBefore = player.GravityUp;
                    int respawns = player.RespawnCount;
                    step.Invoke(player, null);
                    if (player.planet != previousPlanet && player.RespawnCount == respawns)
                        Require(player.planet.Contains(body.position), "Switch only upon entering a field");
                    if (player.RespawnCount == respawns)
                        Require(Vector3.Angle(upBefore, player.GravityUp) <= player.gravityTurnSpeed * Time.fixedDeltaTime + 0.2f, "Bounded gravity rotation");
                    Physics.Simulate(Time.fixedDeltaTime);
                    if (!player.IsInGravityField)
                    {
                        sawSpace = true;
                        if (previousSpace && player.RespawnCount == respawns)
                            Require(Vector3.Distance(previousVelocity, body.linearVelocity) < 0.01f, "Momentum preserved outside every gravity field");
                    }
                    previousSpace = !player.IsInGravityField;
                    previousVelocity = body.linearVelocity;
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
