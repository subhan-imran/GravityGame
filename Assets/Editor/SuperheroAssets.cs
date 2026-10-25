using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class SuperheroAssets
    {
        public const string Folder = "Assets/Art/Hero";
        public const string HeroPath = Folder + "/Prefabs/StrandedHero.prefab";
        private static readonly Color Blue = new Color(0.035f, 0.25f, 0.95f);
        private static readonly Color Red = new Color(0.95f, 0.035f, 0.065f);

        [MenuItem("GravityGame/Art/Create Stranded Hero and Apply")]
        public static void CreateApplyAndPreview()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateAssets();
            foreach (string path in new[] { "Assets/Scenes/MobilePrototype.unity", FirstExpedition.ScenePath,
                "Assets/Scenes/TwoPlanets.unity", "Assets/Scenes/PhaseOne.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = Object.FindAnyObjectByType<SphericalPlayer>();
                ApplyTo(player);
                var ui = Object.FindAnyObjectByType<MobileGameUI>();
                if (ui != null) ui.BuildUI();
                Require(player.GetComponent<CapsuleCollider>().height == 2f &&
                    player.GetComponent<CapsuleCollider>().radius == 0.5f, "Original player collision dimensions");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            CheckOxygenBinding();
            RenderPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("SUPERHERO_ASSETS_PASSED: hero/cape prefabs, color interpolation, oxygen drain/pause/refill binding, shared material isolation, scene setup and visual preview.");
        }

        public static void ApplyTo(SphericalPlayer player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            if (prefab == null) { CreateAssets(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath); }
            var previous = player.transform.Find("Astronaut Visual");
            if (previous != null) previous.gameObject.SetActive(false);
            var visual = player.transform.Find("Hero Visual");
            if (visual == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
                instance.name = "Hero Visual";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                visual = instance.transform;
            }
            visual.gameObject.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual.gameObject);
            if (previous != null) PrefabUtility.RecordPrefabInstancePropertyModifications(previous.gameObject);
            var original = player.GetComponent<MeshRenderer>();
            if (original != null) original.enabled = false;
            Require(visual.GetComponentsInChildren<Collider>(true).Length == 0 &&
                visual.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Hero art adds no movement physics");
        }

        private static void CreateAssets()
        {
            EnsureFolder(Folder);
            foreach (string part in new[] { "Meshes", "Materials", "Prefabs" }) EnsureFolder(Folder + "/" + part);
            var box = SharedMesh("Box");
            var round = SharedMesh("BevelledSuit");
            var sphere = SharedMesh("HelmetSphere");
            var suit = Material("MidnightSuit", new Color(0.055f, 0.085f, 0.20f));
            var trim = Material("HeroGold", new Color(1f, 0.65f, 0.15f));
            var skin = Material("Skin", new Color(0.66f, 0.39f, 0.23f));
            var hair = Material("Hair", new Color(0.025f, 0.022f, 0.035f));
            var mask = Material("Mask", new Color(0.015f, 0.12f, 0.18f));
            var eyes = Material("Eyes", new Color(0.88f, 0.96f, 1f));
            var capeMaterial = Material("OxygenCapeBlue", Blue, true);
            var detail = MeshAsset("DetailSphere", () => CharacterAssets.Patch(0.5f, -90f, 90f, -180f, 180f, 8, 6));
            var hairMesh = MeshAsset("HairCap", () => CharacterAssets.Patch(0.53f, 20f, 90f, -180f, 180f, 16, 4));
            var maskMesh = MeshAsset("DominoMask", () => CharacterAssets.Patch(0.54f, -6f, 22f, -63f, 63f, 8, 3));
            var crest = MeshAsset("HomeStarCrest", Crest);
            var capeMesh = MeshAsset("FoldedCape", Cape);
            var capePrefab = Prefab("OxygenCape", root =>
            {
                var panel = Part(root, "Cape fabric", capeMesh, capeMaterial, Vector3.zero, Vector3.one);
                var oxygen = panel.AddComponent<OxygenCape>();
                oxygen.fullOxygenBlue = Blue;
                oxygen.emptyOxygenRed = Red;
            });
            Prefab("StrandedHero", root =>
            {
                PrefabUtility.InstantiatePrefab(capePrefab, root.transform);
                Part(root, "Hero suit", round, suit, new Vector3(0f, -0.1f, 0f), new Vector3(0.74f, 0.66f, 0.47f));
                Part(root, "Head", sphere, skin, new Vector3(0f, 0.60f, 0f), new Vector3(0.65f, 0.72f, 0.60f));
                Part(root, "Hair", hairMesh, hair, new Vector3(0f, 0.60f, 0f), new Vector3(0.65f, 0.72f, 0.60f));
                Part(root, "Swept hair", box, hair, new Vector3(0.07f, 0.89f, 0.07f), new Vector3(0.28f, 0.13f, 0.24f), Quaternion.Euler(0f, 0f, -14f));
                Part(root, "Eye mask", maskMesh, mask, new Vector3(0f, 0.60f, 0f), new Vector3(0.65f, 0.72f, 0.60f));
                Part(root, "Nose", round, skin, new Vector3(0f, 0.565f, 0.305f), new Vector3(0.07f, 0.09f, 0.08f));
                Part(root, "Mouth", box, hair, new Vector3(0f, 0.46f, 0.284f), new Vector3(0.1f, 0.018f, 0.018f));
                Part(root, "Home star emblem", crest, trim, new Vector3(0f, -0.045f, 0.25f), Vector3.one);
                Part(root, "Belt", round, trim, new Vector3(0f, -0.34f, 0f), new Vector3(0.70f, 0.10f, 0.48f));
                foreach (float side in new[] { -1f, 1f })
                {
                    Part(root, "Ear", detail, skin, new Vector3(side * 0.32f, 0.60f, 0f), new Vector3(0.09f, 0.14f, 0.12f));
                    Part(root, "Eye", detail, eyes, new Vector3(side * 0.12f, 0.67f, 0.315f), new Vector3(0.10f, 0.055f, 0.027f));
                    Part(root, "Pupil", detail, hair, new Vector3(side * 0.12f, 0.67f, 0.333f), new Vector3(0.035f, 0.043f, 0.012f));
                    Part(root, "Arm", round, suit, new Vector3(side * 0.42f, -0.12f, 0f), new Vector3(0.27f, 0.46f, 0.28f), Quaternion.Euler(0f, 0f, side * 10f));
                    Part(root, "Glove", detail, trim, new Vector3(side * 0.46f, -0.43f, 0f), Vector3.one * 0.27f);
                    Part(root, "Leg", round, suit, new Vector3(side * 0.18f, -0.62f, 0f), new Vector3(0.27f, 0.43f, 0.29f));
                    Part(root, "Boot", box, trim, new Vector3(side * 0.18f, -0.86f, 0.06f), new Vector3(0.29f, 0.27f, 0.43f));
                    Part(root, "Cape clasp", detail, trim, new Vector3(side * 0.22f, 0.20f, 0.15f), Vector3.one * 0.10f);
                }
            });
            AssetDatabase.SaveAssets();
            var heroAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            int triangles = 0;
            foreach (var filter in heroAsset.GetComponentsInChildren<MeshFilter>(true)) triangles += filter.sharedMesh.triangles.Length / 3;
            Require(triangles < 3000, "Mobile hero triangle budget");
            Debug.Log("HERO_TRIANGLES: " + triangles);
        }

        private static Mesh Cape()
        {
            var mesh = new CharacterAssets.MeshBuilder();
            Vector3 Point(int x, int y)
            {
                float t = y / 6f, u = x / 8f;
                float width = Mathf.Lerp(0.29f, 0.53f, t);
                return new Vector3((u * 2f - 1f) * width, Mathf.Lerp(0.22f, -0.84f, t),
                    -0.30f - 0.19f * t + Mathf.Sin(u * Mathf.PI * 4f) * 0.026f * t);
            }
            for (int y = 0; y < 6; y++)
            for (int x = 0; x < 8; x++)
            {
                Vector3 a = Point(x, y), b = Point(x + 1, y), c = Point(x + 1, y + 1), d = Point(x, y + 1);
                mesh.Quad(a, b, c, d);
                mesh.Quad(d, c, b, a); // Both sides have outward normals; no cloth simulation required.
            }
            return mesh.Mesh();
        }

        private static Mesh Crest()
        {
            var mesh = new CharacterAssets.MeshBuilder();
            Vector3 Point(int i)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI * 0.25f;
                float radius = i % 2 == 0 ? 0.15f : 0.06f;
                return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            for (int i = 0; i < 8; i++) mesh.Quad(Vector3.forward * 0.02f, Point(i), Point(i + 1), Vector3.forward * 0.02f);
            return mesh.Mesh();
        }

        private static void CheckOxygenBinding()
        {
            EditorSceneManager.OpenScene(FirstExpedition.ScenePath);
            var player = Object.FindAnyObjectByType<SphericalPlayer>();
            var cape = player.GetComponentInChildren<OxygenCape>();
            var body = player.GetComponent<Rigidbody>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SphericalPlayer).GetMethod("Awake", flags).Invoke(player, null);
            var fixedUpdate = typeof(SphericalPlayer).GetMethod("FixedUpdate", flags);
            cape.RefreshFromPlayer();
            Require(ColorNear(cape.DisplayedColor, Blue), "Full oxygen gives a blue cape");
            body.position = Vector3.one * 500f;
            Physics.SyncTransforms();
            int half = Mathf.RoundToInt(player.oxygenCapacity * 0.5f / Time.fixedDeltaTime);
            for (int i = 0; i < half; i++) fixedUpdate.Invoke(player, null);
            cape.RefreshFromPlayer();
            var midpoint = Color.Lerp(Red, Blue, player.OxygenFraction);
            Require(ColorNear(cape.DisplayedColor, midpoint) && !ColorNear(cape.DisplayedColor, Blue) && !ColorNear(cape.DisplayedColor, Red),
                "Partially depleted oxygen blends continuously between blue and red");
            player.SetPaused(true);
            for (int i = 0; i < 5; i++) fixedUpdate.Invoke(player, null);
            cape.RefreshFromPlayer();
            Require(ColorNear(cape.DisplayedColor, midpoint), "Pause preserves the cape color");
            player.SetPaused(false);
            for (int i = 0; i < Mathf.CeilToInt(player.oxygenCapacity / Time.fixedDeltaTime) + 2; i++) fixedUpdate.Invoke(player, null);
            cape.RefreshFromPlayer();
            Require(player.HasLost && ColorNear(cape.DisplayedColor, Red), "Empty oxygen gives a red cape");
            var properties = new MaterialPropertyBlock();
            cape.GetComponent<Renderer>().GetPropertyBlock(properties);
            Require(ColorNear(properties.GetColor("_BaseColor"), Red) &&
                ColorNear(cape.GetComponent<Renderer>().sharedMaterial.GetColor("_BaseColor"), Blue), "Only the instance changes color");
            player.Respawn();
            cape.RefreshFromPlayer();
            Require(ColorNear(cape.DisplayedColor, Blue), "Refilling oxygen restores blue");
            Debug.Log("HERO_CAPE_BINDING_PASSED: full/mid/empty, pause, retry/refill and material isolation.");
        }

        private static void RenderPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int i = 0; i < 4; i++)
            {
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath));
                hero.transform.position = Vector3.right * (-2.1f + i * 1.4f);
                if (i > 0) hero.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                hero.GetComponentInChildren<OxygenCape>().SetOxygenVisual(i <= 1 ? 1f : i == 2 ? 0.5f : 0f);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.64f, 0.72f);
            var key = new GameObject("Hero studio key", typeof(Light)).GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 2f;
            key.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            var camera = new GameObject("Hero studio camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(1.6f, 1.1f, 8f);
            camera.transform.LookAt(new Vector3(0f, -0.03f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 1.60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.07f);
            var texture = new RenderTexture(1800, 1000, 24);
            texture.Create();
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/superhero-cape.png", image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally { RenderTexture.active = previous; camera.targetTexture = null; texture.Release(); Object.DestroyImmediate(texture); }
        }

        private static GameObject Part(GameObject parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion? rotation = null)
        {
            var node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(parent.transform, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            node.transform.localRotation = rotation ?? Quaternion.identity;
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            node.GetComponent<MeshRenderer>().sharedMaterial = material;
            return node;
        }

        private static GameObject Prefab(string name, Action<GameObject> create)
        {
            string path = Folder + "/Prefabs/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var node = new GameObject(name);
            try { create(node); return PrefabUtility.SaveAsPrefabAsset(node, path); }
            finally { Object.DestroyImmediate(node); }
        }

        private static Mesh SharedMesh(string name)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(CharacterAssets.Folder + "/Meshes/" + name + ".asset");
            if (mesh == null) throw new Exception("Missing shared character mesh: " + name);
            return mesh;
        }

        private static Mesh MeshAsset(string name, Func<Mesh> create)
        {
            string path = Folder + "/Meshes/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = create(); mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material Material(string name, Color color, bool emissive = false)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.12f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static bool ColorNear(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.005f;
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Superhero assets: " + message);
        }
    }
}
