using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GravityGame.Editor
{
    public static class StarDemo
    {
        public static void Configure(SphericalPlayer player)
        {
            if (player.stars != null) return;
            var objective = new GameObject("Collect all stars").AddComponent<StarCollection>();
            objective.player = player;
            player.stars = objective;
            var home = player.gravitySources[0];
            var other = player.gravitySources[1];
            // Surface pickups can be reached by walking. No prescribed landing area or order.
            objective.stars = new[]
            {
                Create(objective, home, new Vector3(-0.18f, 0.96f, 0.22f), "Azure - near horizon"),
                Create(objective, home, new Vector3(0.7f, 0.45f, -0.55f), "Azure - far side"),
                Create(objective, other, new Vector3(0.2f, -0.7f, -0.68f), "Ember - explorer")
            };
        }

        internal static CollectibleStar Create(StarCollection collection, PlanetGravity planet, Vector3 normal, string name)
        {
            normal.Normalize();
            var node = new GameObject("Star / " + name, typeof(SphereCollider), typeof(CollectibleStar));
            node.transform.SetParent(collection.transform, false);
            node.transform.SetPositionAndRotation(planet.transform.position + normal * (planet.Radius + 1.35f),
                Quaternion.FromToRotation(Vector3.up, normal));
            var collider = node.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.1f;
            var star = node.GetComponent<CollectibleStar>();
            star.collection = collection;
            star.planet = planet;
            var visual = new GameObject("Gold star", typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(node.transform, false);
            visual.GetComponent<MeshFilter>().sharedMesh = StarMesh();
            var renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = StarMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            star.visual = visual.transform;
            return star;
        }

        private static Material StarMaterial()
        {
            const string path = "Assets/Materials/CollectibleStar.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(1f, 0.72f, 0.12f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Mesh StarMesh()
        {
            const string path = "Assets/Materials/CollectibleStarMesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            // Extruded five-point star, visible from either side; no texture or extra package.
            var vertices = new Vector3[22];
            var triangles = new int[120];
            for (int i = 0; i < 10; i++)
            {
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? 0.85f : 0.38f;
                vertices[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.18f);
                vertices[i + 10] = new Vector3(vertices[i].x, vertices[i].y, 0.18f);
            }
            vertices[20] = Vector3.back * 0.18f;
            vertices[21] = Vector3.forward * 0.18f;
            int t = 0;
            for (int i = 0; i < 10; i++)
            {
                int next = (i + 1) % 10;
                triangles[t++] = 20; triangles[t++] = next; triangles[t++] = i;
                triangles[t++] = 21; triangles[t++] = i + 10; triangles[t++] = next + 10;
                triangles[t++] = i; triangles[t++] = next; triangles[t++] = next + 10;
                triangles[t++] = i; triangles[t++] = next + 10; triangles[t++] = i + 10;
            }
            mesh = new Mesh { name = "Five point collectible star", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
