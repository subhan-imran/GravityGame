using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class OpenWorldChecks
    {
        private const string ValidationWorld = "open-world-regression";

        public static void ValidateAndSave()
        {
            var world = Open();
            PlayerPrefs.DeleteKey(world.SaveKey);
            var level = world.Level;
            var player = level.player;
            var menu = level.interfaceRoot.boothMenu;
            var body = player.GetComponent<Rigidbody>();
            Invoke(world, "Awake");
            Invoke(player, "Awake");
            level.interfaceRoot.Initialize();
            Invoke(menu, "Awake");
            Require(world.HasValidIds() && world.Booths.Length == 3 && level.objective.TotalCount == 9,
                "Three booths and nine separately identified fragments");
            Require(PrefabUtility.IsPartOfPrefabInstance(menu) && world.Booths.All(item =>
                PrefabUtility.IsPartOfPrefabInstance(item)), "Reusable booth and menu prefabs");
            Require(level.player.gravitySources.Select(item => item.Radius).OrderBy(r => r)
                .SequenceEqual(new[] { 24f, 30f, 42f }), "Larger planet surfaces");

            SimulationMode previousMode = Physics.simulationMode;
            float previousScale = Time.timeScale;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Time.timeScale = 1f;
                Physics.SyncTransforms();
                Step(player, 30);
                Invoke(level.cameraRig, "OnEnable");
                Invoke(level.cameraRig, "LateUpdate");
                Invoke(menu, "Update");
                Require(menu.openButton.gameObject.activeSelf, "Starting booth is available from the arrival spot");
                Capture(level.interfaceRoot, "open-world-arrival.png");
                Flight("Haven", "Relay", player, body);
                Flight("Relay", "Haven", player, body);
                Flight("Relay", "Farpoint", player, body);
                Flight("Farpoint", "Relay", player, body);
                menu.Open();
                Require(!menu.IsOpen && !PlayerPrefs.HasKey(world.SaveKey), "Cannot open or save from a distant landing");

                // Exercise the actual UI button listeners and save at a planet other than the start.
                foreach (var star in level.objective.stars)
                    Require(level.objective.TryCollect(star, player), "Fragment collectable");
                Require(level.objective.AllCollected && !player.HasWon, "Free exploration continues after finding every fragment");
                level.objective.ResetProgress();
                Require(level.objective.TryCollect(level.objective.stars[0], player), "First fragment ready to save");

                var booth = world.Booths.Single(item => item.persistentId == "farpoint-station");
                booth.GetArrivalPose(out var position, out var rotation);
                Place(player, body, booth.planet, position, rotation);
                Step(player, 30);
                Invoke(level.cameraRig, "LateUpdate");
                menu.openButton.onClick.Invoke();
                Require(menu.IsOpen && menu.panel.activeSelf && player.IsPaused && !level.interfaceRoot.PauseVisible,
                    "Booth shows its own modal and pauses play");
                Require(menu.inventoryPage.activeSelf && menu.inventoryText.text.Contains("1 / 9"), "Booth inventory reflects collected fragments");
                Capture(level.interfaceRoot, "open-world-booth-inventory.png");
                menu.settingsTab.onClick.Invoke();
                Require(menu.settingsPage.activeSelf && !menu.inventoryPage.activeSelf, "Settings tab opens");
                menu.volumeDown.onClick.Invoke();
                menu.lookUp.onClick.Invoke();
                Require(Mathf.Approximately(world.Volume, 0.9f) && Mathf.Approximately(AudioListener.volume, 0.9f)
                    && Mathf.Approximately(world.LookMultiplier, 1.25f), "Booth settings apply immediately");
                Require(!PlayerPrefs.HasKey(world.SaveKey), "Only Save writes world progress");
                menu.saveButton.onClick.Invoke();
                Require(PlayerPrefs.HasKey(world.SaveKey) && menu.statusText.text == "Saved at Farpoint Station.", "Save button confirms success");
                Capture(level.interfaceRoot, "open-world-booth-settings.png");
                menu.closeButton.onClick.Invoke();
                Require(!menu.IsOpen && !player.IsPaused && Time.timeScale == 1f, "Closing the booth resumes play");
                Require(level.objective.TryCollect(level.objective.stars[1], player), "Can collect after saving");
                world.SetVolume(0.2f);
                Debug.Log("OPEN_WORLD_SAVED: four two-way crossings, booth access, inventory, settings, explicit save and free exploration passed.");
            }
            finally
            {
                menu.Close();
                Physics.simulationMode = previousMode;
                Time.timeScale = previousScale;
            }
        }

        // Run in a second Unity process to check persistence rather than PlayerPrefs' in-memory cache.
        public static void ValidateReopen()
        {
            var world = Open();
            var level = world.Level;
            var player = level.player;
            var body = player.GetComponent<Rigidbody>();
            try
            {
                Require(PlayerPrefs.HasKey(world.SaveKey), "Saved data reached a new process");
                Invoke(world, "Awake");
                Invoke(player, "Awake");
                var booth = world.Booths.Single(item => item.persistentId == "farpoint-station");
                booth.GetArrivalPose(out var position, out _);
                Require(player.planet == booth.planet && Vector3.Distance(body.position, position) < 0.01f,
                    "Reopen restores saved booth away from starting planet");
                Require(level.objective.CollectedCount == 1 && level.objective.stars[0].Collected &&
                    !level.objective.stars[0].visual.gameObject.activeSelf && !level.objective.stars[1].Collected,
                    "Reopen restores exactly the saved inventory");
                Require(Mathf.Approximately(world.Volume, 0.9f) && Mathf.Approximately(world.LookMultiplier, 1.25f),
                    "Reopen restores audio and sensitivity settings");
                player.Respawn();
                Require(player.planet == booth.planet && Vector3.Distance(body.position, position) < 0.2f,
                    "Reloaded booth is the initial recovery location");
                Debug.Log("OPEN_WORLD_REOPEN_PASSED: booth spawn, saved fragments, settings and recovery restored in a new Unity process.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(world.SaveKey);
                PlayerPrefs.Save();
            }
        }

        private static WorldProgress Open()
        {
            EditorSceneManager.OpenScene(OpenWorldSetup.ScenePath);
            var world = Object.FindAnyObjectByType<WorldProgress>();
            Require(world != null, "World is present in the saved scene");
            world.worldId = ValidationWorld;
            world.Level.RefreshReferences();
            return world;
        }

        private static void Flight(string from, string to, SphericalPlayer player, Rigidbody body)
        {
            var source = player.gravitySources.Single(item => item.name == from);
            var target = player.gravitySources.Single(item => item.name == to);
            Vector3 up = (target.transform.position - source.transform.position).normalized;
            Place(player, body, source, source.transform.position + up * (source.Radius + 1.05f),
                Quaternion.FromToRotation(Vector3.up, up));
            Step(player, 75);
            Require(player.IsGrounded && player.planet == source, "Grounded at " + from);
            bool crossedSpace = false;
            for (int stage = 1; stage <= 3; stage++)
            {
                player.RequestJump();
                Step(player, 1);
                Require(player.JumpChain == stage, "Correct jump stage at " + from);
                for (int i = 0; i < 400 && !player.IsGrounded && !player.HasLost; i++)
                {
                    Step(player, 1);
                    if (stage == 3 && !player.IsInGravityField) crossedSpace = true;
                }
                Require(!player.HasLost && player.IsGrounded && player.planet == (stage == 3 ? target : source),
                    "Landing on " + (stage == 3 ? to : from) + " after jump " + stage);
            }
            Require(crossedSpace, "Route crosses a real gravity gap");
            Debug.Log("OPEN_WORLD_ROUTE_PASSED: " + from + " to " + to);
        }

        private static void Place(SphericalPlayer player, Rigidbody body, PlanetGravity planet, Vector3 position, Quaternion rotation)
        {
            player.Respawn();
            player.planet = planet;
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            player.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
        }

        private static void Step(SphericalPlayer player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Invoke(player, "FixedUpdate");
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        private static void Invoke(MonoBehaviour component, string method) =>
            component.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(component, null);

        private static void Capture(MobileGameUI ui, string file)
        {
            MobileDemo.BeginInteractionRender();
            var view = ui.cameraRig.GetComponent<Camera>();
            var texture = view.targetTexture;
            var previous = RenderTexture.active;
            var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes("Logs/" + file, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels);
                MobileDemo.EndInteractionRender();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Open-world validation: " + message);
        }
    }
}
