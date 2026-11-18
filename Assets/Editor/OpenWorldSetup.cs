using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GravityGame.Editor
{
    public static class OpenWorldSetup
    {
        public const string ScenePath = "Assets/Scenes/OpenWorld.unity";
        public const string BoothPath = "Assets/Prefabs/Gameplay/PhoneBooth.prefab";
        public const string MenuPath = "Assets/Prefabs/UI/BoothMenu.prefab";
        public const string WorldUIPath = "Assets/Settings/OpenWorldUI.asset";
        private static readonly Color PanelColor = new Color(0.035f, 0.075f, 0.14f);
        private static readonly Color ButtonColor = new Color(0.10f, 0.30f, 0.47f);
        private static readonly Color Accent = new Color(1f, 0.76f, 0.35f);
        private static readonly Color LightText = new Color(0.88f, 0.94f, 0.99f);

        [MenuItem("GravityGame/Authoring/Open Or Create Open World")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            var template = EditorSceneManager.OpenScene(FirstExpedition.ScenePath);
            if (Object.FindAnyObjectByType<LevelAuthoring>() == null)
                throw new InvalidOperationException("Organize First Expedition into prefabs before creating the open world.");
            if (!EditorSceneManager.SaveScene(template, ScenePath, true))
                throw new IOException("Could not create the open-world scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var level = Object.FindAnyObjectByType<LevelAuthoring>();
            level.displayName = "Haven Reach";
            var world = level.gameObject.AddComponent<WorldProgress>();
            world.worldId = "haven-reach";
            level.objective.finishRunOnComplete = false;

            var source = level.player.gravitySources.ToDictionary(planet => planet.name);
            var haven = source["Azure"];
            var relay = source["Ember"];
            var farpoint = source["Verdant"];
            PlacePlanet(haven, "Haven", Vector3.zero, 42f);
            PlacePlanet(relay, "Relay", new Vector3(0f, 54f, 72f), 30f);
            PlacePlanet(farpoint, "Farpoint", new Vector3(0f, 54f, 146f), 24f);

            foreach (var star in level.objective.stars) Object.DestroyImmediate(star.gameObject);
            AddFragments(level, haven, "haven", new Vector3(0.32f, 1f, 0.3f),
                new Vector3(-0.52f, 0.76f, 0.58f), new Vector3(0.12f, 0.61f, 1f));
            AddFragments(level, relay, "relay", new Vector3(0.3f, 0.65f, -0.72f),
                new Vector3(-0.8f, 0.36f, 0.56f), new Vector3(0.17f, 0.15f, 1f));
            AddFragments(level, farpoint, "farpoint", new Vector3(0.32f, 0.44f, -0.92f),
                new Vector3(-0.7f, 0.72f, 0.12f), new Vector3(0.4f, -0.1f, 1f));

            var enamel = Material("BoothEnamel", new Color(0.04f, 0.56f, 0.62f));
            var trim = Material("BoothTrim", new Color(0.025f, 0.11f, 0.18f));
            var glass = Material("BoothGlass", new Color(0.14f, 0.33f, 0.42f));
            var light = Material("BoothLight", Accent, true);
            var boothPrefab = MakeBooth(enamel, trim, glass, light);
            var booths = PrefabAuthoring.Child("Phone Booths", level.transform);
            var start = PlaceBooth(boothPrefab, booths, haven, "haven-station", "Haven Station",
                new Vector3(0.025f, 1f, 0.10f), Vector3.back);
            PlaceBooth(boothPrefab, booths, relay, "relay-station", "Relay Station",
                new Vector3(0.08f, 0.46f, -1f), new Vector3(0f, 1f, -1f));
            PlaceBooth(boothPrefab, booths, farpoint, "farpoint-station", "Farpoint Station",
                new Vector3(-0.08f, 0.35f, -1f), new Vector3(0f, 1f, -1f));
            var beacons = PrefabAuthoring.Child("Travel Beacons", level.transform);
            AddBeacon(beacons, haven, relay, trim, light);
            AddBeacon(beacons, relay, haven, trim, light);
            AddBeacon(beacons, relay, farpoint, trim, light);
            AddBeacon(beacons, farpoint, relay, trim, light);

            start.GetArrivalPose(out var position, out var rotation);
            level.player.transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(start.transform.position - position, rotation * Vector3.up), rotation * Vector3.up));
            level.startingPlanet = haven;
            level.RefreshReferences();

            var ui = level.interfaceRoot;
            var settings = AssetDatabase.LoadAssetAtPath<GameUISettings>(WorldUIPath);
            if (settings == null)
            {
                settings = Object.Instantiate(ui.settings);
                settings.starCounter = "FRAGMENTS\n{count} / {total}";
                settings.exploreHint = "Follow the gold beacons to cross between planets.";
                settings.tutorial.explore = "Explore the planets and collect star fragments.\nUse a phone booth to save.";
                AssetDatabase.CreateAsset(settings, WorldUIPath);
            }
            ui.settings = settings;
            ui.SafeRoot.Find("Gameplay/Flight caption").GetComponent<Text>().text =
                "PHONE BOOTHS SAVE YOUR EXPLORATION";
            var menu = ((GameObject)PrefabUtility.InstantiatePrefab(MakeMenu(), ui.SafeRoot)).GetComponent<BoothMenu>();
            menu.world = world;
            menu.gameUI = ui;
            ui.boothMenu = menu;
            level.RefreshReferences();
            foreach (var component in level.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("OPEN_WORLD_CREATED: saved three-planet world, booth prefabs, nine fragments and editable booth Canvas.");
        }

        private static void PlacePlanet(PlanetGravity planet, string name, Vector3 position, float radius)
        {
            planet.name = name;
            planet.transform.position = position;
            planet.transform.localScale *= radius / planet.Radius;
            planet.GetComponent<GravityBoundary>().Refresh();
            PrefabUtility.RecordPrefabInstancePropertyModifications(planet.gameObject);
        }

        private static void AddFragments(LevelAuthoring level, PlanetGravity planet, string id, params Vector3[] directions)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAuthoring.Folder + "/CollectibleStar.prefab");
            for (int i = 0; i < directions.Length; i++)
            {
                var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, level.objective.transform);
                item.name = planet.name + " fragment " + (i + 1);
                var fragment = item.GetComponent<CollectibleStar>();
                fragment.collection = level.objective;
                fragment.planet = planet;
                fragment.persistentId = id + "-" + (i + 1);
                var anchor = item.GetComponent<PlanetSurfaceAnchor>();
                anchor.surfaceDirection = directions[i].normalized;
                anchor.heightAboveSurface = 1.35f;
                anchor.ApplyPlacement();
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
        }

        private static PhoneBooth PlaceBooth(GameObject prefab, Transform parent, PlanetGravity planet, string id,
            string stationName, Vector3 direction, Vector3 forward)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = stationName;
            var up = direction.normalized;
            instance.transform.SetPositionAndRotation(planet.transform.position + up * (planet.Radius - 0.035f),
                Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, up), up));
            var booth = instance.GetComponent<PhoneBooth>();
            booth.planet = planet;
            booth.persistentId = id;
            booth.stationName = stationName;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            return booth;
        }

        private static void AddBeacon(Transform parent, PlanetGravity from, PlanetGravity to, Material trim, Material light)
        {
            Vector3 up = (to.transform.position - from.transform.position).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.up, up).normalized;
            var root = PrefabAuthoring.Child(from.name + " to " + to.name, parent);
            root.SetPositionAndRotation(from.transform.position + up * from.Radius, Quaternion.LookRotation(forward, up));
            Box(root, "Launch pad", new Vector3(0f, 0.05f, 0f), new Vector3(6.4f, 0.2f, 4.5f), trim);
            foreach (float side in new[] { -2.55f, 2.55f })
            {
                Box(root, "Beacon pillar", new Vector3(side, 1.9f, 0f), new Vector3(0.38f, 3.8f, 0.38f), trim);
                Box(root, "Gold beacon", new Vector3(side, 2.6f, 0f), new Vector3(0.29f, 2.5f, 0.51f), light);
            }
        }

        private static GameObject MakeBooth(Material enamel, Material trim, Material glass, Material light)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoothPath);
            if (prefab != null) return prefab;
            var root = new GameObject("Phone Booth");
            var booth = root.AddComponent<PhoneBooth>();
            booth.arrival = PrefabAuthoring.Child("Arrival", root.transform);
            booth.arrival.localPosition = new Vector3(0f, 0f, 2.5f);
            Box(root.transform, "Footplate", new Vector3(0f, 0.09f, 0f), new Vector3(2.5f, 0.18f, 1.9f), trim);
            Box(root.transform, "Header", new Vector3(0f, 2.89f, 0f), new Vector3(2.3f, 0.46f, 1.75f), enamel);
            Box(root.transform, "Roof", new Vector3(0f, 3.17f, 0f), new Vector3(2.54f, 0.16f, 1.95f), trim);
            Box(root.transform, "Rear wall", new Vector3(0f, 1.48f, -0.78f), new Vector3(2.03f, 2.63f, 0.14f), glass);
            foreach (float x in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -0.72f, 0.72f })
                    Box(root.transform, "Frame post", new Vector3(x, 1.48f, z), new Vector3(0.16f, 2.7f, 0.16f), enamel);
                Box(root.transform, "Lower side panel", new Vector3(x, 0.65f, 0f), new Vector3(0.13f, 0.94f, 1.5f), enamel);
                Box(root.transform, "Window rail", new Vector3(x, 1.38f, 0f), new Vector3(0.16f, 0.12f, 1.5f), trim);
            }
            Box(root.transform, "Phone body", new Vector3(0f, 1.55f, -0.63f), new Vector3(0.53f, 0.72f, 0.17f), trim);
            Box(root.transform, "Phone display", new Vector3(0.07f, 1.72f, -0.53f), new Vector3(0.22f, 0.18f, 0.04f), light);
            Box(root.transform, "Handset", new Vector3(-0.19f, 1.52f, -0.48f), new Vector3(0.115f, 0.51f, 0.12f), enamel);
            Box(root.transform, "Header lamp", new Vector3(0f, 2.83f, 0.91f), new Vector3(1.56f, 0.14f, 0.052f), light);
            Box(root.transform, "Roof signal", new Vector3(0f, 3.37f, 0f), new Vector3(0.31f, 0.23f, 0.31f), light);
            var sign = PrefabAuthoring.Child("Phone sign", root.transform);
            sign.localPosition = new Vector3(0f, 3.02f, 0.895f);
            sign.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = sign.gameObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "PHONE";
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 38;
            text.characterSize = 0.055f;
            text.color = LightText;
            sign.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            prefab = PrefabUtility.SaveAsPrefabAsset(root, BoothPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject MakeMenu()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath);
            if (prefab != null) return prefab;
            var root = new GameObject("Booth Menu", typeof(RectTransform), typeof(BoothMenu));
            var menu = root.GetComponent<BoothMenu>();
            var rect = (RectTransform)root.transform;
            Stretch(rect);
            menu.openButton = Button(rect, "Open booth", "PHONE BOOTH", new Vector2(0f, -175f),
                new Vector2(270f, 62f), ButtonColor, new Vector2(0.5f, 1f));
            menu.openButton.gameObject.SetActive(false);

            var shade = Image(rect, "Booth overlay", new Color(0.008f, 0.016f, 0.04f, 0.86f));
            Stretch(shade.rectTransform);
            menu.panel = shade.gameObject;
            var dialog = Image(shade.transform, "Phone booth dialog", PanelColor);
            Place(dialog.rectTransform, Vector2.zero, new Vector2(680f, 548f));
            var body = dialog.transform;
            menu.stationHeading = Label(body, "Station name", "Haven Station", new Vector2(0f, 220f),
                new Vector2(588f, 62f), 39, TextAnchor.MiddleCenter);
            menu.inventoryTab = Button(body, "Inventory tab", "INVENTORY", new Vector2(-150f, 140f),
                new Vector2(292f, 65f), ButtonColor);
            menu.settingsTab = Button(body, "Settings tab", "SETTINGS", new Vector2(150f, 140f),
                new Vector2(292f, 65f), ButtonColor);
            var inventory = Rect(body, "Inventory page");
            var settings = Rect(body, "Settings page");
            Stretch(inventory);
            Stretch(settings);
            menu.inventoryPage = inventory.gameObject;
            menu.settingsPage = settings.gameObject;
            menu.inventoryText = Label(inventory, "Fragment inventory", "Star fragments", new Vector2(-5f, -15f),
                new Vector2(560f, 185f), 29, TextAnchor.MiddleLeft);
            menu.volumeText = Label(settings, "Volume value", "Volume   100%", new Vector2(0f, 48f),
                new Vector2(360f, 58f), 26, TextAnchor.MiddleCenter);
            menu.lookText = Label(settings, "Sensitivity value", "Look sensitivity   100%", new Vector2(0f, -37f),
                new Vector2(360f, 58f), 26, TextAnchor.MiddleCenter);
            menu.volumeDown = Adjustment(settings, "Decrease volume", "-", -245f, 48f);
            menu.volumeUp = Adjustment(settings, "Increase volume", "+", 245f, 48f);
            menu.lookDown = Adjustment(settings, "Decrease sensitivity", "-", -245f, -37f);
            menu.lookUp = Adjustment(settings, "Increase sensitivity", "+", 245f, -37f);
            settings.gameObject.SetActive(false);
            menu.statusText = Label(body, "Save status", "", new Vector2(0f, -127f),
                new Vector2(592f, 54f), 22, TextAnchor.MiddleCenter);
            menu.statusText.color = Accent;
            menu.saveButton = Button(body, "Save progress", "SAVE", new Vector2(-150f, -210f),
                new Vector2(292f, 70f), Accent);
            menu.saveButton.GetComponentInChildren<Text>().color = PanelColor;
            menu.closeButton = Button(body, "Close booth", "BACK TO WORLD", new Vector2(150f, -210f),
                new Vector2(292f, 70f), ButtonColor);
            menu.panel.SetActive(false);
            prefab = PrefabUtility.SaveAsPrefabAsset(root, MenuPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Button Adjustment(Transform parent, string name, string label, float x, float y) =>
            Button(parent, name, label, new Vector2(x, y), new Vector2(82f, 64f), ButtonColor);

        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size,
            Color color, Vector2? anchor = null)
        {
            var graphic = Image(parent, name, color);
            Place(graphic.rectTransform, position, size, anchor);
            var button = graphic.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(graphic.transform, "Label", label, Vector2.zero, size, 23, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size,
            int fontSize, TextAnchor alignment)
        {
            var text = Rect(parent, name).gameObject.AddComponent<Text>();
            Place(text.rectTransform, position, size);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = LightText;
            text.raycastTarget = false;
            return text;
        }

        private static Image Image(Transform parent, string name, Color color)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
