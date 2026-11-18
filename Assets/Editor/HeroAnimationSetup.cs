using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class HeroAnimationSetup
    {
        [MenuItem("GravityGame/Art/Add Hero Walking and Cape Motion")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try { UpgradeAssets(); ValidateAndPreview(); }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        public static void UpgradeAssets()
        {
            UpgradeCape(SuperheroAssets.Folder + "/Prefabs/OxygenCape.prefab");
            string path = SuperheroAssets.HeroPath;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var motion = root.GetComponent<HeroLocomotion>();
                if (motion == null) motion = root.AddComponent<HeroLocomotion>();
                motion.leftArm = Joint(root.transform, "Left shoulder", -1f, true);
                motion.rightArm = Joint(root.transform, "Right shoulder", 1f, true);
                motion.leftLeg = Joint(root.transform, "Left hip", -1f, false);
                motion.rightLeg = Joint(root.transform, "Right hip", 1f, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static void UpgradeCape(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cape = root.GetComponentInChildren<OxygenCape>();
                if (cape.GetComponent<CapeFlutter>() == null) cape.gameObject.AddComponent<CapeFlutter>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Transform Joint(Transform root, string name, float side, bool arm)
        {
            var existing = root.Find(name);
            if (existing != null) return existing;
            var joint = new GameObject(name).transform;
            joint.SetParent(root, false);
            joint.localPosition = new Vector3(side * (arm ? 0.42f : 0.18f), arm ? 0.12f : -0.40f, 0f);
            // Snapshot the original child list before moving parts; keep their authored world pose.
            var children = new Transform[root.childCount];
            for (int i = 0; i < children.Length; i++) children[i] = root.GetChild(i);
            foreach (var child in children)
                if ((child.name == (arm ? "Arm" : "Leg") || child.name == (arm ? "Glove" : "Boot")) &&
                    Mathf.Sign(child.localPosition.x) == side)
                    child.SetParent(joint, true);
            Require(joint.childCount == 2, name + " binds both limb pieces");
            return joint;
        }

        private static void ValidateAndPreview()
        {
            foreach (string path in new[] { "Assets/Scenes/PhaseOne.unity", "Assets/Scenes/TwoPlanets.unity",
                "Assets/Scenes/MobilePrototype.unity", FirstExpedition.ScenePath })
            {
                EditorSceneManager.OpenScene(path);
                var player = Object.FindAnyObjectByType<SphericalPlayer>();
                var motion = player.GetComponentInChildren<HeroLocomotion>();
                Require(motion != null && motion.leftArm != null && motion.rightArm != null &&
                    motion.leftLeg != null && motion.rightLeg != null, "Scene inherits animated hero: " + path);
                Require(player.GetComponentInChildren<CapeFlutter>() != null, "Scene inherits cape motion");
                Require(motion.GetComponentsInChildren<Collider>().Length == 0, "Art adds no physics");
            }
            HeroAnimationChecks.Run();
            RenderPreview();
            Debug.Log("HERO_ANIMATION_PASSED: all four scenes inherit the rig; walking/running, heading, airborne settling, pause, retry and isolated cape mesh checked.");
        }

        private static void RenderPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuperheroAssets.HeroPath);
            for (int i = 0; i < 3; i++)
            {
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                hero.transform.position = new Vector3((i - 1) * 1.6f, 0f, 0f);
                var motion = hero.GetComponent<HeroLocomotion>();
                if (i > 0)
                {
                    float stride = i == 1 ? 23f : 39f;
                    motion.leftLeg.localRotation = Quaternion.Euler(stride, 0f, 0f);
                    motion.rightLeg.localRotation = Quaternion.Euler(-stride, 0f, 0f);
                    motion.leftArm.localRotation = Quaternion.Euler(-stride, 0f, 0f);
                    motion.rightArm.localRotation = Quaternion.Euler(stride, 0f, 0f);
                    hero.transform.rotation = Quaternion.Euler(i == 2 ? 7f : 0f, -30f, 0f);
                }
                hero.GetComponentInChildren<OxygenCape>().SetOxygenVisual(1f);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.64f, 0.72f);
            var light = new GameObject("Key", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            var camera = new GameObject("Pose preview", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(1.8f, 1.3f, 8f);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 1.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.07f);
            var target = new RenderTexture(1500, 850, 24);
            target.Create();
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < 2; i++) RenderPipeline.SubmitRenderRequest(camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/hero-walk-run.png", image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Hero animation: " + message);
        }
    }
}
