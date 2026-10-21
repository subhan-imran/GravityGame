using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class MobileDemo
    {
        [MenuItem("GravityGame/Create Mobile Prototype")]
        public static void CreateDemo()
        {
            // Reuse the desktop demo's authored planets without changing its saved configuration.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            TwoPlanetDemo.CreateDemo();
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var camera = Object.FindAnyObjectByType<PlanetCamera>();
            player.useMobileUI = true;
            player.showTravelHelp = false;
            player.victoryPlanet = player.gravitySources[1];
            camera.assisted = true;
            camera.distance = 10f;
            camera.defaultPitch = 8f;
            camera.GetComponent<Camera>().fieldOfView = 72f;
            var ui = new GameObject("Mobile Interface").AddComponent<MobileGameUI>();
            ui.player = player;
            ui.cameraRig = camera;
            ui.BuildUI();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/MobilePrototype.unity");
        }

        public static void ValidateAll()
        {
            TwoPlanetDemo.ValidateAll();
            Validate();
        }

        // Upgrade the saved mobile scene in place so existing scene objects keep their identity.
        public static void UpgradeVictoryScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MobilePrototype.unity");
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            ui.player.victoryPlanet = ui.player.gravitySources[1];
            ui.BuildUI();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("VICTORY_SCENE_UPGRADED");
        }

        // Invoke on a temporary project copy with Unity's -buildTarget Android argument.
        public static void BuildAndroid()
        {
            CreateDemo();
            PlayerSettings.productName = "GravityGame Prototype";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gravitygame.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            System.IO.Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MobilePrototype.unity" },
                locationPathName = "Builds/GravityGame-Prototype.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android prototype build failed: " + report.summary.result);
            Debug.Log("MOBILE_ANDROID_BUILD_PASSED: Builds/GravityGame-Prototype.apk");
        }

        public static void Validate()
        {
            CreateDemo();
            ValidateCurrentScene();
        }

        public static void ValidateCurrentScene()
        {
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            ui.ResumeGame();
            var player = ui.player;
            var body = player.GetComponent<Rigidbody>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SphericalPlayer).GetMethod("Awake", flags).Invoke(player, null);
            ui.Initialize();
            var update = typeof(SphericalPlayer).GetMethod("Update", flags);
            var step = typeof(SphericalPlayer).GetMethod("FixedUpdate", flags);
            var cameraStep = typeof(PlanetCamera).GetMethod("LateUpdate", flags);
            var uiUpdate = typeof(MobileGameUI).GetMethod("Update", flags);
            SimulationMode mode = Physics.simulationMode;
            float clock = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                Step(80);
                Canvas.ForceUpdateCanvases();
                Require(player.IsGrounded && player.useMobileUI && ui.cameraRig.assisted, "Mobile scene wiring");

                var stick = ui.Joystick;
                var finger = Pointer(101, StickPoint(0.45f));
                ExecuteEvents.Execute(stick.gameObject, finger, ExecuteEvents.pointerDownHandler);
                Step(35);
                float walking = Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude;
                Require(!player.IsRunning && walking > 1f && walking < player.speed, "Partial stick walks");
                finger.position = StickPoint(1f);
                ExecuteEvents.Execute(stick.gameObject, finger, ExecuteEvents.dragHandler);
                Step(45);
                Require(player.IsRunning && player.RunningJumpReady &&
                    Vector3.ProjectOnPlane(body.linearVelocity, player.GravityUp).magnitude > player.speed + 4f, "Outer stick automatically runs");

                var foreign = Pointer(999, finger.position);
                ExecuteEvents.Execute(stick.gameObject, foreign, ExecuteEvents.pointerUpHandler);
                Require(stick.HasPointer && stick.Value.y > 0.9f, "A different finger cannot release movement");
                RefreshInteractionRender();
                var jumping = Pointer(202, ScreenPoint(ui.JumpControl.transform));
                var hits = new List<RaycastResult>();
                ui.Events.RaycastAll(jumping, hits);
                Debug.Log("MOBILE_RAYCAST: hits=" + hits.Count + " first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") +
                    " jump=" + jumping.position + " depth=" + ui.JumpControl.GetComponent<UnityEngine.UI.Graphic>().depth +
                    " active=" + ui.JumpControl.gameObject.activeInHierarchy + " screen=" + Screen.width + "x" + Screen.height);
                if (Application.isPlaying)
                    Require(hits.Count > 0 && hits[0].gameObject == ui.JumpControl.gameObject, "Jump receives touches instead of camera look area");
                else
                    Require(ui.JumpControl.transform.GetSiblingIndex() > ui.LookArea.transform.GetSiblingIndex() &&
                        ui.JumpControl.GetComponent<UnityEngine.UI.Image>().raycastTarget, "Jump is authored above the camera look area (rendered routing checked in Play mode)");
                ExecuteEvents.Execute(ui.JumpControl.gameObject, jumping, ExecuteEvents.pointerDownHandler);
                Step(1);
                Require(Vector3.Dot(body.linearVelocity, player.GravityUp) > 15f && stick.HasPointer, "Jump while holding the running finger");
                ExecuteEvents.Execute(ui.JumpControl.gameObject, jumping, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(stick.gameObject, finger, ExecuteEvents.pointerUpHandler);
                Require(!stick.HasPointer && player.MovementInput == Vector2.zero, "Joystick release clears movement");

                var headingField = typeof(PlanetCamera).GetField("heading", flags);
                Vector3 beforeLook = (Vector3)headingField.GetValue(ui.cameraRig);
                var view = ui.cameraRig.GetComponent<Camera>();
                var looking = Pointer(303, new Vector2(view.pixelWidth * 0.7f, view.pixelHeight * 0.6f));
                ExecuteEvents.Execute(ui.LookArea.gameObject, looking, ExecuteEvents.pointerDownHandler);
                looking.delta = new Vector2(120f, -40f);
                ExecuteEvents.Execute(ui.LookArea.gameObject, looking, ExecuteEvents.dragHandler);
                cameraStep.Invoke(ui.cameraRig, null);
                Require(Vector3.Angle(beforeLook, (Vector3)headingField.GetValue(ui.cameraRig)) > 1f,
                    "Touch drag adjusts the camera heading before its smoothed position follows");

                Vector3 pausedPosition = body.position;
                float remaining = player.SpaceTimeRemaining;
                Click(ui.PauseButton.gameObject);
                Require(player.IsPaused && ui.PauseVisible && Time.timeScale == 0f && !ui.LookArea.HasPointer, "Pause screen releases held touches");
                ui.PauseGame(); // Repeated pause must not overwrite the clock to restore with zero.
                Step(20);
                Require(body.position == pausedPosition && Mathf.Approximately(remaining, player.SpaceTimeRemaining), "Pause freezes motion and loss countdown");
                Click(ui.ResumeButton.gameObject);
                Require(!player.IsPaused && !ui.PauseVisible && Time.timeScale == 1f && player.MovementInput == Vector2.zero, "Resume restores clock without stuck input");

                typeof(MobileGameUI).GetMethod("OnApplicationFocus", flags).Invoke(ui, new object[] { false });
                Require(player.IsPaused && ui.PauseVisible, "Focus interruption pauses");
                typeof(MobileGameUI).GetMethod("OnApplicationFocus", flags).Invoke(ui, new object[] { true });
                Require(player.IsPaused, "Regaining focus does not resume unexpectedly");
                ui.ResumeGame();

                ui.ApplySafeArea(new Rect(80f, 20f, 1120f, 680f), new Vector2(1280f, 720f));
                Require(Vector2.Distance(ui.SafeRoot.anchorMin, new Vector2(0.0625f, 20f / 720f)) < 0.001f &&
                    Vector2.Distance(ui.SafeRoot.anchorMax, new Vector2(0.9375f, 700f / 720f)) < 0.001f, "Safe-area insets");
                ui.ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));

                int retries = player.RespawnCount;
                body.position = Vector3.one * 500f;
                Physics.SyncTransforms();
                Step(1);
                Require(player.HasLost && ui.LossVisible && !ui.Joystick.gameObject.activeInHierarchy, "Touch-friendly defeat screen blocks gameplay controls");
                Click(ui.RetryButton.gameObject);
                Require(!player.HasLost && !player.IsPaused && !ui.LossVisible && player.RespawnCount == retries + 1 && Time.timeScale == 1f,
                    "Touch retry restores a playable checkpoint");
                Step(50);
                Require(!player.HasWon && !ui.VictoryVisible, "Starting-planet landings do not complete the level");
                PlanetGravity home = player.planet;
                PlanetGravity goal = player.victoryPlanet;
                Require(goal != null && goal != home, "Mobile victory destination is the second planet");
                PlaceAtGoal(3f);
                Step(10);
                Require(player.IsInGravityField && !player.IsGrounded && !player.HasWon, "Entering destination gravity while airborne is not victory");
                PlaceAtGoal(1.05f);
                Step(2);
                Require(!player.HasWon, "Brief surface contact does not immediately win");
                PlaceAtGoal(3f);
                Step(1);
                PlaceAtGoal(1.05f);
                Step(8);
                Require(!player.HasWon, "Breaking contact resets the stable-landing timer");
                Step(30);
                Require(player.HasWon && ui.VictoryVisible && !ui.LossVisible && !ui.PauseVisible &&
                    !ui.Joystick.gameObject.activeInHierarchy && body.isKinematic, "Stable destination landing shows victory and freezes gameplay");
                Vector3 wonPosition = body.position;
                player.SetTouchMovement(Vector2.up);
                player.RequestJump();
                Step(30);
                ui.PauseGame();
                typeof(MobileGameUI).GetMethod("OnApplicationFocus", flags).Invoke(ui, new object[] { false });
                Require(player.HasWon && !player.IsPaused && ui.VictoryVisible && body.position == wonPosition &&
                    player.MovementInput == Vector2.zero && Time.timeScale == 1f, "Victory remains stable through controls and focus interruptions");
                RefreshInteractionRender();
                var playAgainTouch = Pointer(505, ScreenPoint(ui.PlayAgainButton.transform));
                hits.Clear();
                ui.Events.RaycastAll(playAgainTouch, hits);
                if (Application.isPlaying)
                    Require(hits.Count > 0 && hits[0].gameObject == ui.PlayAgainButton.gameObject, "Play Again receives the victory-screen touch");
                int restarts = player.RespawnCount;
                ExecuteEvents.Execute(ui.PlayAgainButton.gameObject, playAgainTouch, ExecuteEvents.pointerClickHandler);
                Require(!player.HasWon && !player.HasLost && !player.IsPaused && !ui.VictoryVisible &&
                    !body.isKinematic && player.planet == home && player.RespawnCount == restarts + 1 && player.MovementInput == Vector2.zero,
                    "Play Again starts on the original planet instead of the winning checkpoint");
                Step(50);
                Require(player.IsGrounded && !player.HasWon && !ui.VictoryVisible, "New attempt remains playable on the starting planet");
                Debug.Log("VICTORY_VALIDATION_PASSED: gravity entry versus stable landing, contact reset, terminal controls, rendered Play Again routing, fresh starting-planet attempt.");
                ui.PauseGame();
                typeof(MobileGameUI).GetMethod("OnDisable", flags).Invoke(ui, null);
                Require(Time.timeScale == 1f && !player.IsPaused, "Leaving the scene cannot leave the global clock paused");
                Debug.Log("MOBILE_VALIDATION_PASSED: partial/full joystick, independent movement/jump pointers, touch raycast priority, drag look, pause/resume, focus interruption, safe area, defeat/retry, clock cleanup.");
            }
            finally
            {
                ui.ResumeGame();
                Time.timeScale = clock;
                Physics.simulationMode = mode;
            }

            Vector2 StickPoint(float amount)
            {
                RefreshInteractionRender();
                var rect = (RectTransform)ui.Joystick.transform;
                return RectTransformUtility.WorldToScreenPoint(EventCamera(), rect.TransformPoint(new Vector3(0f, rect.rect.height * 0.5f * amount, 0f)));
            }
            Camera EventCamera() => ui.GetComponentInChildren<Canvas>().worldCamera;
            Vector2 ScreenPoint(Transform item)
            {
                RefreshInteractionRender();
                return RectTransformUtility.WorldToScreenPoint(EventCamera(), item.position);
            }
            PointerEventData Pointer(int id, Vector2 position) => new PointerEventData(ui.Events)
                { pointerId = id, position = position, button = PointerEventData.InputButton.Left,
                    pointerPressRaycast = new RaycastResult { module = ui.GetComponentInChildren<UnityEngine.UI.GraphicRaycaster>() } };
            void Click(GameObject button) => ExecuteEvents.Execute(button, Pointer(404, ScreenPoint(button.transform)), ExecuteEvents.pointerClickHandler);
            void PlaceAtGoal(float altitude)
            {
                body.position = player.victoryPlanet.transform.position + Vector3.up * (player.victoryPlanet.Radius + altitude);
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                player.planet = player.victoryPlanet;
                player.transform.SetPositionAndRotation(body.position, body.rotation);
                Physics.SyncTransforms();
            }
            void Step(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    update.Invoke(player, null);
                    step.Invoke(player, null);
                    if (!player.IsPaused) Physics.Simulate(Time.fixedDeltaTime);
                    cameraStep.Invoke(ui.cameraRig, null);
                    uiUpdate.Invoke(ui, null);
                    foreach (var source in player.gravitySources)
                        Require(Vector3.Distance(ui.cameraRig.transform.position, source.transform.position) >= source.Radius,
                            "Assisted camera stays outside planet geometry");
                }
            }
        }

        // Run in a graphics-enabled batch editor to inspect actual Unity UI at phone proportions.
        public static void RenderPreviews()
        {
            CreateDemo();
            RenderCurrentPreviews();
        }

        public static void RenderCurrentPreviews()
        {
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            ui.ResumeGame();
            typeof(SphericalPlayer).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.player, null);
            ui.Initialize();
            var mode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 60; i++)
            {
                typeof(SphericalPlayer).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.player, null);
                Physics.Simulate(Time.fixedDeltaTime);
            }
            Physics.simulationMode = mode;
            typeof(PlanetCamera).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.cameraRig, null);
            typeof(PlanetCamera).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.cameraRig, null);
            var view = ui.cameraRig.GetComponent<Camera>();
            var canvas = ui.GetComponentInChildren<Canvas>();
            var texture = new RenderTexture(1280, 720, 24);
            texture.Create();
            view.targetTexture = texture;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = view;
            canvas.planeDistance = 0.5f;
            typeof(UnityEngine.UI.CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(canvas.GetComponent<UnityEngine.UI.CanvasScaler>(), null);
            try
            {
                Capture("mobile-gameplay.png");
                ui.PauseGame();
                Capture("mobile-pause.png");
                ui.ResumeGame();
                ui.player.GetComponent<Rigidbody>().position = Vector3.one * 500f;
                typeof(SphericalPlayer).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.player, null);
                ui.Refresh();
                Capture("mobile-loss.png");
                ui.PlayAgain();
                var goal = ui.player.victoryPlanet;
                var body = ui.player.GetComponent<Rigidbody>();
                ui.player.planet = goal;
                body.position = goal.transform.position + Vector3.up * (goal.Radius + 1.05f);
                body.rotation = Quaternion.identity;
                ui.player.transform.SetPositionAndRotation(body.position, body.rotation);
                Physics.SyncTransforms();
                var previewMode = Physics.simulationMode;
                Physics.simulationMode = SimulationMode.Script;
                for (int i = 0; i < 60; i++)
                {
                    typeof(SphericalPlayer).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.player, null);
                    Physics.Simulate(Time.fixedDeltaTime);
                }
                Physics.simulationMode = previewMode;
                typeof(PlanetCamera).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.cameraRig, null);
                Capture("mobile-victory.png");
            }
            finally
            {
                ui.ResumeGame();
                view.targetTexture = null;
                texture.Release();
                Object.DestroyImmediate(texture);
            }
            void Capture(string name)
            {
                Canvas.ForceUpdateCanvases();
                ui.Refresh();
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                var previous = RenderTexture.active;
                RenderTexture.active = texture;
                var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                pixels.Apply();
                System.IO.File.WriteAllBytes("Logs/" + name, pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);
                RenderTexture.active = previous;
            }
        }

        private static RenderTexture interactionTexture;
        private static Camera interactionCamera;
        private static Canvas interactionCanvas;

        public static void BeginInteractionRender()
        {
            var ui = Object.FindAnyObjectByType<MobileGameUI>();
            interactionCamera = ui.cameraRig.GetComponent<Camera>();
            interactionCanvas = ui.GetComponentInChildren<Canvas>();
            interactionTexture = new RenderTexture(1280, 720, 24);
            interactionTexture.Create();
            interactionCamera.targetTexture = interactionTexture;
            interactionCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            interactionCanvas.worldCamera = interactionCamera;
            interactionCanvas.planeDistance = 0.5f;
            typeof(UnityEngine.UI.CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(interactionCanvas.GetComponent<UnityEngine.UI.CanvasScaler>(), null);
            RefreshInteractionRender();
        }

        private static void RefreshInteractionRender()
        {
            if (interactionTexture == null) return;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(interactionCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = interactionTexture });
        }

        public static void EndInteractionRender()
        {
            if (interactionCamera != null) interactionCamera.targetTexture = null;
            if (interactionCanvas != null)
            {
                interactionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                interactionCanvas.worldCamera = null;
            }
            if (interactionTexture != null) { interactionTexture.Release(); Object.DestroyImmediate(interactionTexture); }
            interactionTexture = null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Mobile validation failed: " + message);
        }
    }
}
