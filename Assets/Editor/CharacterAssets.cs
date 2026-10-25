using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class CharacterAssets
    {
        public const string Folder = "Assets/Art/Characters";
        public const string ExplorerPath = Folder + "/Prefabs/AstronautExplorer.prefab";
        private const string ScoutPath = Folder + "/Prefabs/AstronautScout.prefab";
        private static Mesh box, round, sphere, cylinder, rim, visor, reflection;
        private static Material white, orange, blue, navy, gold, glass, cyan, shine;

        [MenuItem("GravityGame/Art/Create Astronaut Assets and Apply")]
        public static void CreateApplyAndPreview()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateAssets();
            foreach (string scenePath in new[] { "Assets/Scenes/MobilePrototype.unity", FirstExpedition.ScenePath,
                "Assets/Scenes/TwoPlanets.unity", "Assets/Scenes/PhaseOne.unity" })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var player = Object.FindAnyObjectByType<SphericalPlayer>();
                ApplyTo(player);
                Require(player.GetComponent<CapsuleCollider>().height == 2f &&
                    Mathf.Approximately(player.GetComponent<CapsuleCollider>().radius, 0.5f), "Original movement capsule retained");
                Require(player.GetComponent<Rigidbody>() != null && player.planet != null, "Physics references retained");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            RenderPreview();
            AssetDatabase.SaveAssets();
            Debug.Log("CHARACTER_ASSETS_PASSED: four reusable prefabs; no visual colliders; original player physics retained in four scenes; studio preview rendered.");
        }

        public static void ApplyTo(SphericalPlayer player)
        {
            if (player == null) throw new Exception("No player found for the astronaut visual.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplorerPath);
            if (prefab == null) { CreateAssets(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplorerPath); }
            if (player.transform.Find("Astronaut Visual") == null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
                model.name = "Astronaut Visual";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
            }
            // Preserve the root's mesh/collider identity and every movement reference.
            var oldRenderer = player.GetComponent<MeshRenderer>();
            if (oldRenderer != null) oldRenderer.enabled = false;
            Require(player.transform.Find("Astronaut Visual").GetComponentsInChildren<Collider>(true).Length == 0,
                "Visual parts must not add gameplay collisions");
        }

        private static void CreateAssets()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(Folder);
            foreach (string part in new[] { "Meshes", "Materials", "Prefabs" }) EnsureFolder(Folder + "/" + part);
            white = Material("SuitWhite", new Color(0.84f, 0.9f, 0.95f));
            orange = Material("SafetyOrange", new Color(1f, 0.29f, 0.055f));
            blue = Material("ScoutBlue", new Color(0.065f, 0.43f, 0.74f));
            navy = Material("BootsAndPack", new Color(0.035f, 0.075f, 0.12f));
            gold = Material("HelmetRim", new Color(0.94f, 0.61f, 0.15f), 0.5f, 0.5f);
            glass = Material("Visor", new Color(0.015f, 0.08f, 0.13f), 0.65f, 0.85f);
            cyan = Material("TankStripe", new Color(0.14f, 0.85f, 0.95f));
            shine = Material("VisorReflection", new Color(0.6f, 0.89f, 1f), 0.2f, 0.75f);
            box = MeshAsset("Box", Box);
            round = MeshAsset("BevelledSuit", () => Lathe(new[] { new Vector2(-0.5f, 0f), new Vector2(-0.5f, 0.28f),
                new Vector2(-0.38f, 0.5f), new Vector2(0.38f, 0.5f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0f) }, 12));
            cylinder = MeshAsset("Cylinder", () => Lathe(new[] { new Vector2(-0.5f, 0f), new Vector2(-0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f) }, 12));
            sphere = MeshAsset("HelmetSphere", () =>
            {
                var profile = new Vector2[11];
                for (int i = 0; i <= 10; i++)
                {
                    float angle = -Mathf.PI * 0.5f + i * Mathf.PI / 10f;
                    profile[i] = new Vector2(Mathf.Sin(angle) * 0.5f, Mathf.Max(0f, Mathf.Cos(angle) * 0.5f));
                }
                return Lathe(profile, 16);
            });
            rim = MeshAsset("VisorRim", () => Patch(0.455f, -32f, 32f, -75f, 75f, 8, 4));
            visor = MeshAsset("VisorWindow", () => Patch(0.47f, -25f, 25f, -66f, 66f, 8, 4));
            reflection = MeshAsset("VisorHighlight", () => Patch(0.48f, 13f, 20f, -44f, -24f, 2, 1));
            var helmet = Prefab("AstronautHelmet", root =>
            {
                Part(root, "White helmet shell", sphere, white, Vector3.zero, Vector3.one * 0.86f);
                Part(root, "Gold visor frame", rim, gold, Vector3.zero, Vector3.one);
                Part(root, "Dark visor", visor, glass, Vector3.zero, Vector3.one);
                Part(root, "Visor glint", reflection, shine, Vector3.zero, Vector3.one);
                foreach (float side in new[] { -1f, 1f })
                    Part(root, "Helmet radio", cylinder, orange, new Vector3(side * 0.42f, 0f, 0f),
                        new Vector3(0.19f, 0.07f, 0.19f), Quaternion.Euler(0f, 0f, 90f));
            });
            var pack = Prefab("OxygenBackpack", root =>
            {
                Part(root, "Backpack housing", box, navy, Vector3.zero, new Vector3(0.59f, 0.64f, 0.28f));
                foreach (float side in new[] { -1f, 1f })
                {
                    Part(root, "Oxygen cylinder", round, white, new Vector3(side * 0.17f, 0f, -0.21f), new Vector3(0.22f, 0.56f, 0.22f));
                    Part(root, "Tank safety band", cylinder, orange, new Vector3(side * 0.17f, -0.12f, -0.21f), new Vector3(0.227f, 0.07f, 0.227f));
                }
                Part(root, "Blue tank stripe", box, cyan, new Vector3(0f, 0.08f, -0.325f), new Vector3(0.07f, 0.32f, 0.025f));
            });
            Prefab("AstronautExplorer", root => Character(root, helmet, pack, white, orange));
            Prefab("AstronautScout", root => Character(root, helmet, pack, blue, cyan));
            AssetDatabase.SaveAssets();
            foreach (string path in new[] { ExplorerPath, ScoutPath, Folder + "/Prefabs/AstronautHelmet.prefab", Folder + "/Prefabs/OxygenBackpack.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab != null && prefab.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    prefab.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Reusable visual-only prefab");
                int triangles = 0;
                foreach (var mesh in prefab.GetComponentsInChildren<MeshFilter>(true)) triangles += mesh.sharedMesh.triangles.Length / 3;
                Require(triangles < 3000, "Mobile triangle budget");
                Debug.Log("CHARACTER_PREFAB: " + path + " triangles=" + triangles);
            }
        }

        private static void Character(GameObject root, GameObject helmet, GameObject pack, Material suit, Material accent)
        {
            Nested(helmet, root, new Vector3(0f, 0.58f, 0f));
            Nested(pack, root, new Vector3(0f, -0.1f, -0.3f));
            Part(root, "Suit torso", round, suit, new Vector3(0f, -0.1f, 0f), new Vector3(0.68f, 0.66f, 0.47f));
            Part(root, "Neck collar", cylinder, navy, new Vector3(0f, 0.245f, 0f), new Vector3(0.44f, 0.1f, 0.44f));
            Part(root, "Utility belt", round, accent, new Vector3(0f, -0.33f, 0f), new Vector3(0.66f, 0.11f, 0.48f));
            Part(root, "Chest panel", box, accent, new Vector3(0f, -0.06f, 0.24f), new Vector3(0.32f, 0.25f, 0.07f));
            Part(root, "Chest indicator", box, cyan, new Vector3(0f, -0.02f, 0.28f), new Vector3(0.19f, 0.045f, 0.02f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Arm", round, suit, new Vector3(side * 0.39f, -0.12f, 0f),
                    new Vector3(0.25f, 0.45f, 0.27f), Quaternion.Euler(0f, 0f, side * 12f));
                Part(root, "Glove", sphere, accent, new Vector3(side * 0.445f, -0.42f, 0f), Vector3.one * 0.27f);
                Part(root, "Trouser leg", round, suit, new Vector3(side * 0.18f, -0.62f, 0f), new Vector3(0.26f, 0.43f, 0.29f));
                Part(root, "Boot", box, navy, new Vector3(side * 0.18f, -0.87f, 0.065f), new Vector3(0.29f, 0.24f, 0.44f));
                Part(root, "Boot sole", box, accent, new Vector3(side * 0.18f, -0.975f, 0.065f), new Vector3(0.295f, 0.045f, 0.445f));
            }
        }

        private static void Nested(GameObject prefab, GameObject parent, Vector3 position)
        {
            var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            node.transform.localPosition = position;
        }

        private static void Part(GameObject parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Quaternion? rotation = null)
        {
            var node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(parent.transform, false);
            node.transform.localPosition = position;
            node.transform.localScale = scale;
            node.transform.localRotation = rotation ?? Quaternion.identity;
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            node.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static GameObject Prefab(string name, Action<GameObject> build)
        {
            string path = Folder + "/Prefabs/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing; // Never overwrite an artist's subsequent edits.
            var root = new GameObject(name);
            try { build(root); return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        private static Material Material(string name, Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset != null) return asset;
            asset = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            asset.SetColor("_BaseColor", color);
            asset.SetFloat("_Metallic", metallic);
            asset.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Mesh MeshAsset(string name, Func<Mesh> create)
        {
            string path = Folder + "/Meshes/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = create();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh Lathe(Vector2[] profile, int segments)
        {
            var builder = new MeshBuilder();
            for (int row = 0; row < profile.Length - 1; row++)
            for (int col = 0; col < segments; col++)
            {
                float a = col * Mathf.PI * 2f / segments, b = (col + 1) * Mathf.PI * 2f / segments;
                Vector3 Point(Vector2 p, float angle) => new Vector3(Mathf.Sin(angle) * p.y, p.x, Mathf.Cos(angle) * p.y);
                builder.Quad(Point(profile[row], a), Point(profile[row], b), Point(profile[row + 1], b), Point(profile[row + 1], a));
            }
            return builder.Mesh();
        }

        private static Mesh Patch(float radius, float bottom, float top, float left, float right, int columns, int rows)
        {
            var builder = new MeshBuilder();
            Vector3 Point(int x, int y)
            {
                float lat = Mathf.Lerp(bottom, top, (float)y / rows) * Mathf.Deg2Rad;
                float lon = Mathf.Lerp(left, right, (float)x / columns) * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(lon) * Mathf.Cos(lat), Mathf.Sin(lat), Mathf.Cos(lon) * Mathf.Cos(lat)) * radius;
            }
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++) builder.Quad(Point(x, y), Point(x + 1, y), Point(x + 1, y + 1), Point(x, y + 1));
            return builder.Mesh();
        }

        private static Mesh Box()
        {
            var builder = new MeshBuilder();
            foreach (Vector3 normal in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down })
            {
                Vector3 right = Vector3.Cross(Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up, normal) * 0.5f;
                Vector3 up = Vector3.Cross(normal, right);
                Vector3 center = normal * 0.5f;
                builder.Quad(center - right - up, center + right - up, center + right + up, center - right + up);
            }
            return builder.Mesh();
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Triangle(a, b, c); Triangle(a, c, d); }
            private void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < 0.0000000001f) return;
                int index = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            }
            public Mesh Mesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static void RenderPreview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var front = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ExplorerPath));
            front.transform.position = Vector3.left * 1.5f;
            var back = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ExplorerPath));
            back.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var scout = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
            scout.transform.position = Vector3.right * 1.5f;
            Part(new GameObject("Studio floor"), "Floor", box, navy, new Vector3(0f, -1.04f, 0f), new Vector3(7f, 0.06f, 3f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
            var key = new GameObject("Studio key", typeof(Light)).GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 2f;
            key.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            var view = new GameObject("Studio camera", typeof(Camera)).GetComponent<Camera>();
            view.transform.position = new Vector3(2.4f, 1.4f, 7f);
            view.transform.LookAt(new Vector3(0f, -0.05f, 0f));
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.025f, 0.04f, 0.07f);
            view.orthographic = true;
            view.orthographicSize = 1.55f;
            var texture = new RenderTexture(1600, 900, 24);
            texture.Create();
            var previous = RenderTexture.active;
            try
            {
                view.targetTexture = texture;
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture;
                var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/astronaut-assets.png", pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);
            }
            finally { RenderTexture.active = previous; view.targetTexture = null; texture.Release(); Object.DestroyImmediate(texture); }
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Character assets: " + message);
        }
    }
}
