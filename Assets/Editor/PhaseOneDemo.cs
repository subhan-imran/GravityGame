using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GravityGame.Editor
{
    public static class PhaseOneDemo
    {
        // Batch entry point: compile, generate a playable scene, and verify its wiring and gravity.
        public static void Validate()
        {
            CreateDemo();
            Physics.SyncTransforms();
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var camera = Object.FindAnyObjectByType<PlanetCamera>();
            if (player == null || camera == null || player.planet == null ||
                camera.target != player.transform || player.movementCamera != camera.transform)
                throw new System.Exception("Demo references are incomplete.");
            var planet = player.planet;
            foreach (Vector3 axis in new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                Vector3 position = planet.transform.position + axis * 21f;
                if (Vector3.Distance(planet.AccelerationAt(position), -axis * planet.acceleration) > 0.001f)
                    throw new System.Exception("Gravity direction or magnitude is incorrect.");
                if (!planet.GetComponent<Collider>().Raycast(new Ray(position, -axis), out _, 1.1f))
                    throw new System.Exception("Surface probe failed.");
            }
            if (planet.UpAt(planet.transform.position).sqrMagnitude < 0.99f)
                throw new System.Exception("Gravity center fallback is invalid.");
            var body = player.GetComponent<Rigidbody>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(SphericalPlayer).GetMethod("Awake", flags).Invoke(player, null);
            var physicsStep = typeof(SphericalPlayer).GetMethod("FixedUpdate", flags);
            var previousMode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                foreach (Vector3 axis in new[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    body.position = planet.transform.position + axis * 23f;
                    body.rotation = Quaternion.FromToRotation(Vector3.up, axis);
                    body.linearVelocity = Vector3.zero;
                    Physics.SyncTransforms();
                    for (int step = 0; step < 200; step++)
                    {
                        physicsStep.Invoke(player, null);
                        Physics.Simulate(Time.fixedDeltaTime);
                    }
                    float radius = Vector3.Distance(body.position, planet.transform.position);
                    if (Mathf.Abs(radius - 21f) > 0.15f || !player.IsGrounded)
                        throw new System.Exception("Player did not settle on the surface at " + axis + ": " + radius);
                }
            }
            finally { Physics.simulationMode = previousMode; }
            // Restore the initial spawn rather than saving the validation pose.
            Debug.Log("PHASE_ONE_VALIDATION_PASSED: scene wiring, six gravity directions, probes, center fallback, six physics landings.");
        }

        [MenuItem("GravityGame/Create Phase 1 Demo")]
        public static void CreateDemo()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var planetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetObject.name = "Planet";
            planetObject.transform.localScale = Vector3.one * 40f;
            var planet = planetObject.AddComponent<PlanetGravity>();

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = Vector3.up * 21.05f;
            player.AddComponent<Rigidbody>();
            var controller = player.AddComponent<SphericalPlayer>();
            controller.planet = planet;

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<PlanetCamera>();
            camera.target = player.transform;
            camera.planet = planet;
            controller.movementCamera = cameraObject.transform;
            cameraObject.transform.position = new Vector3(0f, 24f, -7f);
            cameraObject.transform.LookAt(player.transform);

            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.55f);

            // Landmarks reveal travel around the otherwise uniform sphere.
            for (int i = 0; i < 24; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / 24f;
                float angle = i * 2.399963f;
                Vector3 normal = new Vector3(Mathf.Sqrt(1f - y * y) * Mathf.Cos(angle), y,
                    Mathf.Sqrt(1f - y * y) * Mathf.Sin(angle));
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Landmark " + i;
                marker.transform.SetPositionAndRotation(normal * 20.1f, Quaternion.FromToRotation(Vector3.up, normal));
                marker.transform.localScale = new Vector3(0.6f, 0.2f, 2f);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/PhaseOne.unity");
        }
    }
}
